using System.Collections.Concurrent;
using Sayra.Server.Monitoring.Interfaces;

namespace Sayra.Server.Monitoring.Services;

public class AlertService : IAlertService
{
    private readonly ConcurrentDictionary<string, string> _activeAlerts = new();

    public void ProcessMetric(string name, double value, string? context = null)
    {
        if (name == "CPU" && value > 90)
        {
            _activeAlerts[$"CPU_{context}"] = $"High CPU usage on client {context}: {value:F1}%";
        }
        else if (name == "CPU" && value <= 90)
        {
            _activeAlerts.TryRemove($"CPU_{context}", out _);
        }

        if (name == "FailedCommands" && value > 10)
        {
            _activeAlerts["CommandFailure"] = "High command failure rate detected!";
        }
    }

    public IEnumerable<string> GetActiveAlerts() => _activeAlerts.Values;
}
