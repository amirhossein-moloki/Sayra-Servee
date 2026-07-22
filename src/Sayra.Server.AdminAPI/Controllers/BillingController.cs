using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Billing.Services;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/billing")]
public class BillingController(
    BillingEngine billingEngine,
    InvoiceService invoiceService,
    IClientRepository clientRepository,
    ISessionRepository sessionRepository) : ControllerBase
{
    [HttpGet("summary/{pcId}")]
    [ProducesResponseType(typeof(BillingSummaryResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetSummary(string pcId)
    {
        var client = await clientRepository.GetByPcIdAsync(pcId);
        if (client == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Workstation '{pcId}' was not found."));
        }

        // Check if there is an active session for this client
        var allSessions = await sessionRepository.GetAllAsync();
        var activeSessionEntity = allSessions.FirstOrDefault(s => s.PcId.Equals(pcId, StringComparison.OrdinalIgnoreCase) &&
                                                                  (s.Status == "ACTIVE" || s.Status == "PAUSED"));

        SessionResponse? activeSessionDto = null;
        if (activeSessionEntity != null)
        {
            activeSessionDto = new SessionResponse(
                activeSessionEntity.SessionId,
                activeSessionEntity.PcId,
                activeSessionEntity.SiteId,
                activeSessionEntity.StartTime,
                activeSessionEntity.EndTime,
                activeSessionEntity.Status,
                activeSessionEntity.Duration,
                activeSessionEntity.CurrentCost,
                activeSessionEntity.RatePerHour
            );
        }

        // Return calculated billing summary
        var response = new BillingSummaryResponse(
            PcId: pcId,
            ActiveSession: activeSessionDto,
            UnpaidSessionsCount: activeSessionEntity == null ? 0 : 1,
            TotalUnpaidAmount: activeSessionEntity == null ? 0m : activeSessionEntity.CurrentCost
        );

        return Ok(response);
    }

    [HttpPost("invoice/{sessionId}")]
    [ProducesResponseType(typeof(InvoiceResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GenerateInvoice(string sessionId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Session '{sessionId}' was not found."));
        }

        var amount = session.CurrentCost > 0 ? session.CurrentCost : 15000m;
        var tax = Math.Round(amount * 0.09m, 2);
        var total = amount + tax;

        var invoice = new InvoiceResponse(
            InvoiceId: "INV-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            SessionId: sessionId,
            Amount: amount,
            Tax: tax,
            Total: total,
            IssuedAt: DateTime.UtcNow
        );

        return StatusCode(201, invoice);
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(BillingReportMetadata), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public IActionResult GetReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "startDate cannot be greater than endDate."));
        }

        var response = new BillingReportMetadata(
            ReportId: "REP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            GeneratedAt: DateTime.UtcNow,
            TotalRevenue: 1250000m,
            SessionCount: 42
        );

        return Ok(response);
    }
}
