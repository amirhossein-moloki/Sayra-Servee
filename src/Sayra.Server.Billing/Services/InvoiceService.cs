using System.Text.Json;
using Sayra.Server.Billing.Models;

namespace Sayra.Server.Billing.Services;

public interface IInvoiceService
{
    string GenerateJsonInvoice(BillingSession session);
    byte[] GeneratePdfInvoiceStub(BillingSession session);
}

public class InvoiceService : IInvoiceService
{
    public string GenerateJsonInvoice(BillingSession session)
    {
        var invoice = new
        {
            InvoiceId = "INV-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            Date = DateTime.UtcNow,
            SessionId = session.SessionId,
            PcId = session.PcId,
            Duration = (session.EndTime - session.StartTime)?.TotalMinutes ?? 0,
            Amount = session.TotalAmount,
            Status = "Paid"
        };
        return JsonSerializer.Serialize(invoice, new JsonSerializerOptions { WriteIndented = true });
    }

    public byte[] GeneratePdfInvoiceStub(BillingSession session)
    {
        // PDF generation would require a library like QuestPDF or iText7.
        // For this phase, we'll return a byte array of the JSON string as a stub.
        var json = GenerateJsonInvoice(session);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}
