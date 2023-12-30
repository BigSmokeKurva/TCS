﻿using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using TCS.Database.Models;
using static TCS.Database.Models.Proxy;

namespace TCS;

public class UserValidators
{
    private static readonly Regex emailRegex = new(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", RegexOptions.Compiled);
    private static readonly Regex loginRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex passwordRegex = new("^[a-zA-Z0-9!@#$%^&*()_-]+$", RegexOptions.Compiled);

    internal static bool ValidateEmail(string email)
    {
        // Проверяем, что email соответствует регулярному выражению и его длина не превышает 30 символов
        return emailRegex.IsMatch(email) && email.Length <= 30;
    }
    internal static bool ValidateLogin(string login)
    {
        int minLength = 4; // Минимальная длина
        int maxLength = 16; // Максимальная длина

        bool hasValidLength = login.Length >= minLength && login.Length <= maxLength;
        bool matchesPattern = loginRegex.IsMatch(login);

        return hasValidLength && matchesPattern;
    }
    internal static bool ValidatePassword(string password)
    {
        int minLength = 5; // Минимальная длина пароля
        int maxLength = 30; // Максимальная длина пароля

        bool hasValidLength = password.Length >= minLength && password.Length <= maxLength;
        bool matchesPattern = passwordRegex.IsMatch(password);

        return hasValidLength && matchesPattern;
    }

    internal static bool ValidateStreamerUsername(string login)
    {
        int minLength = 4; // Минимальная длина
        int maxLength = 30; // Максимальная длина

        bool hasValidLength = login.Length >= minLength && login.Length <= maxLength;
        bool matchesPattern = loginRegex.IsMatch(login);

        return hasValidLength && matchesPattern;
    }
}

/// <summary>
/// Чекер токенов твича
/// </summary>
public class TokenCheck
{
    private static readonly Dictionary<string, string> headers = new()
            {
                {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0"},
                {"Accept", "*/*"},
                //{"Authorization", $"OAuth {token}"},
                {"Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko"},
                //{"Accept-Encoding", "gzip, deflate, br"},
                {"Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3"},
                {"Host", "gql.twitch.tv" },
                {"Origin", "https://www.twitch.tv/" },
                {"Referer", "https://www.twitch.tv/" },
                {"Connection", "keep-alive"},
                {"Sec-Fetch-Dest", "empty"},
                {"Sec-Fetch-Mode", "cors"},
                {"Sec-Fetch-Site", "same-site"},
            };
    internal static int Threads;

    internal static async Task<Dictionary<string, string>> Check(IEnumerable<string> tokens)
    {
        ConcurrentDictionary<string, string> result = new();
        var content = new StringContent("[{\"operationName\":\"Core_Services_Spade_CurrentUser\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"482be6fdcd0ff8e6a55192210e2ec6db8a67392f206021e81abe0347fc727ebe\"}}}]", Encoding.UTF8, "text/plain"); ;
        using var client = new HttpClient();
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
        await Parallel.ForEachAsync(tokens, new ParallelOptions() { MaxDegreeOfParallelism = Threads }, async (token, e) =>
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gql.twitch.tv/gql");
                request.Headers.Add("Authorization", $"OAuth {token}");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (response is null)
                    return;
                var json = await response.Content.ReadFromJsonAsync<CoreServicesJson[]>();
                result.TryAdd(token, json[0].data.currentUser.login);
                response.Dispose();

            }
            catch { }
        });
        return UniqueValues(new Dictionary<string, string>(result));
    }
    private static Dictionary<string, string> UniqueValues(Dictionary<string, string> dictionary)
    {
        var result = new Dictionary<string, string>();
        foreach (var item in dictionary)
        {
            if (!result.ContainsValue(item.Value))
                result.Add(item.Key, item.Value);
        }
        return result;
    }
    public class CoreServicesJson
    {
        public class CurrentUser
        {
            public string login { get; set; }
        }
        public class Data
        {
            public CurrentUser currentUser { get; set; }
        }

        public Data data { get; set; }
    }

}
public class ProxyCheck
{
    private static readonly HashSet<string> Types = new() { "http", "socks5" };
    internal static List<Proxy> Parse(IEnumerable<string> proxies)
    {
        List<Proxy> result = [];

        foreach (var proxy in proxies)
        {
            try
            {
                var split = proxy.Split(':');

                if (split.Length != 3 && split.Length != 5)
                    continue;

                var type = split[0].ToLower();

                if (!Types.Contains(type))
                    continue;


                result.Add(new Proxy
                {
                    Type = type,
                    Host = split[1],
                    Port = split[2],
                    Credentials = split.Length == 5 ? new UnSafeCredentials(split[3], split[4]) : null
                });
            }
            catch { }
        }

        return result;
    }
    public static string ProxyToString(Proxy proxy)
    {
        return proxy.Credentials is null
            ? $"{proxy.Type}:{proxy.Host}:{proxy.Port}"
            : $"{proxy.Type}:{proxy.Host}:{proxy.Port}:{proxy.Credentials.Value.Username}:{proxy.Credentials.Value.Password}";
    }
    public static string[] ProxyToString(IEnumerable<Proxy> proxies)
    {
        return proxies.Select(ProxyToString).ToArray();
    }
}
