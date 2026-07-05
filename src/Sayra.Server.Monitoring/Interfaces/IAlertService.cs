namespace Sayra.Server.Monitoring.Interfaces;

public interface IAlertService
{
    void ProcessMetric(string name, double value, string? context = null);
    IEnumerable<string> GetActiveAlerts();
}
