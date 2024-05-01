using Microsoft.EntityFrameworkCore;
using TCS.Server.Database;
using TCS.Server.Database.Models;

namespace TCS.Server.Services;

public class InviteCodeExpiresCheckService(IServiceProvider _serviceProvider) : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider = _serviceProvider;
    private Timer _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckCodes, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    private async void CheckCodes(object state)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var now = TimeHelper.GetUnspecifiedUtc();
        await context.InviteCodes
            .Where(x => x.Status == InviteCodeStatus.Active && x.Mode == InviteCodeMode.Time && x.Expires < now)
            .ExecuteUpdateAsync(x => x.SetProperty(c => c.Status, c => InviteCodeStatus.Expired));
        await context.SaveChangesAsync();
    }
}