using Microsoft.EntityFrameworkCore;

public class PaymentExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentExpirationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                var expiredTransactions = await context.Payments
                    .Where(t => t.Status == "Pending" && t.ExpirationTime <= now)
                    .ToListAsync(stoppingToken);

                foreach (var transaction in expiredTransactions)
                {
                    transaction.Status = "Expired";
                }

                if (expiredTransactions.Any())
                {
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
