using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCS.Controllers.Models;
using TCS.Database;

namespace TCS.Controllers
{

    [ApiController]
    [Route("api/auth")]
    public class AuthApiController(ILogger<AuthApiController> logger, DatabaseContext db) : ControllerBase
    {
        private readonly ILogger<AuthApiController> _logger = logger;
        private readonly DatabaseContext db = db;

        [HttpPost]
        [Route("registration")]
        public async Task<ActionResult> Registration([FromBody] RegistrationModel model)
        {
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

            if (await db.Users.AnyAsync(x => x.Username == model.Login))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Данный логин уже используется."
                });
            }
            if (await db.Users.AnyAsync(x => x.Email == model.Email))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Данная почта уже используется."
                });
            }
            var user = await db.Users.AddAsync(new()
            {
                Email = model.Email,
                Username = model.Login,
                Password = model.Password,
                Sessions = [
                   new()
                   {
                       Expires = TimeHelper.GetMoscowTime().AddDays(30),
                   }
               ],
                Configuration = new(),
                Logs = [
                   new()
                   {
                       Time = TimeHelper.GetMoscowTime(),
                       Message = "Зарегистрировался.",
                       Type = Database.Models.LogType.Action
                   }
               ]
            });
            await db.SaveChangesAsync();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,

                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = TimeHelper.GetMoscowTime().AddDays(30)
            };
            Response.Cookies.Append("auth_token", user.Entity.Sessions.First().AuthToken.ToString(), cookieOptions);
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

            var user = await db.Users.FirstOrDefaultAsync(x => x.Username == model.Login && x.Password == model.Password);
            if (user is null)
            {
                var data = new
                {
                    status = "error",
                    message = "Пользователь не найден."
                };
                return Ok(data);
            }
            // получаем токен сессии
            var auth_token = await db.Sessions.AddAsync(new()
            {
                Id = user.Id,
                Expires = TimeHelper.GetMoscowTime().AddDays(30)
            });
            await db.AddLog(user, "Авторизовался.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = TimeHelper.GetMoscowTime().AddDays(30),
                Path = "/"
            };
            Response.Cookies.Append("auth_token", auth_token.Entity.AuthToken.ToString(), cookieOptions);
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

            var auth_token = Guid.Parse(Request.Cookies["auth_token"]);

            if (!await db.Sessions.AnyAsync(x => x.AuthToken == auth_token))
                return Ok(new { status = "ok" });
            db.Sessions.Remove(await db.Sessions.FindAsync(auth_token));
            await db.AddLog(await db.Users.FirstAsync(x => x.Sessions.Any(y => y.AuthToken == auth_token && x.Id == y.Id)), "Вышел из аккаунта.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
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
