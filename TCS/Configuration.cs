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
            public DatabaseJson Database { get; set; }
            public RootAccountJson RootAccount { get; set; }
        }

        public static ConfigurationJson.DatabaseJson Database { get; set; }
        public static ConfigurationJson.RootAccountJson RootAccount { get; set; }
        internal static void Init()
        {
            var config = JsonSerializer.Deserialize<ConfigurationJson>(File.ReadAllText("configuration.json"));
            Database = config.Database;
            RootAccount = config.RootAccount;
        }
    }
}
