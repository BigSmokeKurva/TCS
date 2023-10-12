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
    [ApiController]
    [Route("api")]
    public class AccountCreateAndLoginController : ControllerBase
    {
        private readonly ILogger<AccountCreateAndLoginController> _logger;

        public AccountCreateAndLoginController(ILogger<AccountCreateAndLoginController> logger)
        {
            _logger = logger;
        }
        private static bool ValidateEmail(string email)
        {
            // Регулярное выражение для валидации email
            string emailPattern = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
            return Regex.IsMatch(email, emailPattern);
        }
        private static bool ValidateLogin(string login)
        {
            // Регулярное выражение для валидации логина
            string loginPattern = "^[a-zA-Z0-9_-]+$";

            return Regex.IsMatch(login, loginPattern);
        }
        private static bool ValidatePassword(string password)
        {
            int minLength = 5; // Минимальная длина пароля
            string passwordPattern = "^[a-zA-Z0-9!@#$%^&*()_-]+$";

            bool hasValidLength = password.Length >= minLength;
            bool matchesPattern = Regex.IsMatch(password, passwordPattern);

            return hasValidLength && matchesPattern;
        }

        [HttpPost]
        [Route("registration")]
        public async Task<ActionResult> Registration([FromBody] RegistrationModel model)
        {
            string error;
            if(!(ValidateEmail(model.Email) || ValidateLogin(model.Login) || ValidatePassword(model.Password)))
            {
                // Данные не прошли валидацию
                // TODO
                return Ok("Bad");
            }
            error = await Database.CheckBusy(model);
            if (error != "ОК")
            {
                // Уже есть в базе
                return Ok(error);
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
            return Ok("GOOD");
        }
    }
}
