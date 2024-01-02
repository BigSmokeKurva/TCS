using System.Net;
using System.Net.WebSockets;
using System.Text;
using TCS.Database.Models;

namespace TCS.BotsManager
{
    public class Bot(string username, string token, string streamerUsername, Proxy proxy)
    {

        public string username { get; set; } = username;
        public string token { get; set; } = token;
        public string streamerUsername { get; set; } = streamerUsername;
        public ClientWebSocket connection { get; set; } = null;
        public Proxy proxy = proxy;

        public async Task Connect()
        {
            await Disconnect();
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
            try
            {
                connection.Dispose();
            }
            catch { }
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
                await Connect();
                await connection.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"PRIVMSG #{streamerUsername} :{message}")), WebSocketMessageType.Text, true, ctoken.Token);
            }
        }
    }

}
