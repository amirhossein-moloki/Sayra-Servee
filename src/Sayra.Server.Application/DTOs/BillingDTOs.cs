namespace Sayra.Server.Application.DTOs;

public record BillingSummaryResponse(
    string PcId,
    SessionResponse? ActiveSession,
    int UnpaidSessionsCount,
    decimal TotalUnpaidAmount
);

public record InvoiceResponse(
    string InvoiceId,
    string SessionId,
    decimal Amount,
    decimal Tax,
    decimal Total,
    DateTime IssuedAt
);

public record BillingReportMetadata(
    string ReportId,
    DateTime GeneratedAt,
    decimal TotalRevenue,
    int SessionCount
);
