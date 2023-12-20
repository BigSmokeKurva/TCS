using System.Net;

namespace TCS.Database.Models
{
    public struct Proxy
    {
        public struct UnSafeCredentials(string username, string password) : ICredentials
        {
            public string Username { get; set; } = username;
            public string Password { get; set; } = password;

            public readonly NetworkCredential GetCredential(Uri uri, string authType)
            {
                return new NetworkCredential(Username, Password);
            }
        }

        public string Type { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public UnSafeCredentials? Credentials { get; set; }
    }

    public class Configuration
    {
        public int Id { get; set; }

        // Key - Token , Value - Name
        public Dictionary<string, string> Tokens { get; set; } = [];

        public List<Proxy> Proxies { get; set; } = [];

        public string StreamerUsername { get; set; } = string.Empty;

        public int SpamThreads { get; set; } = 1;

        public int SpamDelay { get; set; } = 1;

        public List<string> SpamMessages { get; set; } = [];
        public Dictionary<string, List<string>> Binds { get; set; } = [];

        //public virtual User User { get; set; }
    }
}
