using Npgsql;
using System.Data;
using TCS.Controllers;

namespace TCS
{
    public class Database
    {
        private static NpgsqlConnection connection;
        private static Timer timer;
        private static async void AutoCleanerSessions(object state)
        {
            Console.WriteLine("ujryutyuyrtu");
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
                "username text," +
                "password text," +
                "email text," +
                "unique(email, username)" +
                ");",
                $"insert into users (username, password, email) values ('root', '{Configuration.RootAccount.Password}', 'root@root.com') on conflict(username, email) do update set password = '{Configuration.RootAccount.Password}';",
                // sessions
                "create table if not exists sessions (" +
                "auth_token uuid primary key default gen_random_uuid()," +
                "id integer," +
                "expires timestamp," +
                "unique(auth_token)" +
                ");",
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
                    while (reader.Read())
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
                reader.Read();
                result = reader.GetString(0);
            }
            return result;

        }
        internal static async Task<int> AddUser(RegistrationModel data)
        {
            int id;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO users (username, password, email) VALUES (@Username, @Password, @Email) RETURNING id;";

                cmd.Parameters.AddWithValue("@Username", data.Login);
                cmd.Parameters.AddWithValue("@Password", data.Password);
                cmd.Parameters.AddWithValue("@Email", data.Email);

                using var reader = await cmd.ExecuteReaderAsync();
                reader.Read();
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
                reader.Read();
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
                    cmd.CommandText = $"SELECT EXISTS (SELECT 1 FROM sessions WHERE auth_token = '@auth_token') AS exists_token;";
                    cmd.Parameters.AddWithValue("@auth_token", auth_token);
                    using var reader = await cmd.ExecuteReaderAsync();
                    reader.Read();
                    isValid = reader.GetBoolean(0);
                }
            }
            catch
            {
                isValid = false;
            }
            return isValid;
        }
    }
}
