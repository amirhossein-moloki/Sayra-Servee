using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Monitoring.Interfaces;

namespace Sayra.Server.Monitoring;

public class MonitoringEventHandler
{
    private readonly IMetricsService _metricsService;
    private readonly IAlertService _alertService;
    private int _clientCount = 0;
    private int _sessionCount = 0;

    public MonitoringEventHandler(
        IEventSubscriber subscriber,
        IMetricsService metricsService,
        IAlertService alertService)
    {
        _metricsService = metricsService;
        _alertService = alertService;

        subscriber.Subscribe<ClientConnectedEvent>(HandleClientConnected);
        subscriber.Subscribe<ClientDisconnectedEvent>(HandleClientDisconnected);
        subscriber.Subscribe<SessionStartedEvent>(HandleSessionStarted);
        subscriber.Subscribe<SessionEndedEvent>(HandleSessionEnded);
        subscriber.Subscribe<TelemetryReceivedEvent>(HandleTelemetry);
        subscriber.Subscribe<CommandExecutedEvent>(HandleCommand);
    }

    private Task HandleClientConnected(ClientConnectedEvent @event, CancellationToken ct)
    {
        Interlocked.Increment(ref _clientCount);
        _metricsService.SetClientCount(_clientCount);
        return Task.CompletedTask;
    }

    private Task HandleClientDisconnected(ClientDisconnectedEvent @event, CancellationToken ct)
    {
        Interlocked.Decrement(ref _clientCount);
        _metricsService.SetClientCount(_clientCount);
        return Task.CompletedTask;
    }

    private Task HandleSessionStarted(SessionStartedEvent @event, CancellationToken ct)
    {
        Interlocked.Increment(ref _sessionCount);
        _metricsService.SetSessionCount(_sessionCount);
        return Task.CompletedTask;
    }

    private Task HandleSessionEnded(SessionEndedEvent @event, CancellationToken ct)
    {
        Interlocked.Decrement(ref _sessionCount);
        _metricsService.SetSessionCount(_sessionCount);
        return Task.CompletedTask;
    }

    private Task HandleTelemetry(TelemetryReceivedEvent @event, CancellationToken ct)
    {
        _metricsService.RecordTelemetry(@event.PcId, @event.CpuUsage, @event.RamUsage);
        _alertService.ProcessMetric("CPU", @event.CpuUsage, @event.PcId);
        return Task.CompletedTask;
    }

    private Task HandleCommand(CommandExecutedEvent @event, CancellationToken ct)
    {
        bool success = !string.IsNullOrEmpty(@event.Result) && !@event.Result.Contains("Error", StringComparison.OrdinalIgnoreCase);
        _metricsService.RecordCommand(success);
        return Task.CompletedTask;
    }
}
