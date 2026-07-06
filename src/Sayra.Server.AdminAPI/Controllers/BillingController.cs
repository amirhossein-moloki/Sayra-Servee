using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Billing.Services;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("billing")]
public class BillingController(BillingEngine billingEngine, InvoiceService invoiceService) : ControllerBase
{
    [HttpGet("summary/{pcId}")]
    [ProducesResponseType(typeof(BillingSummaryResponse), 200)]
    public IActionResult GetSummary(string pcId)
    {
        return Ok(new BillingSummaryResponse(pcId, null, 0, 0));
    }

    [HttpPost("invoice/{sessionId}")]
    [ProducesResponseType(typeof(InvoiceResponse), 201)]
    public async Task<IActionResult> GenerateInvoice(string sessionId)
    {
        var invoice = new InvoiceResponse(Guid.NewGuid().ToString(), sessionId, 0, 0, 0, DateTime.UtcNow);
        return CreatedAtAction(null, invoice);
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(BillingReportMetadata), 200)]
    public IActionResult GetReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        return Ok(new BillingReportMetadata(Guid.NewGuid().ToString(), DateTime.UtcNow, 0, 0));
    }
}
