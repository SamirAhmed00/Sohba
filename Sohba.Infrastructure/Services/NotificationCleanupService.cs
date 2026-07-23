using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sohba.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sohba.Infrastructure.Services
{
    public class NotificationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public NotificationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<NotificationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    _logger.LogInformation("Running notification cleanup...");

                    // Delete notifications older than 30 days
                    var result = await notificationService.DeleteOldNotificationsAsync(30);

                    if (result.IsSuccess)
                    {
                        _logger.LogInformation("Notification cleanup completed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Notification cleanup failed: {Error}", result.Error);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error during notification cleanup");
                }
            }

            _logger.LogInformation("Notification Cleanup Service stopped");
        }
    }
}