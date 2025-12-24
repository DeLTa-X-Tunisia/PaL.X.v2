using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using PaL.X.Data;
using Microsoft.EntityFrameworkCore;

namespace PaL.X.Api.Services
{
    public class MessageCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run once a day
        private readonly TimeSpan _errorBackoff = TimeSpan.FromSeconds(15);
        private readonly int _retentionDays = 30; // Default 30 days

        public MessageCleanupService(IServiceProvider serviceProvider, ILogger<MessageCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

                        var oldMessages = dbContext.Messages.Where(m => m.Timestamp < cutoffDate);
                        if (await oldMessages.AnyAsync(stoppingToken))
                        {
                            dbContext.Messages.RemoveRange(oldMessages);
                            int deletedCount = await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"Deleted {deletedCount} old messages (older than {cutoffDate}).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during message cleanup.");
                    try
                    {
                        await Task.Delay(_errorBackoff, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // shutting down
                    }
                    continue;
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
