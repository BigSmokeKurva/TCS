using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using TCS.BotsManager;
using TCS.Controllers.Models;
using TCS.Database;
using TCS.Database.Models;
using TCS.Filters;
using TCS.Follow;

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
            var user = await db.Users.FindAsync(id);

            var userInfo = await db.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    Admin = u.Admin,
                    Password = u.Password,
                    TokensCount = u.Configuration.Tokens.Count(),
                    Paused = u.Paused,
                    LogsTime = u.Logs
                        .Select(l => l.Time.Date.ToString("dd.MM.yyyy"))
                        .ToImmutableHashSet()
                        .ToList()
                })
                .FirstAsync();
            userInfo.LogsTime.Reverse();

            return Ok(userInfo);
        }

        [HttpGet]
        [Route("getlogs")]
        public async Task<ActionResult> GetLogs(int id, string time, LogType type)
        {

            var timeParsed = DateTime.ParseExact(time, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var logs = await db.Logs
                .Where(x => x.Id == id && timeParsed.Date == x.Time.Date && x.Type == type)
                .Select(x => new
                {
                    x.Message,
                    x.Time
                })
                .ToListAsync();
            logs.Reverse();
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
                ChangeType.Paused => await ChangePaused(model.Id, model.Value.GetBoolean()),
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
        private async Task<ActionResult> ChangePaused(int id, bool value)
        {
            var user = await db.Users.FindAsync(id);
            await Manager.StopSpam(id, db);
            await Manager.DisconnectAllBots(id, db);
            user.Paused = value;
            if (value)
            {
                await Manager.StopSpam(id, db);
                await Manager.DisconnectAllBots(id, db);
                await FollowBot.RemoveAllFromQueue(x => x.Id == id);
            }
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }
        private async Task<ActionResult> ChangeTokens(int id, JsonElement tokens)
        {
            // Format: token:proxy_type:proxy_host:proxy_port:proxy_username:proxy_password
            var _tokens = tokens.EnumerateArray().Select(x => (x.GetString()).Split(':')).Distinct().Where(x => x.Length == 6).ToDictionary(x => x[0], x => x[1..]);
            var tokensChecked = await TokenCheck.Check(_tokens.Keys);
            await Manager.StopSpam(id, db);
            await Manager.DisconnectAllBots(id, db);
            await FollowBot.RemoveAllFromQueue(x => x.Id == id);
            var user = await db.Users.FindAsync(id);
            user.Configuration.Tokens = tokensChecked.Keys.Select(x => new TokenItem
            {
                Proxy = new TCS.Database.Models.Proxy
                {
                    Type = _tokens[x][0],
                    Host = _tokens[x][1],
                    Port = _tokens[x][2],
                    Credentials = new Proxy.UnSafeCredentials(_tokens[x][3], _tokens[x][4])
                },
                Token = x,
                Username = tokensChecked[x]
            }).ToList();
            await db.Tokens.AddRangeAsync(tokensChecked.Select(x => new TokenInfo
            {
                Token = x.Key,
                Username = x.Value
            }).Where(x => !db.Tokens.Any(y => x.Token == y.Token)));
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                message = tokensChecked.Count
            });
        }

        [HttpGet]
        [Route("gettokens")]
        public async Task<ActionResult> GetTokens(int id)
        {

            var tokens = (await db.Configurations
                .Where(x => x.Id == id)
                .Select(x => x.Tokens)
                .FirstAsync())
                .Select(x => $"{x.Token}:{x.Proxy.Type}:{x.Proxy.Host}:{x.Proxy.Port}:{x.Proxy.Credentials.Value.Username}:{x.Proxy.Credentials.Value.Password}");
            return Ok(tokens);
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

        [HttpPost]
        [Route("uploadfilter")]
        public async Task<ActionResult> UploadFilter([FromBody] List<string> words)
        {
            var set = words.ToImmutableHashSet();
            db.FilterWords.RemoveRange(db.FilterWords);
            await db.FilterWords.AddRangeAsync(set.Select(x => new FilterWord
            {
                Word = x
            }));
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("getfilter")]
        public async Task<ActionResult> GetFilter()
        {

            var words = await db.FilterWords
                .Select(x => x.Word)
                .ToListAsync();
            return Ok(words);
        }
    }
}
