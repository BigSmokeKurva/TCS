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
                var ctoken = new CancellationTokenSource();
                ctoken.CancelAfter(5000);
                await connection.ConnectAsync(new Uri($"wss://irc-ws.chat.twitch.tv:443"), ctoken.Token);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("CAP REQ :twitch.tv/tags twitch.tv/commands")), WebSocketMessageType.Text, true, CancellationToken.None);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + token)), WebSocketMessageType.Text, true, CancellationToken.None);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"NICK {username}")), WebSocketMessageType.Text, true, CancellationToken.None);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"USER {username} 8 *:{username}")), WebSocketMessageType.Text, true, CancellationToken.None);
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"JOIN #{streamerUsername}")), WebSocketMessageType.Text, true, CancellationToken.None);

            }
            public async Task Disconnect()
            {
                await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                connection.Dispose();
            }
            public async Task Send(string message)
            {
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"PRIVMSG #{streamerUsername} :{message}")), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        public class User
        {
            public int id { get; set; }
            private string streamerUsername;
            public Dictionary<string, Bot> bots = new();
            private Task task = null;
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
                    // TODO: remove and disconnect bots
                    await DisconnectAllBots();
                    users.Remove(id);
                }
                catch (TaskCanceledException ex)
                {
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex);
                }
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
    }
}
