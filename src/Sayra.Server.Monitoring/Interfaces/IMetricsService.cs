using Sayra.Server.Monitoring.Models;

namespace Sayra.Server.Monitoring.Interfaces;

public interface IMetricsService
{
    SystemMetrics GetCurrentMetrics();
    IEnumerable<TelemetryDataPoint> GetCpuHistory();
    IEnumerable<TelemetryDataPoint> GetRamHistory();
    void RecordTelemetry(string pcId, float cpu, float ram);
    void RecordCommand(bool success);
    void SetClientCount(int count);
    void SetSessionCount(int count);
}
