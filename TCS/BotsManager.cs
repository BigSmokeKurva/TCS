using System.Net.WebSockets;

namespace TCS
{
    public class BotsManager
    {
        public class Bot
        {
            public string username { get; set; }
            public string token { get; set; }
            public ClientWebSocket connection { get; set; } = null;
        }
        public class User
        {
            public int id { get; set; }
            public List<Bot> bots { get; set; }
        }
        public static List<User> users = new List<User>();
        public static bool IsConnected(int id, string botUsername)
        {
            var user = users.FirstOrDefault(x => x.id == id);

            if (user is null)
                return false;

            var bot = user.bots.FirstOrDefault(x => x.username == botUsername);

            return bot is not null;
        }
    }
}
