using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text;
using TCS.Controllers;
using static TCS.ProxyCheck;

namespace TCS
{
    public class Database
    {
        private static string connectionString;
        private static Timer timer;
        private static async void AutoCleanerSessions(object state)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var cmd = connection.CreateCommand())
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
                """
                create table if not exists users (
                    id serial primary key,
                    username varchar(50),
                    password varchar(50),
                    email varchar(50),
                    admin bool default false,
                    unique(email, username)
                );
                """,
                // sessions
                """
                create table if not exists sessions (
                    auth_token uuid primary key default gen_random_uuid(),
                    id integer,
                    expires timestamp,
                    unique(auth_token)
                );
                """,
                // logs
                """
                create table if not exists logs (
                    id serial,
                    message text,
                    time timestamp
                );
                """,
                // config
                """
                create table if not exists configuration (
                    id serial primary key,
                    tokens jsonb default '{}',
                    proxies jsonb default '[]',
                    streamerUsername varchar(50) default '',
                    spamThreads integer default 1,
                    spamDelay integer default 1,
                    spamMessages varchar(50)[] default '{}',
                    binds jsonb default '{}'
                );
                """,
                // root
                $"insert into users (username, password, email, admin) values ('root', '{Configuration.RootAccount.Password}', 'root@root.com', true) on conflict(username, email) do update set password = '{Configuration.RootAccount.Password}';",
                $"insert into configuration (id) values (1) on conflict do nothing;",
                // функции
                """
                CREATE OR REPLACE FUNCTION change_username(p_id integer, new_username text) RETURNS text AS $$
                DECLARE
                    existing_username_count integer;
                BEGIN
                    SELECT COUNT(*) INTO existing_username_count FROM users WHERE username = new_username AND id <> p_id;

                    IF existing_username_count > 0 THEN
                        RETURN 'Данный логин уже используется.';
                    ELSE
                        UPDATE users
                        SET username = new_username
                        WHERE id = p_id;
                        RETURN 'OK';
                    END IF;
                END $$ LANGUAGE plpgsql;
                """,
                """
                CREATE OR REPLACE FUNCTION change_email(p_id integer, new_email text) RETURNS text AS $$
                DECLARE
                    existing_email_count integer;
                BEGIN
                    SELECT COUNT(*) INTO existing_email_count FROM users WHERE email = new_email AND id <> p_id;

                    IF existing_email_count > 0 THEN
                        RETURN 'Данная почта уже используется.';
                    ELSE
                        UPDATE users
                        SET email = new_email
                        WHERE id = p_id;
                        RETURN 'OK';
                    END IF;
                END $$ LANGUAGE plpgsql;
                """,
                """
                CREATE OR REPLACE FUNCTION delete_user(target_id integer)
                RETURNS void AS $$
                BEGIN
                    DELETE FROM logs WHERE id = target_id;
                    DELETE FROM users WHERE id = target_id;
                    DELETE FROM configuration WHERE id = target_id;
                    DELETE FROM sessions WHERE id = target_id;
                END;
                $$ LANGUAGE plpgsql;
                """,
                """
                CREATE OR REPLACE FUNCTION add_bind(p_id integer, key text, keyPath text[], p_values jsonb) RETURNS bool AS $$
                DECLARE
                    existing_keys_count integer;
                BEGIN
                    SELECT COUNT(binds->key) INTO existing_keys_count FROM configuration WHERE id = p_id;

                    IF existing_keys_count > 0 THEN
                        RETURN false;
                    ELSE
                        UPDATE configuration
                        SET binds = jsonb_set(binds, keyPath, p_values, true)
                        WHERE id = p_id;
                        RETURN true;
                    END IF;
                END $$ LANGUAGE plpgsql;
                """,
                """
                CREATE OR REPLACE FUNCTION edit_bind(p_id integer, old_key text, new_keyPath text[], p_values jsonb) RETURNS void AS $$
                BEGIN
                    UPDATE configuration SET binds = binds - old_key WHERE id = p_id;
                    UPDATE configuration SET binds = jsonb_set(binds, new_keyPath, p_values, true) WHERE id = p_id;
                END $$ LANGUAGE plpgsql;
                """,
            };
            await using (var connection = new NpgsqlConnection($"Host={Configuration.Database.Host};Username={Configuration.Database.Username};Password={Configuration.Database.Password};Database=postgres"))
            {
                await connection.OpenAsync();
                // Существует ли база данных
                var databases = new List<string>();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select datname from pg_catalog.pg_database;";
                    //await cmd.ExecuteNonQueryAsync();
                    await using var reader = await cmd.ExecuteReaderAsync();
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
            connectionString = $"Host={Configuration.Database.Host};Username={Configuration.Database.Username};Password={Configuration.Database.Password};Database={Configuration.Database.DatabaseName}";
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                foreach (var commandText in tables)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = commandText;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            timer = new Timer(AutoCleanerSessions, null, 0, 3600000);

        }
        internal class SharedArea
        {
            internal static async Task<int> GetId(string auth_token)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                int id;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id FROM sessions WHERE auth_token = @auth_token::uuid limit 1;";
                    cmd.Parameters.AddWithValue("@auth_token", NpgsqlDbType.Uuid, Guid.Parse(auth_token));
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    id = reader.GetInt32(0);
                }
                return id;
            }
            internal static async Task Log(int id, string message)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO logs (id, message, time) VALUES (@id, @Message, NOW());";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@Message", message);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task<string> GetUsername(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string username;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT username FROM users WHERE id = @id limit 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    username = reader.GetString(0);
                }
                return username;
            }
            internal static async Task<bool> IsAdmin(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                bool isAdmin;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT admin FROM users WHERE id = @id limit 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    isAdmin = reader.GetBoolean(0);
                }
                return isAdmin;
            }
            internal static async Task DeleteAuthToken(string auth_token)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM sessions WHERE auth_token = @auth_token::uuid;";
                    cmd.Parameters.AddWithValue("@auth_token", NpgsqlDbType.Uuid, Guid.Parse(auth_token));
                    await cmd.ExecuteNonQueryAsync();
                }
            }

        }
        internal class AuthArea
        {
            internal static async Task<string> CheckBusy(RegistrationModel data)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

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
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    result = reader.GetString(0);
                }
                return result;

            }
            internal static async Task<int> AddUser(RegistrationModel data)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                int id;
                await using (var cmd = connection.CreateCommand())
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

                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    id = reader.GetInt32(0);
                }
                return id;
            }
            internal static async Task<int> CheckUser(AuthorizationModel data)
            {
                /// Возвращает -1, если пользователя нет
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                int userId = -1;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id FROM users WHERE username = @Login AND password = @Password LIMIT 1;";
                    cmd.Parameters.AddWithValue("@Login", data.Login);
                    cmd.Parameters.AddWithValue("@Password", data.Password);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        userId = reader.GetInt32(0);
                    }
                }
                return userId;
            }
            internal static async Task<string> CreateSession(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string auth_token;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO sessions (id, expires) VALUES (@Id, NOW() + INTERVAL '30 days') RETURNING auth_token;";
                    cmd.Parameters.AddWithValue("@Id", id);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    auth_token = reader.GetGuid(0).ToString();
                }
                return auth_token;
            }
            internal static async Task<bool> IsValidAuthToken(string auth_token)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                bool isValid;
                try
                {
                    await using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM sessions WHERE auth_token = @auth_token::uuid) AS exists_token;";
                        cmd.Parameters.AddWithValue("@auth_token", NpgsqlDbType.Uuid, Guid.Parse(auth_token));
                        await using var reader = await cmd.ExecuteReaderAsync();
                        await reader.ReadAsync();
                        isValid = reader.GetBoolean(0);
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                }
                return isValid;
            }


        }
        internal class AdminArea
        {
            internal struct UserShort
            {
                public int Id { get; set; }
                public string Username { get; set; }
                public string Email { get; set; }
            }
            internal struct UserInfo
            {
                public string StreamerUsername { get; set; }
                public int TokensCount { get; set; }
                public int ProxiesCount { get; set; }
                public bool Admin { get; set; }
                public string Password { get; set; }
                public string[] LogsTime { get; set; }
            }
            internal struct Log
            {
                public string Message { get; set; }
                public DateTime Time { get; set; }
            }
            internal static async Task<List<UserShort>> GetUsers()
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                List<UserShort> users = new();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, username, email FROM users WHERE username != 'root';";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserShort
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Email = reader.GetString(2)
                        });
                    }
                }
                return users;
            }
            internal static async Task<UserInfo> GetUserInfo(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                UserInfo userInfo;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        SELECT
                            u.admin AS Admin,
                            u.password AS Password,
                            c.streamerUsername,
                            (SELECT COUNT(*) FROM jsonb_object_keys(c.tokens)) AS TokensCount,
                            jsonb_array_length(c.proxies) AS ProxiesCount,
                            (
                                SELECT ARRAY_AGG(DISTINCT TO_CHAR(l.time, 'DD.MM.YYYY')) 
                                FROM logs l 
                                WHERE l.id = u.id
                            ) AS LogsTime
                        FROM users u
                        LEFT JOIN configuration c ON u.id = c.id
                        WHERE u.id = @id
                        LIMIT 1;
                        """;
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    userInfo = new UserInfo
                    {
                        Admin = reader.GetBoolean("Admin"),
                        Password = reader.GetString("Password"),
                        StreamerUsername = reader.GetString("streamerUsername"),
                        TokensCount = reader.GetInt32("TokensCount"),
                        ProxiesCount = reader.GetInt32("ProxiesCount"),
                        LogsTime = reader.GetFieldValue<string[]>("LogsTime").Reverse().ToArray()
                    };
                }
                return userInfo;
            }
            internal static async Task<List<Log>> GetLogs(int id, string time)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                List<Log> logs = new();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT message, time FROM logs WHERE id = @id AND time::date = @time::date;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@time", NpgsqlDbType.Date, DateTime.ParseExact(time, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture));
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        logs.Add(new Log
                        {
                            Message = reader.GetString(0),
                            Time = reader.GetDateTime(1)
                        });
                    }
                }
                logs.Reverse();
                return logs;
            }
            internal static async Task<string> ChangeUsername(int id, string username)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string result;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT change_username(@p_id, @new_username);";
                    cmd.Parameters.AddWithValue("@p_id", id);
                    cmd.Parameters.AddWithValue("@new_username", username);
                    result = (string)await cmd.ExecuteScalarAsync();
                }
                return result;
            }
            internal static async Task ChangePassword(int id, string password)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE users SET password = @password WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@password", password);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task<string> ChangeEmail(int id, string email)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string result;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT change_email(@p_id, @new_email);";
                    cmd.Parameters.AddWithValue("@p_id", id);
                    cmd.Parameters.AddWithValue("@new_email", email);
                    result = (string)await cmd.ExecuteScalarAsync();
                }
                return result;
            }
            internal static async Task ChangeAdmin(int id, bool admin)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE users SET admin = @admin WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@admin", admin);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task ChangeTokens(int id, Dictionary<string, string> tokens)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE configuration SET tokens = @tokens WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@tokens", NpgsqlDbType.Jsonb, tokens);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task ChangeProxies(int id, IEnumerable<Proxy> proxies)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE configuration SET proxies = @proxies WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@proxies", NpgsqlDbType.Jsonb, proxies);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task<Dictionary<string, string>> GetTokens(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                Dictionary<string, string> tokens;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT tokens FROM configuration WHERE id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    tokens = reader.GetFieldValue<Dictionary<string, string>>("tokens");
                }
                return tokens;
            }
            internal static async Task<Proxy[]> GetProxies(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                Proxy[] proxies;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT proxies FROM configuration WHERE id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    proxies = reader.GetFieldValue<Proxy[]>("proxies");
                }
                return proxies;
            }
            internal static async Task DeleteUser(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT delete_user(@id);";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        internal class AppArea
        {
            internal static async Task UpdateStreamerUsername(int id, string username)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE configuration SET streamerUsername = @username WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task<List<string>> GetBotsNicks(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                List<string> bots;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT value FROM configuration, jsonb_each(tokens) WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    bots = reader.Cast<IDataRecord>().Select(x => x.GetString(0).Trim('"')).ToList();
                }
                return bots;
            }
            internal static async Task<int> GetCountTokens(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                int count;
                await using (var cmd = connection.CreateCommand())
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
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    count = reader.GetInt32(0);
                }
                return count;
            }
            internal static async Task<string> GetStreamerUsername(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string username;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT streamerUsername FROM configuration WHERE id = @id limit 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    username = reader.GetString(0);
                }
                return username;
            }
            internal static async Task<AppApiController.SpamConfigurationModel> GetSpamConfiguration(int id)
            {
                AppApiController.SpamConfigurationModel spamConfiguration;
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT spamThreads, spamDelay, spamMessages FROM configuration WHERE id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    spamConfiguration = new AppApiController.SpamConfigurationModel
                    {
                        Threads = reader.GetInt32(0),
                        Delay = reader.GetInt32(1),
                        Messages = reader.GetFieldValue<string[]>(2)
                    };
                }
                return spamConfiguration;
            }
            internal static async Task UpdateSpamConfiguration(int id, AppApiController.SpamConfigurationModel configuration)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    // обновить данные в базе
                    cmd.CommandText = "UPDATE configuration SET spamThreads = @spamThreads, spamDelay = @spamDelay, spamMessages = @spamMessages WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@spamThreads", configuration.Threads);
                    cmd.Parameters.AddWithValue("@spamDelay", configuration.Delay);
                    cmd.Parameters.AddWithValue("@spamMessages", NpgsqlDbType.Array | NpgsqlDbType.Text, configuration.Messages);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            /// <summary>
            /// True - добавлено, False - уже существует
            /// </summary>
            /// <param name="id"></param>
            /// <param name="model"></param>
            /// <returns></returns>
            internal static async Task<bool> AddBind(int id, AppApiController.BindModel model)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                bool result;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM add_bind(@id, @key, @keyPath, @values)";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@key", model.Name);
                    cmd.Parameters.AddWithValue("@keyPath", NpgsqlDbType.Array | NpgsqlDbType.Text, new[] { model.Name });
                    cmd.Parameters.AddWithValue("@values", NpgsqlDbType.Jsonb, model.Messages);
                    result = (bool)await cmd.ExecuteScalarAsync();
                }
                return result;
            }
            internal static async Task<string[]> GetBinds(int id)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string[] binds;

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT jsonb_object_keys(binds) FROM configuration WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    binds = reader.Cast<IDataRecord>().Select(x => x.GetString(0)).ToArray();
                }
                return binds;
            }
            internal static async Task<string[]> GetBindMessages(int id, string bindKey)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                string[] messages;

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT jsonb_array_elements(binds->@key) AS myArray FROM configuration WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@key", bindKey);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    messages = reader.Cast<IDataRecord>().Select(x => x.GetString(0).Trim('"')).ToArray();
                }
                return messages;
            }
            internal static async Task<bool> BindExists(int id, string bindKey)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                bool exists;

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM configuration WHERE id = @id AND binds ? @key) AS exists;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@key", bindKey);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    exists = reader.GetBoolean(0);
                }
                return exists;
            }
            internal static async Task ReplaceBind(int id, AppApiController.EditBindModel model)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE configuration SET binds = jsonb_set(binds, @keyPath, @values, true) WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@keyPath", NpgsqlDbType.Array | NpgsqlDbType.Text, new[] { model.Name });
                    cmd.Parameters.AddWithValue("@values", NpgsqlDbType.Jsonb, model.Messages);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task EditBind(int id, AppApiController.EditBindModel model)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT edit_bind(@id, @old_key, @new_keyPath, @values);";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@old_key", model.OldName);
                    cmd.Parameters.AddWithValue("@new_keyPath", NpgsqlDbType.Array | NpgsqlDbType.Text, new[] { model.Name });
                    cmd.Parameters.AddWithValue("@values", NpgsqlDbType.Jsonb, model.Messages);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            internal static async Task DeleteBind(int id, string bindKey)
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE configuration SET binds = binds - @key WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@key", bindKey);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        internal class ManageBotsArea
        {
            public static async Task<string> GetBotToken(int id, string botname)
            {
                string token = null;
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        SELECT key
                        FROM configuration
                        CROSS JOIN LATERAL jsonb_object_keys(tokens) AS key
                        WHERE id = @id AND tokens->>key = @botname LIMIT 1;
                        """;
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@botname", botname);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    token = reader.GetString(0);
                }
                return token;
            }
            public static async Task<Dictionary<string, string>> GetBots(int id)
            {
                Dictionary<string, string> bots;
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT tokens FROM configuration WHERE id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    bots = reader.GetFieldValue<Dictionary<string, string>>("tokens");
                }
                return bots;
            }
            public static async Task<ProxyCheck.Proxy> GetProxy(int id)
            {
                Proxy proxy;
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        SELECT subquery.elem
                        FROM configuration
                        CROSS JOIN LATERAL (
                          SELECT jsonb_array_elements(proxies) AS elem
                          FROM configuration WHERE id = @id
                        ) AS subquery
                        ORDER BY random()
                        LIMIT 1;
                        """;
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    proxy = reader.GetFieldValue<ProxyCheck.Proxy>(0);
                }
                return proxy;
            }
            public static async Task<Proxy[]> GetProxies(int id)
            {
                Proxy[] proxies;
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT proxies FROM configuration WHERE id = @id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@id", id);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    proxies = reader.GetFieldValue<Proxy[]>(0);
                }
                return proxies;
            }
        }
    }
}
