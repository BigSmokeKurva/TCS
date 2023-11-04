using System.Text.Json;

namespace TCS
{
    public class Configuration
    {
        public class ConfigurationJson
        {
            public class DatabaseJson
            {
                public string Host { get; set; }
                public string Username { get; set; }
                public string Password { get; set; }
                public string DatabaseName { get; set; }
            }
            public class RootAccountJson
            {
                public string Password { get; set; }
            }
            public class TokenCheckJson
            {
                public int Threads { get; set; }
            }
            public class AppJson
            {
                public int ConnectThreads { get; set; }
                public int DisconnectThreads { get; set; }
            }
            public DatabaseJson Database { get; set; }
            public RootAccountJson RootAccount { get; set; }
            public TokenCheckJson TokenCheck { get; set; }
            public AppJson App { get; set; }
        }

        public static ConfigurationJson.DatabaseJson Database { get; set; }
        public static ConfigurationJson.RootAccountJson RootAccount { get; set; }
        public static ConfigurationJson.TokenCheckJson TokenCheck { get; set; }
        public static ConfigurationJson.AppJson App { get; set; }
        public static string PageTitle = "RBTChat";
        internal static void Init()
        {
            var config = JsonSerializer.Deserialize<ConfigurationJson>(File.ReadAllText("configuration.json"));
            Database = config.Database;
            RootAccount = config.RootAccount;
            TokenCheck = config.TokenCheck;
            App = config.App;

        }
    }
}
