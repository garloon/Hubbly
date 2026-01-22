using Hubbly.Application.Features.Rooms;

namespace Hubbly.Api.Jobs
{
    public class OnlineUsersCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OnlineUsersCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public OnlineUsersCleanupService(
            IServiceProvider serviceProvider,
            ILogger<OnlineUsersCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OnlineUsersCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var roomService = scope.ServiceProvider.GetRequiredService<RoomService>();

                    await roomService.SyncAllRoomsCountAsync();

                    _logger.LogDebug("Synced room counts with Redis");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnlineUsersCleanupService");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
