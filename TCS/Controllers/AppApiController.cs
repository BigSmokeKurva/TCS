using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Frozen;
using TCS.BotsManager;
using TCS.Controllers.Models;
using TCS.Database;
using TCS.Filters;
using TCS.Follow;

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
            await FollowBot.RemoveAllFromQueue(x => x.Id == user.Id);
            await db.AddLog(user, $"Обновил ник стримера на {username}.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            FollowBot.Queue.RemoveAll(x => x.Id == user.Id);
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
            var bots = user.Configuration.Tokens.Select(x => x.Username).ToFrozenDictionary(x => x, x => Manager.IsConnected(user.Id, x));
            return Ok(bots);
        }

        [HttpGet]
        [Route("ping")]
        public async Task<ActionResult> Ping()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var id = await db.GetId(auth_token);
            await Manager.UpdateTimer(id, db);
            return Ok(new
            {
                status = "ok",
                message = "pong"
            });
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
            await db.AddLog(id, $"Подключил бота {model.BotUsername}.", Database.Models.LogType.Action);
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
            await db.AddLog(id, $"Отключил бота {model.BotUsername}.", Database.Models.LogType.Action);
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
            await db.AddLog(id, $"Подключил всех ботов.", Database.Models.LogType.Action);
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
            await db.AddLog(id, $"Отключил всех ботов.", Database.Models.LogType.Action);
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
            if (await db.CheckMessageFilter(model.Message))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }
            try
            {
                var r = await Manager.Send(id, model.BotName, model.Message, db);
                if (r)
                {
                    await db.AddLog(id, $"Отправил сообщение {model.Message}.", Database.Models.LogType.Chat);
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
            if (await db.CheckMessageFilter(model.Messages))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }
            var configuration = await db.Configurations.FindAsync(id);
            configuration.SpamThreads = model.Threads;
            configuration.SpamDelay = model.Delay;
            configuration.SpamMessages = [.. model.Messages];
            await db.AddLog(id, $"Обновил конфигурацию спама.", Database.Models.LogType.Action);
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
                    message = $"Количество {(model.Mode == SpamMode.Random ? "потоков" : "ботов")} не может быть больше 50."
                });
            }
            if (Manager.users[id].bots.Count < model.Threads)
            {
                return Ok(new
                {
                    status = "error",
                    message = $"Количество {(model.Mode == SpamMode.Random ? "потоков" : "ботов")} не может быть больше количества подключенных ботов."
                });
            }
            model.Messages = model.Messages.Select(x => x.Trim()).Where(x => !(string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x))).ToArray();
            if (await db.CheckMessageFilter(model.Messages))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }
            await Manager.StartSpam(id, model.Threads, model.Delay, model.Messages, model.Mode, db);
            await db.AddLog(id, $"Запустил спам.", Database.Models.LogType.Action);
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
            await db.AddLog(id, $"Остановил спам.", Database.Models.LogType.Action);
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
            await db.AddLog(user, $"Добавил бинд {model.Name}.", Database.Models.LogType.Action);
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
                await db.AddLog(user, $"Обновил бинд {model.Name}.", Database.Models.LogType.Action);
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
            await db.AddLog(user, $"Обновил бинд {model.OldName} -> {model.Name}.", Database.Models.LogType.Action);
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
                await db.AddLog(configuration.Id, $"Удалил бинд {bindName}.", Database.Models.LogType.Action);
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
            if (await db.CheckMessageFilter(message))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Сообщение содержит запрещенные слова."
                });
            }
            var r = await Manager.Send(configuration.Id, model.botname, message, db);
            if (!r)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Ошибка отправки сообщения."
                });
            }
            await db.AddLog(configuration.Id, $"Отправил сообщение {message} из бинда {model.bindname}.", Database.Models.LogType.Chat);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("getFollowBots")]
        public async Task<ActionResult> GetFollowBots()
        {
            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var configuration = await db.GetConfiguration(auth_token);
            var followedUsernames = configuration.Tokens.Where(x => db.Bots.Any(y => y.Username == x.Username && y.Followed.Contains(configuration.StreamerUsername))).Select(x => x.Username);
            var inQueueTokens = await FollowBot.IsInQueue(configuration.Tokens.Select(x => x.Token), configuration.Id);
            return Ok(configuration.Tokens.ToDictionary(x => x.Username, x =>
            {
                if (inQueueTokens.Contains(x.Token))
                {
                    return "waiting";
                }
                if (followedUsernames.Contains(x.Username))
                {
                    return "followed";
                }
                return "not-followed";
            }));
        }

        [HttpGet]
        [Route("followBot")]
        public async Task<ActionResult> FollowBot_(string botname)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botname);
            if (token is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не найден."
                });
            }
            if (await FollowBot.IsInQueue(token.Token, user.Id))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот уже в очереди."
                });
            }

            var item = new Item
            {
                Id = user.Id,
                Username = token.Username,
                Token = token.Token,
                TargetId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername),
                Channel = user.Configuration.StreamerUsername,
                Action = Actions.Follow,
                Date = TimeHelper.GetUnspecifiedUtc(),
                Proxy = token.Proxy
            };
            await FollowBot.AddToQueue(item);
            await db.AddLog(user, $"Добавил бота {botname} в очередь на follow.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("unfollowBot")]
        public async Task<ActionResult> UnfollowBot(string botname)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botname);
            if (token is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не найден."
                });
            }
            if (await FollowBot.IsInQueue(token.Token, user.Id))
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот уже в очереди."
                });
            }
            var item = new Item
            {
                Id = user.Id,
                Username = token.Username,
                Token = token.Token,
                TargetId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername),
                Channel = user.Configuration.StreamerUsername,
                Action = Actions.Unfollow,
                Date = TimeHelper.GetUnspecifiedUtc(),
                Proxy = token.Proxy
            };
            await FollowBot.AddToQueue(item);
            await db.AddLog(user, $"Добавил бота {botname} в очередь на unfollow.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("followBotCancel")]
        public async Task<ActionResult> FollowBotCancel(string botname)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var token = user.Configuration.Tokens.FirstOrDefault(x => x.Username == botname);
            if (token is null)
            {
                return Ok(new
                {
                    status = "error",
                    message = "Бот не найден."
                });
            }
            await FollowBot.RemoveFromQueue(token.Username, user.Id);
            await db.AddLog(user, $"Убрал из очереди бота {botname}.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();

            return Ok(new
            {
                status = "ok",
                message = (await db.Bots.Where(x => x.Username == botname).Select(x => x.Followed).FirstAsync()).Any(x => x.Contains(user.Configuration.StreamerUsername)) ?
                "followed" : "not-followed"
            });
        }

        [HttpPost]
        [Route("followAllBots")]
        public async Task<ActionResult> FollowAllBots([FromBody] FollowAllBotsModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var tokens = user.Configuration.Tokens.Select(x => x.Token);
            var bots = user.Configuration.Tokens.Select(x => x.Username);
            var channelId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername);
            var inQueueTokens = await FollowBot.IsInQueue(tokens, user.Id);
            var followedTokens = (await db.Bots.Where(x => x.Followed.Contains(user.Configuration.StreamerUsername) && bots.Contains(x.Username)).Select(x => x.Username).ToListAsync())
                .Select(x => user.Configuration.Tokens.First(y => x == y.Username).Token);
            var items = new List<Item>();
            var num = 1;
            foreach (var token in tokens)
            {
                if (inQueueTokens.Contains(token) || followedTokens.Contains(token))
                {
                    continue;
                }
                items.Add(new Item
                {
                    Id = user.Id,
                    Username = user.Configuration.Tokens.First(x => x.Token == token).Username,
                    Token = token,
                    TargetId = channelId,
                    Channel = user.Configuration.StreamerUsername,
                    Action = Actions.Follow,
                    Date = TimeHelper.GetUnspecifiedUtc().AddSeconds(model.Delay * num),
                    Proxy = user.Configuration.Tokens.First(y => y.Token == token).Proxy
                });
                num++;
            }
            await FollowBot.AddToQueue(items);
            await db.AddLog(user, $"Добавил всех ботов в очередь на follow.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpPost]
        [Route("unfollowAllBots")]
        public async Task<ActionResult> UnfollowAllBots([FromBody] FollowAllBotsModel model)
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            var tokens = user.Configuration.Tokens.Select(x => x.Token);
            var bots = user.Configuration.Tokens.Select(x => x.Username);
            var channelId = await FollowBot.GetChannelId(user.Configuration.StreamerUsername);
            var inQueueTokens = await FollowBot.IsInQueue(tokens, user.Id);
            var followedTokens = (await db.Bots.Where(x => x.Followed.Contains(user.Configuration.StreamerUsername) && bots.Contains(x.Username)).Select(x => x.Username).ToListAsync())
                .Select(x => user.Configuration.Tokens.First(y => x == y.Username).Token);
            var items = new List<Item>();
            var num = 1;
            foreach (var token in tokens)
            {
                if (inQueueTokens.Contains(token) || !followedTokens.Contains(token))
                {
                    continue;
                }
                items.Add(new Item
                {
                    Id = user.Id,
                    Username = user.Configuration.Tokens.First(x => x.Token == token).Username,
                    Token = token,
                    TargetId = channelId,
                    Channel = user.Configuration.StreamerUsername,
                    Action = Actions.Unfollow,
                    Date = TimeHelper.GetUnspecifiedUtc().AddSeconds(model.Delay * num),
                    Proxy = user.Configuration.Tokens.First(y => y.Token == token).Proxy
                });
                num++;
            }
            await FollowBot.AddToQueue(items);
            await db.AddLog(user, $"Добавил всех ботов в очередь на unfollow.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok"
            });
        }

        [HttpGet]
        [Route("followAllBotsCancel")]
        public async Task<ActionResult> FollowAllBotsCancel()
        {

            var auth_token = Guid.Parse(Request.Headers.Authorization);
            var user = await db.GetUser(auth_token);
            await FollowBot.RemoveAllFromQueue(x => x.Id == user.Id);
            await db.AddLog(user, $"Убрал всех ботов из очереди followbot.", Database.Models.LogType.Action);
            await db.SaveChangesAsync();
            return Ok(new
            {
                status = "ok",
            });
        }
    }
}
