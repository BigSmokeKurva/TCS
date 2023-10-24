using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TCS.Controllers
{

    [Route("api/admin")]
    [ApiController]
    public class AdminPanelApiController : ControllerBase
    {
        public enum ChangeType
        {
            Username = 0,
            Password = 1,
            Email = 2,
            Admin = 3,
            Tokens = 4,
            Proxies = 5
        }

        public class EditUserModel
        {
            public int Id { get; set; }
            public ChangeType Property { get; set; }
            public JsonElement Value { get; set; }
        }

        private async Task<bool> IsAdmin()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            return auth_token is not null && await Database.IsValidAuthToken(auth_token) && await Database.IsAdmin(await Database.GetId(auth_token));
        }

        [HttpGet]
        [Route("getusers")]
        public async Task<ActionResult> GetUsers()
        {
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            return Ok(await Database.AdminArea.GetUsers());
        }

        [HttpGet]
        [Route("getuserinfo")]
        public async Task<ActionResult> GetUserInfo(int id)
        {
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            return Ok(await Database.AdminArea.GetUserInfo(id));
        }

        [HttpGet]
        [Route("getlogs")]
        public async Task<ActionResult> GetLogs(int id, string time)
        {
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            return Ok(await Database.AdminArea.GetLogs(id, time));
        }

        [HttpPost]
        [Route("edituser")]
        public async Task<ActionResult> EditUser([FromBody] EditUserModel model)
        {
            string result;
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            switch (model.Property)
            {
                case ChangeType.Username:
                    if (!UserValidators.ValidateLogin(model.Value.ToString()))
                    {
                        return Ok(new
                        {
                            status = "error",
                            message = "Ошибка валидации данных."
                        });
                    }
                    result = await Database.AdminArea.ChangeUsername(model.Id, model.Value.ToString());
                    if (result != "OK")
                    {
                        var data = new
                        {
                            status = "error",
                            message = result
                        };
                        return Ok(data);
                    }
                    break;
                case ChangeType.Password:
                    if (!UserValidators.ValidatePassword(model.Value.GetString()))
                    {
                        return Ok(new
                        {
                            status = "error",
                            message = "Ошибка валидации данных."
                        });
                    }
                    await Database.AdminArea.ChangePassword(model.Id, model.Value.GetString());
                    break;
                case ChangeType.Email:
                    if (!UserValidators.ValidateEmail(model.Value.ToString()))
                    {
                        return Ok(new
                        {
                            status = "error",
                            message = "Ошибка валидации данных."
                        });
                    }
                    result = await Database.AdminArea.ChangeEmail(model.Id, model.Value.GetString());
                    if (result != "OK")
                    {
                        var data = new
                        {
                            status = "error",
                            message = result
                        };
                        return Ok(data);
                    }
                    break;
                case ChangeType.Admin:
                    await Database.AdminArea.ChangeAdmin(model.Id, model.Value.GetBoolean());
                    break;
                case ChangeType.Tokens:
                    var tokensChecked = await TokenCheck.Check(model.Value.EnumerateArray().Select(x => x.GetString()));
                    await Database.AdminArea.ChangeTokens(model.Id, tokensChecked);
                    return Ok(new
                    {
                        status = "ok",
                        message = tokensChecked.Count
                    });
                case ChangeType.Proxies:
                    var proxies = ProxyCheck.Parse(model.Value.EnumerateArray().Select(x => x.GetString()));
                    await Database.AdminArea.ChangeProxies(model.Id, proxies);
                    return Ok(new
                    {
                        status = "ok",
                        message = proxies.Count
                    });
            }
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("gettokens")]
        public async Task<ActionResult> GetTokens(int id)
        {
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            return Ok(await Database.AdminArea.GetTokens(id));
        }

        [HttpGet]
        [Route("getproxies")]
        public async Task<ActionResult> GetProxies(int id)
        {
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            var proxies = await Database.AdminArea.GetProxies(id);
            return Ok(ProxyCheck.ProxyToString(proxies));
        }

        [HttpGet]
        [Route("deleteuser")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            if (!await IsAdmin())
            {
                return Unauthorized();
            }
            await Database.AdminArea.DeleteUser(id);
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}
