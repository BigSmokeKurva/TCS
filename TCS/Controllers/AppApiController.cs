using Microsoft.AspNetCore.Mvc;
using TCS.Filters;

namespace TCS.Controllers
{
    [Route("api/app")]
    [ApiController]
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppApiController : Controller
    {
        public class ConnectBotModel
        {
            public string BotUsername { get; set; }
        }

        public class SendMessageModel
        {
            public string BotName { get; set; }
            public string Message { get; set; }
        }

        public class SpamConfigurationModel
        {
            public int Threads { get; set; }
            public int Delay { get; set; }
            public string[] Messages { get; set; }
        }


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
            try
            {
                await BotsManager.ChangeStreamerUsername(id, username);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            return Ok(new
            {
                status = "ok"
            });
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

        [HttpGet]
        [Route("ping")]
        public async Task<ActionResult> Ping()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            BotsManager.UpdateTimer(id);
            return Ok("pong");
        }

        [HttpPost]
        [Route("connectBot")]
        public async Task<ActionResult> ConnectBot(ConnectBotModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            try
            {
                await BotsManager.ConnectBot(id, model.BotUsername);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка подключения."
                });
            }
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("disconnectBot")]
        public async Task<ActionResult> DisconnectBot(ConnectBotModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            try
            {
                await BotsManager.DisconnectBot(id, model.BotUsername);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("connectAllBots")]
        public async Task<ActionResult> ConnectAllBots()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            try
            {
                await BotsManager.ConnectAllBots(id);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("disconnectAllBots")]
        public async Task<ActionResult> DisconnectAllBots()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            try
            {
                await BotsManager.DisconnectAllBots(id);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("sendMessage")]
        public async Task<ActionResult> SendMessage(SendMessageModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (!BotsManager.IsConnected(id, model.BotName))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не подключен."
                });
            }
            if (model.Message.Length > 1000)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение слишком длинное."
                });
            }
            try
            {
                var r = await BotsManager.Send(id, model.BotName, model.Message);
                if (r)
                {
                    return Ok(new
                    {
                        status = "ok"
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = "error",
                        message = "Ошибка отправки сообщения."
                    });
                }
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка отправки сообщения."
                });
            }
        }

        [HttpPost]
        [Route("updateSpamConfiguraion")]
        public async Task<ActionResult> UpdateSpamConfiguration(SpamConfigurationModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (await BotsManager.SpamStarted(id))
            {
                await BotsManager.StopSpam(id);
            }
            if (model.Delay > 500 || model.Delay < 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Задержка не может быть больше 500 с."
                });
            }
            if (model.Threads > 50 || model.Threads < 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Количество потоков не может быть больше 50."
                });
            }
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x))).ToArray();
            await Database.AppArea.UpdateSpamConfiguration(id, model);
            return Ok(new
            {
                status = "ok"
            });

        }

        [HttpPost]
        [Route("startSpam")]
        public async Task<ActionResult> StartSpam(SpamConfigurationModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (await BotsManager.SpamStarted(id))
            {
                await BotsManager.StopSpam(id);
            }
            if (model.Delay > 500 || model.Delay < 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Задержка не может быть больше 500 с."
                });
            }
            if (model.Threads > 50 || model.Threads < 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Количество потоков не может быть больше 50."
                });
            }
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x))).ToArray();
            await BotsManager.StartSpam(id, model.Threads, model.Delay, model.Messages);
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("stopSpam")]
        public async Task<ActionResult> StopSpam()
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (await BotsManager.SpamStarted(id))
            {
                await BotsManager.StopSpam(id);
            }
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}
