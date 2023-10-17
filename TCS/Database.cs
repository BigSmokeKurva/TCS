using Npgsql;
using NpgsqlTypes;
using TCS.Controllers;

namespace TCS
{
    public class Database
    {
        private static NpgsqlConnection connection;
        private static Timer timer;
        private static async void AutoCleanerSessions(object state)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM sessions WHERE expires < NOW();";
                await cmd.ExecuteNonQueryAsync();
            }
        }
        internal static async Task Init()
        {
            string[] tables =
            {
                // users
                "create table if not exists users (" +
                "id serial primary key," +
                "username varchar(50)," +
                "password varchar(50)," +
                "email varchar(50)," +
                "admin bool default false," +
                "unique(email, username)" +
                ");",
                // sessions
                "create table if not exists sessions (" +
                "auth_token uuid primary key default gen_random_uuid()," +
                "id integer," +
                "expires timestamp," +
                "unique(auth_token)" +
                ");",
                // logs
                "create table if not exists logs (" +
                "id serial," +
                "message text," +
                "time timestamp" +
                ");",
                // config
                "create table if not exists configuration (" +
                "id serial primary key," +
                "tokens jsonb default '{}'," +
                "streamerUsername varchar(50) default ''" +
                ");",
                // root
                $"insert into users (username, password, email, admin) values ('root', '{Configuration.RootAccount.Password}', 'root@root.com', true) on conflict(username, email) do update set password = '{Configuration.RootAccount.Password}';",
                $"insert into configuration (id) values (1) on conflict do nothing;"

            };
            using (var connection = new NpgsqlConnection($"Host={Configuration.Database.Host};Username={Configuration.Database.Username};Password={Configuration.Database.Password};Database=postgres"))
            {
                await connection.OpenAsync();
                // Существует ли база данных
                var databases = new List<string>();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select datname from pg_catalog.pg_database;";
                    //await cmd.ExecuteNonQueryAsync();
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        databases.Add(reader.GetString(0));
                }
                if (!databases.Contains(Configuration.Database.DatabaseName))
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"create database {Configuration.Database.DatabaseName};";
                        await cmd.ExecuteNonQueryAsync();
                    }
                await connection.CloseAsync();
            }
            // Создание таблиц по необходимости
            connection = new NpgsqlConnection($"Host={Configuration.Database.Host};Username={Configuration.Database.Username};Password={Configuration.Database.Password};Database={Configuration.Database.DatabaseName}");
            await connection.OpenAsync();
            foreach (var commandText in tables)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = commandText;
                await cmd.ExecuteNonQueryAsync();
            }
            timer = new Timer(AutoCleanerSessions, null, 0, 3600000);
        }
        internal static async Task<string> CheckBusy(RegistrationModel data)
        {
            string result;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
        SELECT
            CASE
                WHEN EXISTS (SELECT 1 FROM users WHERE username = @Login) THEN 'Данный логин уже используется.'
                WHEN EXISTS (SELECT 1 FROM users WHERE email = @Email) THEN 'Данная почта уже используется.'
                ELSE 'ОК'
            END AS result";

                cmd.Parameters.AddWithValue("@Login", data.Login);
                cmd.Parameters.AddWithValue("@Email", data.Email);
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                result = reader.GetString(0);
            }
            return result;

        }
        internal static async Task<int> AddUser(RegistrationModel data)
        {
            int id;
            using (var cmd = connection.CreateCommand())
            {
                //cmd.CommandText = "INSERT INTO users (username, password, email) VALUES (@Username, @Password, @Email) RETURNING id;";
                cmd.CommandText =
                    """
                    WITH inserted_user AS (
                        INSERT INTO users (username, password, email)
                        VALUES (@Username, @Password, @Email)
                        RETURNING id
                    )
                    INSERT INTO configuration (id)
                    SELECT id
                    FROM inserted_user
                    returning id;
                    """;

                cmd.Parameters.AddWithValue("@Username", data.Login);
                cmd.Parameters.AddWithValue("@Password", data.Password);
                cmd.Parameters.AddWithValue("@Email", data.Email);

                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                id = reader.GetInt32(0);
            }
            return id;
        }
        internal static async Task<string> CreateSession(int id)
        {
            string auth_token;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO sessions (id, expires) VALUES (@Id, NOW() + INTERVAL '30 days') RETURNING auth_token;";
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                auth_token = reader.GetGuid(0).ToString();
            }
            return auth_token;
        }
        internal static async Task<bool> IsValidAuthToken(string auth_token)
        {
            bool isValid;
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    // TODO
                    cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM sessions WHERE auth_token = @auth_token::uuid) AS exists_token;";
                    cmd.Parameters.AddWithValue("@auth_token", NpgsqlDbType.Uuid, Guid.Parse(auth_token));
                    using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    isValid = reader.GetBoolean(0);
                }
            }
            catch
            {
                isValid = false;
            }
            return isValid;
        }
        internal static async Task<int> CheckUser(AuthorizationModel data)
        {
            /// Возвращает -1, если пользователя нет
            int userId = -1;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM users WHERE username = @Login AND password = @Password LIMIT 1;";
                cmd.Parameters.AddWithValue("@Login", data.Login);
                cmd.Parameters.AddWithValue("@Password", data.Password);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    userId = reader.GetInt32(0);
                }
            }
            return userId;
        }
        internal static async Task Log(int id, string message)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO logs (id, message, time) VALUES (@id, @Message, NOW());";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@Message", message);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        internal static async Task<int> GetId(string auth_token)
        {
            int id;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM sessions WHERE auth_token = @auth_token::uuid limit 1;";
                cmd.Parameters.AddWithValue("@auth_token", NpgsqlDbType.Uuid, Guid.Parse(auth_token));
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                id = reader.GetInt32(0);
            }
            return id;
        }
        internal static async Task<string> GetUsername(int id)
        {
            string username;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT username FROM users WHERE id = @id limit 1;";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                username = reader.GetString(0);
            }
            return username;
        }
        internal static async Task<int> GetCountTokens(int id)
        {
            int count;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    """
                    SELECT count(*) AS dictionary_length
                    FROM (
                      SELECT jsonb_object_keys(tokens) AS key
                      FROM configuration
                      WHERE id = @id
                    ) AS subquery limit 1;
                    """;
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                count = reader.GetInt32(0);
            }
            return count;
        }
        internal static async Task<string> GetStreamerUsername(int id)
        {
            string username;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT streamerUsername FROM configuration WHERE id = @id limit 1;";
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                username = reader.GetString(0);
            }
            return username;
        }
        internal static async Task DeleteAuthToken(string auth_token)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM sessions WHERE auth_token = @auth_token::uuid;";
                cmd.Parameters.AddWithValue("@auth_token", NpgsqlDbType.Uuid, Guid.Parse(auth_token));
                await cmd.ExecuteNonQueryAsync();
            }
        }

    }
}
