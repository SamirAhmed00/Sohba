using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sohba.Application.Interfaces;
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
            _logger.LogInformation("Story Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

                    var mediaUrls = await unitOfWork.Stories.DeleteExpiredStoriesAsync();

                    foreach (var mediaUrl in mediaUrls)
                    {
                        try
                        {
                            await fileStorage.DeleteFileAsync(mediaUrl);
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogWarning(fileEx,
                                "Could not delete story media file {MediaUrl}. It will be retried or needs manual cleanup.",
                                mediaUrl);
                        }
                    }

                    if (mediaUrls.Count > 0)
                    {
                        _logger.LogInformation(
                            "Story cleanup soft-deleted {StoryCount} expired stories and freed {FileCount} media files.",
                            mediaUrls.Count, mediaUrls.Count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up expired stories.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Story Cleanup Service stopped.");
        }
    }
}