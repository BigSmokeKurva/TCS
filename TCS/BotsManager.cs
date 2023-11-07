using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace TCS
{
    public class BotsManager
    {
        public class Bot
        {
            public string username { get; set; }
            public string token { get; set; }
            public string streamerUsername { get; set; }
            public ClientWebSocket connection { get; set; } = null;
            public ProxyCheck.Proxy proxy;
            public Bot(string username, string token, string streamerUsername, ProxyCheck.Proxy proxy)
            {
                this.username = username;
                this.token = token;
                this.streamerUsername = streamerUsername;
                this.proxy = proxy;
            }
            public async Task Connect()
            {
                connection?.Dispose();
                connection = new();
                connection.Options.Proxy = new WebProxy()
                {
                    Address = new Uri($"{proxy.Type}://{proxy.Host}:{proxy.Port}"),
                    Credentials = proxy.Credentials
                };
                await connection.ConnectAsync(new Uri($"wss://irc-ws.chat.twitch.tv:443"), new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("CAP REQ :twitch.tv/tags twitch.tv/commands")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + token)), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"NICK {username}")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"USER {username} 8 *:{username}")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"JOIN #{streamerUsername}")), WebSocketMessageType.Text, true, new CancellationTokenSource(5000).Token);

            }
            public async Task Disconnect()
            {
                try
                {
                    await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch { }
                connection.Dispose();
                connection = null;
            }
            public async Task Send(string message, CancellationTokenSource? ctoken = null)
            {
                ctoken ??= new CancellationTokenSource(5000);
                try
                {
                    await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"PRIVMSG #{streamerUsername} :{message}")), WebSocketMessageType.Text, true, ctoken.Token);
                }
                catch
                {
                    await Disconnect();
                    await Connect();
                    await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"PRIVMSG #{streamerUsername} :{message}")), WebSocketMessageType.Text, true, ctoken.Token);
                }
            }
        }
        public class User
        {
            public int id { get; set; }
            private string streamerUsername;
            public Dictionary<string, Bot> bots = new();
            private Task task = null;
            private List<Task> spamTasks = new();
            private CancellationTokenSource spamCancellationToken = null;
            private CancellationTokenSource cancellationToken = new();
            private static readonly Random rnd = new();
            public User(int id, string streamerUsername)
            {
                this.id = id;
                this.streamerUsername = streamerUsername;
            }
            public void UpdateTimer()
            {
                if (task is null)
                {
                    cancellationToken = new();
                    task = Timer();
                    return;
                }
                cancellationToken.Cancel();
                task.Wait();
                cancellationToken.Dispose();
                cancellationToken = new();
                task = Timer();
            }
            private async Task Timer()
            {
                try
                {
                    await Task.Delay(600000, cancellationToken.Token);
                    if (spamTasks.Any())
                    {
                        await Database.SharedArea.Log(id, "Остановил спам. (Бездействие)");
                        await StopSpam();
                    }
                    if (bots.Any())
                    {
                        await Database.SharedArea.Log(id, "Отключил всех ботов. (Бездействие)");
                        await Parallel.ForEachAsync(bots, new ParallelOptions() { MaxDegreeOfParallelism = Configuration.App.DisconnectThreads }, async (bot, e) =>
                        {
                            try
                            {
                                await bot.Value.Disconnect();
                            }
                            catch { }
                        });

                        bots.Clear();
                    }
                    users.Remove(id);
                }
                catch (TaskCanceledException ex)
                {
                }
                catch { }
            }
            internal async Task ConnectBot(string botname)
            {
                UpdateTimer();
                if (bots.ContainsKey(botname))
                {
                    return;
                }
                var token = await Database.ManageBotsArea.GetBotToken(id, botname) ?? throw new Exception("Bot token is null");
                var bot = new Bot(botname, token, streamerUsername, await Database.ManageBotsArea.GetProxy(id));
                await bot.Connect();
                bots.Add(botname, bot);
            }
            internal async Task DisconnectBot(string botname)
            {
                UpdateTimer();

                if (!bots.TryGetValue(botname, out var bot))
                    return;
                await bot.Disconnect();
                bots.Remove(botname);
            }
            internal async Task ConnectAllBots()
            {
                UpdateTimer();

                var bots = await Database.ManageBotsArea.GetBots(id);
                if (bots.Count == this.bots.Count)
                    return;
                var proxies = await Database.ManageBotsArea.GetProxies(id);
                ConcurrentDictionary<string, Bot> keyValuePairs = new();
                await Parallel.ForEachAsync(bots, new ParallelOptions() { MaxDegreeOfParallelism = Configuration.App.ConnectThreads }, async (bot, e) =>
                {
                    try
                    {
                        if (this.bots.ContainsKey(bot.Value))
                            return;
                        var _bot = new Bot(bot.Value, bot.Key, streamerUsername, proxies[rnd.Next(0, proxies.Length)]);
                        await _bot.Connect();
                        keyValuePairs.TryAdd(bot.Value, _bot);
                    }
                    catch
                    {
                    }
                });
                foreach (var pair in keyValuePairs)
                {
                    this.bots.Add(pair.Key, pair.Value);
                }
            }
            internal async Task DisconnectAllBots()
            {
                UpdateTimer();
                if (!bots.Any())
                    return;
                await Parallel.ForEachAsync(bots, new ParallelOptions() { MaxDegreeOfParallelism = Configuration.App.DisconnectThreads }, async (bot, e) =>
                {
                    try
                    {
                        await bot.Value.Disconnect();
                    }
                    catch { }
                });

                bots.Clear();
            }
            internal async Task Send(string botname, string message)
            {
                UpdateTimer();
                if (!bots.TryGetValue(botname, out var bot))
                    return;
                await bot.Send(message);
            }
            internal bool SpamStarted()
            {
                UpdateTimer();
                return spamTasks.Any();
            }
            internal async Task StopSpam()
            {
                spamCancellationToken?.Cancel();
                // ждем пока все потоки остановятся
                while (spamTasks.Any(x => x.Status == TaskStatus.Running))
                {
                    await Task.Delay(200);
                }
                spamCancellationToken?.Dispose();
                spamTasks.Clear();
                spamCancellationToken = null;
            }
            internal async Task StartSpam(int threads, int delay, string[] messages)
            {
                spamCancellationToken = new();
                for (int i = 0; i < threads; i++)
                {
                    spamTasks.Add(SpamThread(delay, messages));
                }
                //await Task.WhenAll(spamTasks);

            }
            internal async Task SpamThread(int delay, string[] messages)
            {
                delay *= 1000;
                while (!spamCancellationToken.IsCancellationRequested)
                {
                    if (!bots.Any())
                    {
                        await Task.Delay(delay, spamCancellationToken.Token);
                    }
                    try
                    {
                        var bot = bots.Values.ElementAt(rnd.Next(0, bots.Count));
                        await bot.Send(messages[rnd.Next(0, messages.Length)], spamCancellationToken);
                        await Task.Delay(delay, spamCancellationToken.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    catch { }
                }
            }
            internal async Task ChangeStreamerUsername(string streamerUsername)
            {
                UpdateTimer();
                if (SpamStarted())
                {
                    await StopSpam();
                }
                if (bots.Any())
                {
                    await DisconnectAllBots();
                }
                bots.Clear();
                this.streamerUsername = streamerUsername;
            }
            internal async Task Remove()
            {
                if (SpamStarted())
                {
                    await Database.SharedArea.Log(id, "Остановил спам. (Бездействие)");
                    await StopSpam();
                }
                if (bots.Any())
                {
                    await Database.SharedArea.Log(id, "Отключил всех ботов. (Бездействие)");
                    await DisconnectAllBots();
                }
                users.Remove(id);
            }
        }
        public static Dictionary<int, User> users = new();
        public static bool IsConnected(int id, string botUsername)
        {
            if (!users.TryGetValue(id, out var user))
                return false;

            return user.bots.ContainsKey(botUsername);
        }
        public static async Task UpdateTimer(int id)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                users[id].UpdateTimer();
                return;
            }

            user.UpdateTimer();
        }
        public async static Task ConnectBot(int id, string botUsername)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                await users[id].ConnectBot(botUsername);
                return;
            }

            await user.ConnectBot(botUsername);
        }
        public async static Task DisconnectBot(int id, string botUsername)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                users[id].UpdateTimer();
                return;
            }

            await user.DisconnectBot(botUsername);
        }
        public async static Task ConnectAllBots(int id)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                await users[id].ConnectAllBots();
                return;
            }

            await user.ConnectAllBots();
        }
        public async static Task DisconnectAllBots(int id)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                users[id].UpdateTimer();
                return;
            }

            await user.DisconnectAllBots();
        }
        public async static Task<bool> Send(int id, string botUsername, string message)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                users[id].UpdateTimer();
                return false;
            }

            await user.Send(botUsername, message);
            return true;
        }
        public static async Task<bool> SpamStarted(int id)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                users[id].UpdateTimer();
                return false;
            }
            return user.SpamStarted();
        }
        public static async Task StopSpam(int id)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                users[id].UpdateTimer();
                return;
            }
            await user.StopSpam();
        }
        public static async Task StartSpam(int id, int threads, int delay, string[] messages)
        {
            if (!users.TryGetValue(id, out var user))
            {
                var streamerUsername = await Database.AppArea.GetStreamerUsername(id);
                users.Add(id, new User(id, streamerUsername));
                user = users[id];
                user.UpdateTimer();
                await user.StartSpam(threads, delay, messages);
                return;
            }
            await user.StartSpam(threads, delay, messages);
        }
        public static async Task ChangeStreamerUsername(int id, string streamerUsername)
        {
            if (!users.TryGetValue(id, out var user))
            {
                users.Add(id, new User(id, streamerUsername));
                return;
            }
            await user.ChangeStreamerUsername(streamerUsername);
        }
        public static async Task Remove(int id)
        {
            if (!users.TryGetValue(id, out var user))
            {
                return;
            }
            await user.Remove();
        }
    }
}
