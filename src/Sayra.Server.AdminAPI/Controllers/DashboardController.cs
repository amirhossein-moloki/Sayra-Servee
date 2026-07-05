using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Monitoring.Interfaces;
using Sayra.Server.Monitoring.Models;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly IAlertService _alertService;

    public DashboardController(IMetricsService metricsService, IAlertService alertService)
    {
        _metricsService = metricsService;
        _alertService = alertService;
    }

    [HttpGet("overview")]
    public ActionResult<SystemMetrics> GetOverview()
    {
        return Ok(_metricsService.GetCurrentMetrics());
    }

    [HttpGet("metrics/timeline")]
    public ActionResult GetTimeline()
    {
        return Ok(new
        {
            Cpu = _metricsService.GetCpuHistory(),
            Ram = _metricsService.GetRamHistory()
        });
    }

    [HttpGet("alerts")]
    public ActionResult<IEnumerable<string>> GetAlerts()
    {
        return Ok(_alertService.GetActiveAlerts());
    }
}
