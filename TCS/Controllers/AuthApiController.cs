using Microsoft.AspNetCore.Mvc;

namespace TCS.Controllers
{
    public class RegistrationModel
    {
        public string Email { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }
    public class AuthorizationModel
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly ILogger<AuthApiController> _logger;
        public AuthApiController(ILogger<AuthApiController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("registration")]
        public async Task<ActionResult> Registration([FromBody] RegistrationModel model)
        {
            string error;
            if (!(UserValidators.ValidateEmail(model.Email) && UserValidators.ValidateLogin(model.Login) && UserValidators.ValidatePassword(model.Password)))
            {
                // Данные не прошли валидацию
                var data = new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                };
                return Ok(data);
            }
            error = await Database.AuthArea.CheckBusy(model);
            if (error != "ОК")
            {
                var data = new
                {
                    status = "error",
                    message = error
                };
                return Ok(data);
            }
            int id = await Database.AuthArea.AddUser(model);
            var auth_token = await Database.AuthArea.CreateSession(id);
            // Создание и добавление cookie в ответ

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // Чтобы предотвратить доступ к cookie средствами JavaScript
                Secure = true,   // Если ваше приложение работает по HTTPS
                SameSite = SameSiteMode.None, // Установите в соответствии с вашими требованиями безопасности
                Expires = DateTime.UtcNow.AddMonths(1) // Время жизни cookie (например, 1 месяц)
            };
            Response.Cookies.Append("auth_token", auth_token, cookieOptions);
            // редирект на главную страницу
            await Database.SharedArea.Log(id, "Зарегистрировался.");
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("authorization")]
        public async Task<ActionResult> Authorization([FromBody] AuthorizationModel model)
        {
            string error;
            if (!(UserValidators.ValidateLogin(model.Login) && UserValidators.ValidatePassword(model.Password)))
            {
                // Данные не прошли валидацию
                var data = new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                };
                return Ok(data);
            }
            // проверка на существование пользователя
            var id = await Database.AuthArea.CheckUser(model);
            if (id == -1)
            {
                var data = new
                {
                    status = "error",
                    message = "Пользователь не найден."
                };
                return Ok(data);
            }
            // получаем токен сессии
            var auth_token = await Database.AuthArea.CreateSession(id);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // Чтобы предотвратить доступ к cookie средствами JavaScript
                Secure = true,   // Если ваше приложение работает по HTTPS
                SameSite = SameSiteMode.None, // Установите в соответствии с вашими требованиями безопасности
                Expires = DateTime.UtcNow.AddMonths(1)
            };
            Response.Cookies.Append("auth_token", auth_token, cookieOptions);
            await Database.SharedArea.Log(id, "Авторизовался.");
            // редирект на главную страницу
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("unauthorization")]
        public async Task<ActionResult> Unauthorization()
        {
            // Получаем токен сессии из cookie
            var auth_token = Request.Cookies["auth_token"];
            if (!await Database.AuthArea.IsValidAuthToken(auth_token))
                return Ok(new { status = "ok" });
            // удалением его из базы
            if (auth_token is not null)
                await Database.SharedArea.DeleteAuthToken(auth_token);
            // Удаляем все cookie на сайте
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            // Выполняем редирект на страницу "/"
            return Ok(new { status = "ok" });
        }
    }
}
