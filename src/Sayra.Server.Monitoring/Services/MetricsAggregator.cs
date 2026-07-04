using System.Collections.Concurrent;
using Sayra.Server.Monitoring.Interfaces;
using Sayra.Server.Monitoring.Models;

namespace Sayra.Server.Monitoring.Services;

public class MetricsAggregator : IMetricsService
{
    private int _activeClients;
    private int _activeSessions;
    private int _commandsExecuted;
    private int _failedCommands;

    private readonly ConcurrentDictionary<string, (float Cpu, float Ram)> _latestClientTelemetry = new();
    private readonly ConcurrentQueue<TelemetryDataPoint> _cpuHistory = new();
    private readonly ConcurrentQueue<TelemetryDataPoint> _ramHistory = new();
    private const int MaxHistory = 100;

    public SystemMetrics GetCurrentMetrics()
    {
        var avgCpu = _latestClientTelemetry.Values.Any() ? _latestClientTelemetry.Values.Average(x => x.Cpu) : 0;
        var avgRam = _latestClientTelemetry.Values.Any() ? _latestClientTelemetry.Values.Average(x => x.Ram) : 0;

        return new SystemMetrics
        {
            ActiveClients = _activeClients,
            ActiveSessions = _activeSessions,
            AverageCpuUsage = avgCpu,
            AverageRamUsage = avgRam,
            CommandsExecuted = _commandsExecuted,
            FailedCommands = _failedCommands
        };
    }

    public IEnumerable<TelemetryDataPoint> GetCpuHistory() => _cpuHistory.ToArray();
    public IEnumerable<TelemetryDataPoint> GetRamHistory() => _ramHistory.ToArray();

    public void RecordTelemetry(string pcId, float cpu, float ram)
    {
        _latestClientTelemetry[pcId] = (cpu, ram);

        var now = DateTime.UtcNow;
        AddToHistory(_cpuHistory, new TelemetryDataPoint(now, cpu));
        AddToHistory(_ramHistory, new TelemetryDataPoint(now, ram));
    }

    private void AddToHistory(ConcurrentQueue<TelemetryDataPoint> queue, TelemetryDataPoint point)
    {
        queue.Enqueue(point);
        while (queue.Count > MaxHistory)
        {
            queue.TryDequeue(out _);
        }
    }

    public void RecordCommand(bool success)
    {
        Interlocked.Increment(ref _commandsExecuted);
        if (!success) Interlocked.Increment(ref _failedCommands);
    }

    public void SetClientCount(int count) => _activeClients = count;
    public void SetSessionCount(int count) => _activeSessions = count;
}
