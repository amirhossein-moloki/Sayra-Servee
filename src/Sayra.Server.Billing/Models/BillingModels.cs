namespace Sayra.Server.Billing.Models;

public class PricePlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public decimal RatePerHour { get; set; }
    public decimal MinimumCharge { get; set; }
}

public class BillingSession
{
    public string SessionId { get; set; } = string.Empty;
    public string PcId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
}
