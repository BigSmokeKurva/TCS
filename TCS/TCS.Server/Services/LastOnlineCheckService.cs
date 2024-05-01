using Microsoft.EntityFrameworkCore;
using TCS.Server.BotsManager;
using TCS.Server.Database;
using TCS.Server.Database.Models;

namespace TCS.Server.Services;

public class LastOnlineCheckService(IServiceProvider _serviceProvider) : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider = _serviceProvider;
    private Timer _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckLastOnline, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    private async void CheckLastOnline(object state)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var users = Manager.users.Where(x => x.Value.bots.Any());
        var now = TimeHelper.GetUnspecifiedUtc();
        foreach (var user in users)
        {
            if (!(now - await context.Users.Where(x => x.Id == user.Key).Select(x => x.LastOnline).FirstAsync() >
                  TimeSpan.FromMinutes(10)))
                continue;
            if (user.Value.spamTasks.Any())
            {
                await user.Value.StopSpam();
                await context.AddLog(user.Key, "Остановил спам. (Бездействие)", LogType.Action);
            }

            if (user.Value.bots.Any())
            {
                await user.Value.DisconnectAllBots();
                await context.AddLog(user.Key, "Отключил всех ботов. (Бездействие)", LogType.Action);
            }

            Manager.users.Remove(user.Key);
        }

        await context.SaveChangesAsync();
    }
}