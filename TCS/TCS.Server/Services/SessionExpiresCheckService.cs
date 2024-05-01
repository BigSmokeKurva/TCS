using Microsoft.EntityFrameworkCore;
using TCS.Server.Database;

namespace TCS.Server.Services;

public class SessionExpiresCheckService(IServiceProvider _serviceProvider) : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider = _serviceProvider;
    private Timer _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckSessions, null, TimeSpan.Zero, TimeSpan.FromHours(1));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    public async void CheckSessions(object state)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await context.Sessions.Where(x => x.Expires < TimeHelper.GetUnspecifiedUtc()).ExecuteDeleteAsync();
        await context.SaveChangesAsync();
    }
}