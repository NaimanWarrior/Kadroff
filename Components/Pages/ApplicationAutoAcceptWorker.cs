using kadroff.Components.Data;
using Microsoft.EntityFrameworkCore;

namespace kadroff.Components.Services
{
    public class ApplicationAutoAcceptWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ApplicationAutoAcceptWorker> _logger;

        public ApplicationAutoAcceptWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ApplicationAutoAcceptWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var today = DateTime.UtcNow.Date;

                    var expiredApps = await context.JobApplications
                        .Where(j => j.AutoAcceptDate != null
                                 && j.AutoAcceptDate.Value.Date <= today
                                 && j.Status != JobApplicationStatus.Accepted
                                 && j.Status != JobApplicationStatus.Rejected)
                        .ToListAsync(stoppingToken);

                    if (expiredApps.Any())
                    {
                        foreach (var app in expiredApps)
                        {
                            app.Status = JobApplicationStatus.Accepted;
                        }

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Автоматически переведены в статус 'Принят' отклики в количестве: {Count}", expiredApps.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при автоматическом обновлении статусов откликов");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}