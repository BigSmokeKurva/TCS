using Microsoft.AspNetCore.Mvc;
using TCS.Filters;

namespace TCS.Controllers
{
    [Route("api/app")]
    [ApiController]
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppApiController : Controller
    {
        private static readonly Random rnd = new();
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

        public class BindModel
        {
            public string Name { get; set; }
            public string[] Messages { get; set; }
        }

        public class EditBindModel
        {
            public string Name { get; set; }
            public string[] Messages { get; set; }
            public string OldName { get; set; }
        }

        public class SendBindMessageModel
        {
            public string bindname { get; set; }
            public string botname { get; set; }
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
            await Database.SharedArea.Log(id, $"Обновил ник стримера на {username}.");
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
            await Database.SharedArea.Log(id, $"Подключил бота {model.BotUsername}.");
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
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            await Database.SharedArea.Log(id, $"Отключил бота {model.BotUsername}.");
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
            await Database.SharedArea.Log(id, $"Подключил всех ботов.");
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
            await Database.SharedArea.Log(id, $"Отключил всех ботов.");
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
                    await Database.SharedArea.Log(id, $"Отправил сообщение {model.Message}.");
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
            await Database.SharedArea.Log(id, $"Обновил конфигурацию спама.");
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
            await Database.SharedArea.Log(id, $"Запустил спам.");
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
            await Database.SharedArea.Log(id, $"Остановил спам.");
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("addBind")]
        public async Task<ActionResult> AddBind(BindModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x))).ToArray();
            if (model.Messages.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Список сообщений не может быть пустым."
                });
            }
            model.Name = model.Name.Trim();
            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrWhiteSpace(model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Название не может быть пустым."
                });
            }
            var r = await Database.AppArea.AddBind(id, model);
            if (r)
            {
                await Database.SharedArea.Log(id, $"Добавил бинд {model.Name}.");
                return Ok(new
                {
                    status = "ok",
                    bindName = model.Name
                });
            }
            else
            {
                return Ok(new
                {
                    status = "error",
                    message = "Невозможно добавить. Возможно бинд с таким названием уже существует."
                });
            }
        }

        [HttpGet]
        [Route("getBindMessages")]
        public async Task<ActionResult> GetBindMessages(string bindName)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (await Database.AppArea.BindExists(id, bindName))
            {
                var messages = await Database.AppArea.GetBindMessages(id, bindName);
                return Ok(new
                {
                    status = "ok",
                    messages
                });
            }
            return Ok(new
            {
                status = "error",
                message = "Бинд не найден."
            });
        }

        [HttpPost]
        [Route("editBind")]
        public async Task<ActionResult> EditBind(EditBindModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x))).ToArray();
            if (model.Messages.Length == 0)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Список сообщений не может быть пустым."
                });
            }
            model.Name = model.Name.Trim();
            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrWhiteSpace(model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Название не может быть пустым."
                });
            }
            if (model.Name == model.OldName)
            {
                await Database.AppArea.ReplaceBind(id, model);
                await Database.SharedArea.Log(id, $"Обновил бинд {model.Name}.");
                return Ok(new
                {
                    status = "ok",
                    name = model.Name,
                    messages = model.Messages
                });
            }
            if (await Database.AppArea.BindExists(id, model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинда с таким названием уже существует."
                });
            }
            await Database.AppArea.EditBind(id, model);
            await Database.SharedArea.Log(id, $"Обновил бинд {model.Name}.");
            return Ok(new
            {
                status = "ok",
                name = model.Name,
                messages = model.Messages
            });
        }

        [HttpDelete]
        [Route("deleteBind")]
        public async Task<ActionResult> DeleteBind(string bindName)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (await Database.AppArea.BindExists(id, bindName))
            {
                await Database.AppArea.DeleteBind(id, bindName);
                await Database.SharedArea.Log(id, $"Удалил бинд {bindName}.");
                return Ok(new
                {
                    status = "ok"
                });
            }
            return Ok(new
            {
                status = "error",
                message = "Бинд не найден."
            });
        }

        [HttpPost]
        [Route("sendBindMessage")]
        public async Task<ActionResult> SendBindMessage(SendBindMessageModel model)
        {
            var auth_token = Request.Headers.Authorization.ToString();
            var id = await Database.SharedArea.GetId(auth_token);
            if (await Database.AppArea.BindExists(id, model.bindname))
            {
                if (BotsManager.IsConnected(id, model.botname))
                {
                    var messages = await Database.AppArea.GetBindMessages(id, model.bindname);
                    var message = messages[rnd.Next(0, messages.Length)];
                    await BotsManager.Send(id, model.botname, message);
                    await Database.SharedArea.Log(id, $"Отправил сообщение {message} из бинда {model.bindname}.");
                    return Ok(new
                    {
                        status = "ok"
                    });
                }
                return Ok(new
                {
                    status = "error",
                    message = "Бот не подключен."
                });
            }
            return Ok(new
            {
                status = "error",
                message = "Бинд не найден."
            });
        }
    }
}
