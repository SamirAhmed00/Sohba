using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sohba.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sohba.Infrastructure.Services
{
    public class StoryCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StoryCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public StoryCleanupService(IServiceProvider serviceProvider, ILogger<StoryCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    await unitOfWork.Stories.DeleteExpiredStoriesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up expired stories.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}