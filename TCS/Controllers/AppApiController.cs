using Microsoft.AspNetCore.Mvc;

namespace TCS.Controllers
{
    [Route("api/app")]
    [ApiController]
    public class AppApiController : ControllerBase
    {
        private async Task<bool> IsValidSession()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            return auth_token is not null && await Database.IsValidAuthToken(auth_token);
        }

        [HttpPut]
        [Route("updateStreamerUsername")]
        public async Task<ActionResult> UpdateStreamerUsername(string username)
        {
            if (!await IsValidSession())
            {
                return Unauthorized();
            }
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
            var id = await Database.GetId(auth_token);
            await Database.AppArea.UpdateStreamerUsername(id, username);
            return Ok();
        }
    }
}
