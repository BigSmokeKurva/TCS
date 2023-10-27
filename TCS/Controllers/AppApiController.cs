using Microsoft.AspNetCore.Mvc;
using TCS.Filters;

namespace TCS.Controllers
{
    [Route("api/app")]
    [ApiController]
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppApiController : Controller
    {
        [HttpPut]
        [Route("updateStreamerUsername")]
        public async Task<ActionResult> UpdateStreamerUsername(string username)
        {
            if (!UserValidators.ValidateStreamerUsername(username))
            {
                var data = new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                };
                return Ok(data);
            }
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            await Database.AppArea.UpdateStreamerUsername(id, username);
            return Ok();
        }

        [HttpGet]
        [Route("getBots")]
        public async Task<ActionResult> GetBots()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            var bots = await Database.AppArea.GetBotsNicks(id);
            var botsData = bots.ToDictionary(x => x, x => BotsManager.IsConnected(id, x));
            return Ok(botsData);
        }
    }
}
