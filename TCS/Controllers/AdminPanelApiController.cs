using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using TCS.BotsManager;
using TCS.Controllers.Models;
using TCS.Database;
using TCS.Filters;

namespace TCS.Controllers
{

    [Route("api/admin")]
    [ApiController]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
    public class AdminPanelApiController(DatabaseContext db) : ControllerBase
    {
        private readonly DatabaseContext db = db;
        [HttpGet]
        [Route("getusers")]
        public async Task<ActionResult> GetUsers()
        {

            var users = await db.Users
                .Where(x => x.Username != "root")
                .Select(x => new
                {
                    x.Id,
                    x.Username,
                    x.Email
                }).ToListAsync();
            return Ok(users);
        }

        [HttpGet]
        [Route("getuserinfo")]
        public async Task<ActionResult> GetUserInfo(int id)
        {
            // TODO
            var user = await db.Users.FindAsync(id);

            var userInfo = await db.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    Admin = u.Admin,
                    Password = u.Password,
                    StreamerUsername = u.Configuration.StreamerUsername,
                    TokensCount = u.Configuration.Tokens.Count(),
                    ProxiesCount = u.Configuration.Proxies.Count(),
                    LogsTime = u.Logs
                        .Select(l => l.Time.Date.ToString("dd.MM.yyyy"))
                        .ToImmutableHashSet()
                })
                .FirstAsync();

            return Ok(userInfo);
        }

        [HttpGet]
        [Route("getlogs")]
        public async Task<ActionResult> GetLogs(int id, string time)
        {

            var timeParsed = DateTime.ParseExact(time, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var logs = await db.Logs
                .Where(x => x.Id == id && timeParsed.Date == x.Time.Date)
                .ToListAsync();
            return Ok(logs);
        }

        [HttpPost]
        [Route("edituser")]
        public async Task<ActionResult> EditUser([FromBody] EditUserModel model)
        {
            return model.Property switch
            {
                ChangeType.Username => await ChangeUsername(model.Id, model.Value.GetString()),
                ChangeType.Password => await ChangePassword(model.Id, model.Value.GetString()),
                ChangeType.Email => await ChangeEmail(model.Id, model.Value.GetString()),
                ChangeType.Admin => await ChangeAdmin(model.Id, model.Value.GetBoolean()),
                ChangeType.Tokens => await ChangeTokens(model.Id, model.Value),
                ChangeType.Proxies => await ChangeProxies(model.Id, model.Value),
                _ => Ok(new
                {
                    status = "error",
                    message = "Неизвестное свойство."
                }),
            };
        }
        private async Task<ActionResult> ChangeUsername(int id, string username)
        {

            if (!UserValidators.ValidateLogin(username))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                });
            }
            if (await db.Users.AnyAsync(x => x.Username == username))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Пользователь с таким логином уже существует."
                });
            }
            var user = await db.Users.FindAsync(id);
            user.Username = username;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangePassword(int id, string password)
        {

            if (!UserValidators.ValidatePassword(password))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                });
            }
            var user = await db.Users.FindAsync(id);
            user.Password = password;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangeEmail(int id, string email)
        {

            if (!UserValidators.ValidateEmail(email))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                });
            }
            if (await db.Users.AnyAsync(x => x.Email == email))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Пользователь с таким email уже существует."
                });
            }
            var user = await db.Users.FindAsync(id);
            user.Email = email;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangeAdmin(int id, bool value)
        {

            var user = await db.Users.FindAsync(id);
            user.Admin = value;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangeTokens(int id, JsonElement tokens)
        {

            var tokensChecked = await TokenCheck.Check(tokens: tokens.EnumerateArray().Select(x => x.GetString()).Distinct());
            await Manager.StopSpam(id, db);
            await Manager.DisconnectAllBots(id, db);
            var user = await db.Users.FindAsync(id);
            user.Configuration.Tokens = tokensChecked;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                message = tokensChecked.Count
            });
        }
        private async Task<ActionResult> ChangeProxies(int id, JsonElement proxies)
        {

            var proxiesChecked = ProxyCheck.Parse(proxies.EnumerateArray().Select(x => x.GetString()));
            await Manager.StopSpam(id, db);
            await Manager.DisconnectAllBots(id, db);
            var user = await db.Users.FindAsync(id);
            user.Configuration.Proxies = proxiesChecked;
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                message = proxiesChecked.Count
            });
        }

        [HttpGet]
        [Route("gettokens")]
        public async Task<ActionResult> GetTokens(int id)
        {

            var tokens = await db.Configurations
                .Where(x => x.Id == id)
                .Select(x => x.Tokens)
                .FirstAsync();
            return Ok(tokens);
        }

        [HttpGet]
        [Route("getproxies")]
        public async Task<ActionResult> GetProxies(int id)
        {

            var proxies = await db.Configurations
                .Where(x => x.Id == id)
                .Select(x => x.Proxies)
                .FirstAsync();
            return Ok(ProxyCheck.ProxyToString(proxies));
        }

        [HttpDelete]
        [Route("deleteuser")]
        public async Task<ActionResult> DeleteUser(int id)
        {

            await Manager.Remove(id, db);
            db.Users.Remove(await db.Users.FindAsync(id));
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}
