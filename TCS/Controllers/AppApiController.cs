using Microsoft.AspNetCore.Mvc;
using System.Collections.Frozen;
using TCS.BotsManager;
using TCS.Controllers.Models;
using TCS.Database;
using TCS.Filters;

namespace TCS.Controllers
{
    [Route("api/app")]
    [ApiController]
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class AppApiController(DatabaseContext db) : ControllerBase
    {
        private static readonly Random rnd = new();
        private readonly DatabaseContext db = db;

        [HttpPut]
        [Route("updateStreamerUsername")]
        public async Task<ActionResult> UpdateStreamerUsername(string username)
        {
            // TODO не работает
            if (!UserValidators.ValidateStreamerUsername(username))
            {
                var data = new
                {
                    status = "error",
                    message = "Ошибка валидации данных."
                };
                return Ok(data);
            }
            var auth_token = Guid.Parse(Request.Headers.Authorization);

            var user = await db.GetUser(auth_token);
            user.Configuration.StreamerUsername = username;

            try
            {
                await Manager.ChangeStreamerUsername(user.Id, username);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            await db.AddLog(user, $"Обновил ник стримера на {username}.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("getBots")]
        public async Task<ActionResult> GetBots()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var bots = user.Configuration.Tokens.Values.ToFrozenDictionary(x => x, x => Manager.IsConnected(user.Id, x));
            return Ok(bots);
        }

        [HttpGet]
        [Route("ping")]
        public async Task<ActionResult> Ping()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            Manager.UpdateTimer(id, db);
            return Ok("pong");
        }

        [HttpPost]
        [Route("connectBot")]
        public async Task<ActionResult> ConnectBot(ConnectBotModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            try
            {
                await Manager.ConnectBot(id, model.BotUsername, db);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка подключения."
                });
            }
            await db.AddLog(id, $"Подключил бота {model.BotUsername}.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("disconnectBot")]
        public async Task<ActionResult> DisconnectBot(ConnectBotModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            try
            {
                await Manager.DisconnectBot(id, model.BotUsername, db);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            await db.AddLog(id, $"Отключил бота {model.BotUsername}.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("connectAllBots")]
        public async Task<ActionResult> ConnectAllBots()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            try
            {
                await Manager.ConnectAllBots(id, db);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            await db.AddLog(id, $"Подключил всех ботов.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("disconnectAllBots")]
        public async Task<ActionResult> DisconnectAllBots()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            try
            {
                await Manager.DisconnectAllBots(id, db);
            }
            catch
            {
                return Ok(new
                {
                    status = "error",
                    message = ""
                });
            }
            await db.AddLog(id, $"Отключил всех ботов.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("sendMessage")]
        public async Task<ActionResult> SendMessage(SendMessageModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (!Manager.IsConnected(id, model.BotName))
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
                var r = await Manager.Send(id, model.BotName, model.Message, db);
                if (r)
                {
                    await db.AddLog(id, $"Отправил сообщение {model.Message}.");
                    await db.SaveChangesAsync();
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                await Manager.StopSpam(id, db);
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
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x) || x.Length > 49)).ToArray();
            var configuration = await db.Configurations.FindAsync(id);
            configuration.SpamThreads = model.Threads;
            configuration.SpamDelay = model.Delay;
            configuration.SpamMessages = [.. model.Messages];
            await db.AddLog(id, $"Обновил конфигурацию спама.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });

        }

        [HttpPost]
        [Route("startSpam")]
        public async Task<ActionResult> StartSpam(SpamConfigurationModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                await Manager.StopSpam(id, db);
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
            await Manager.StartSpam(id, model.Threads, model.Delay, model.Messages, db);
            await db.AddLog(id, $"Запустил спам.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("stopSpam")]
        public async Task<ActionResult> StopSpam()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            if (await Manager.SpamStarted(id, db))
            {
                await Manager.StopSpam(id, db);
            }
            await db.AddLog(id, $"Остановил спам.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("addBind")]
        public async Task<ActionResult> AddBind(BindModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
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
            if (user.Configuration.Binds.ContainsKey(model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Невозможно добавить. Возможно бинд с таким названием уже существует."
                });
            }
            //user.Configuration.Id = user.Id;
            user.Configuration.Binds.Add(model.Name, [.. model.Messages]);
            db.Entry(user.Configuration).Property(x => x.Binds).IsModified = true;
            await db.AddLog(user, $"Добавил бинд {model.Name}.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
                bindName = model.Name
            });

        }

        [HttpGet]
        [Route("getBindMessages")]
        public async Task<ActionResult> GetBindMessages(string bindName)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            if (configuration.Binds.TryGetValue(bindName, out List<string>? value))
            {
                return Ok(new
                {
                    status = "ok",
                    messages = value
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
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
                //user.Configuration.Id = user.Id;
                user.Configuration.Binds[model.Name] = [.. model.Messages];
                db.Entry(user.Configuration).Property(x => x.Binds).IsModified = true;
                await db.AddLog(user, $"Обновил бинд {model.Name}.");
                await db.SaveChangesAsync();
                return Ok(new
                {
                    status = "ok",
                    name = model.Name,
                    messages = model.Messages
                });
            }
            if (user.Configuration.Binds.ContainsKey(model.Name))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинда с таким названием уже существует."
                });
            }
            //user.Configuration.Id = user.Id;
            user.Configuration.Binds.Remove(model.OldName);
            user.Configuration.Binds.Add(model.Name, [.. model.Messages]);
            db.Entry(user.Configuration).Property(x => x.Binds).IsModified = true;
            await db.AddLog(user, $"Обновил бинд {model.OldName} -> {model.Name}.");
            await db.SaveChangesAsync();
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            if (configuration.Binds.Remove(bindName))
            {
                db.Entry(configuration).Property(x => x.Binds).IsModified = true;
                await db.AddLog(configuration.Id, $"Удалил бинд {bindName}.");
                await db.SaveChangesAsync();
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

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            if (!configuration.Binds.TryGetValue(model.bindname, out List<string>? messages))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бинд не найден."
                });
            }
            if (!Manager.IsConnected(configuration.Id, model.botname))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не подключен."
                });
            }
            var message = messages[rnd.Next(0, messages.Count)];
            await Manager.Send(configuration.Id, model.botname, message, db);
            await db.AddLog(configuration.Id, $"Отправил сообщение {message} из бинда {model.bindname}.");
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });

        }
    }
}
