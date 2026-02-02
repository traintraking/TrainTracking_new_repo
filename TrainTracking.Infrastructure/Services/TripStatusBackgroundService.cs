using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TrainTracking.Application.Interfaces;

public class TripStatusBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TripStatusBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    //change status of finished trips to Completed
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var tripRepository = scope.ServiceProvider.GetRequiredService<ITripRepository>();

            await tripRepository.CompleteFinishedTripsAsync();

            // كل دقيقة
            await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken);
        }
    }
}
