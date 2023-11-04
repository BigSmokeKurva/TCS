using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json;
using TCS.Filters;

namespace TCS.Controllers
{

    [Route("api/admin")]
    [ApiController]
    [TypeFilter(typeof(AdminAuthorizationFilter))]
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

        [HttpGet]
        [Route("getusers")]
        public async Task<ActionResult> GetUsers()
        {
            //if (!await IsAdmin())
            //{
            //    return Unauthorized();
            //}
            return Ok(await Database.AdminArea.GetUsers());
        }

        [HttpGet]
        [Route("getuserinfo")]
        public async Task<ActionResult> GetUserInfo(int id)
        {
            return Ok(await Database.AdminArea.GetUserInfo(id));
        }

        [HttpGet]
        [Route("getlogs")]
        public async Task<ActionResult> GetLogs(int id, string time)
        {
            return Ok(await Database.AdminArea.GetLogs(id, time));
        }

        [HttpPost]
        [Route("edituser")]
        public async Task<ActionResult> EditUser([FromBody] EditUserModel model)
        {
            string result;
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
                    var tokens = model.Value.EnumerateArray().Select(x => x.GetString()).Distinct();
                    var tokensChecked = (await TokenCheck.Check(tokens: tokens));
                    await Database.AdminArea.ChangeTokens(model.Id, tokensChecked);
                    await BotsManager.StopSpam(model.Id);
                    await BotsManager.DisconnectAllBots(model.Id);
                    return Ok(new
                    {
                        status = "ok",
                        message = tokensChecked.Count
                    });
                case ChangeType.Proxies:
                    var proxies = ProxyCheck.Parse(model.Value.EnumerateArray().Select(x => x.GetString()));
                    await Database.AdminArea.ChangeProxies(model.Id, proxies);
                    await BotsManager.StopSpam(model.Id);
                    await BotsManager.DisconnectAllBots(model.Id);
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
            return Ok(await Database.AdminArea.GetTokens(id));
        }

        [HttpGet]
        [Route("getproxies")]
        public async Task<ActionResult> GetProxies(int id)
        {
            var proxies = await Database.AdminArea.GetProxies(id);
            return Ok(ProxyCheck.ProxyToString(proxies));
        }

        [HttpDelete]
        [Route("deleteuser")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            await Database.AdminArea.DeleteUser(id);
            await BotsManager.Remove(id);
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}
