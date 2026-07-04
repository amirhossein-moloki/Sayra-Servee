using Microsoft.Extensions.DependencyInjection;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Monitoring.Interfaces;
using Sayra.Server.Monitoring.Services;
using Sayra.Server.Monitoring;
using Sayra.Server.ProductionHardening.CircuitBreaker;
using Sayra.Server.Application.Interfaces;
using Moq;
using Xunit;

namespace Sayra.Server.IntegrationTests;

public class Phase4Tests
{
    [Fact]
    public async Task TelemetryReceivedEvent_UpdatesMetricsAggregator()
    {
        // Arrange
        var services = new ServiceCollection();
        var eventBus = new InMemoryEventBus();
        services.AddSingleton<IEventPublisher>(eventBus);
        services.AddSingleton<IEventSubscriber>(eventBus);
        services.AddSingleton<IMetricsService, MetricsAggregator>();
        services.AddSingleton<IAlertService, AlertService>();
        services.AddSingleton<MonitoringEventHandler>();

        var sp = services.BuildServiceProvider();
        var metrics = sp.GetRequiredService<IMetricsService>();
        var handler = sp.GetRequiredService<MonitoringEventHandler>();

        var telemetryEvent = new TelemetryReceivedEvent("PC-01", 45.5f, 2048f, 3600);

        // Act
        await eventBus.PublishAsync(telemetryEvent);
        await Task.Delay(100);

        // Assert
        var currentMetrics = metrics.GetCurrentMetrics();
        Assert.Equal(45.5f, currentMetrics.AverageCpuUsage);
    }

    [Fact]
    public async Task CircuitBreaker_Opens_AfterFailures()
    {
        // Arrange
        var mockRepo = new Mock<ISessionRepository>();
        mockRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB Down"));

        var cb = new DbCircuitBreaker();
        var decorator = new SessionRepositoryDecorator(mockRepo.Object, cb);

        // Act & Assert
        // Fail 5 times to open circuit
        for(int i=0; i<5; i++)
        {
            await Assert.ThrowsAsync<Exception>(() => decorator.GetAllAsync());
        }

        // 6th call should fail immediately with Circuit OPEN message
        var ex = await Assert.ThrowsAsync<Exception>(() => decorator.GetAllAsync());
        Assert.Contains("Circuit is OPEN", ex.Message);
    }
}
