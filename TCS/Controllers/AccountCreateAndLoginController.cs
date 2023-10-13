using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

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
    [Route("api")]
    public class AccountCreateAndLoginController : ControllerBase
    {
        private readonly ILogger<AccountCreateAndLoginController> _logger;
        private static readonly Regex emailRegex = new(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", RegexOptions.Compiled);
        private static readonly Regex loginRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex passwordRegex = new("^[a-zA-Z0-9!@#$%^&*()_-]+$", RegexOptions.Compiled);
        public AccountCreateAndLoginController(ILogger<AccountCreateAndLoginController> logger)
        {
            _logger = logger;
        }
        private static bool ValidateEmail(string email)
        {
            // Проверяем, что email соответствует регулярному выражению и его длина не превышает 30 символов
            return emailRegex.IsMatch(email) && email.Length <= 30;
        }
        private static bool ValidateLogin(string login)
        {
            int minLength = 4; // Минимальная длина
            int maxLength = 12; // Максимальная длина

            bool hasValidLength = login.Length >= minLength && login.Length <= maxLength;
            bool matchesPattern = loginRegex.IsMatch(login);

            return hasValidLength && matchesPattern;
        }
        private static bool ValidatePassword(string password)
        {
            int minLength = 5; // Минимальная длина пароля
            int maxLength = 30; // Максимальная длина пароля

            bool hasValidLength = password.Length >= minLength && password.Length <= maxLength;
            bool matchesPattern = passwordRegex.IsMatch(password);

            return hasValidLength && matchesPattern;
        }

        [HttpPost]
        [Route("registration")]
        public async Task<ActionResult> Registration([FromBody] RegistrationModel model)
        {
            string error;
            if(!(ValidateEmail(model.Email) && ValidateLogin(model.Login) && ValidatePassword(model.Password)))
            {
                // Данные не прошли валидацию
                var data = new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                };
                return Ok(data);
            }
            error = await Database.CheckBusy(model);
            if (error != "ОК")
            {
                var data = new
                {
                    status = "error",
                    message = error
                };
                return Ok(data);
            }
            int id = await Database.AddUser(model);
            var auth_token = await Database.CreateSession(id);
            // Создание и добавление cookie в ответ

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Чтобы предотвратить доступ к cookie средствами JavaScript
                Secure = true,   // Если ваше приложение работает по HTTPS
                SameSite = SameSiteMode.Lax, // Установите в соответствии с вашими требованиями безопасности
                Expires = DateTime.UtcNow.AddMonths(1) // Время жизни cookie (например, 1 месяц)
            };
            Response.Cookies.Append("auth_token", auth_token, cookieOptions);
            // редирект на главную страницу
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
            if (!(ValidateLogin(model.Login) && ValidatePassword(model.Password)))
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
            var id = await Database.CheckUser(model);
            if(id == -1)
            {
                var data = new
                {
                    status = "error",
                    message = "Пользователь не найден."
                };
                return Ok(data);
            }
            // получаем токен сессии
            var auth_token = await Database.CreateSession(id);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Чтобы предотвратить доступ к cookie средствами JavaScript
                Secure = true,   // Если ваше приложение работает по HTTPS
                SameSite = SameSiteMode.Lax, // Установите в соответствии с вашими требованиями безопасности
                Expires = DateTime.UtcNow.AddMonths(1) // Время жизни cookie (например, 1 месяц)
            };
            Response.Cookies.Append("auth_token", auth_token, cookieOptions);
            // редирект на главную страницу
            return Ok(new
            {
                status = "ok"
            });
        }

    }
}
