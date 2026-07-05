namespace Sayra.Server.MultiSite.Interfaces;

public interface ISiteContext
{
    string? CurrentSiteId { get; set; }
}

public class SiteContext : ISiteContext
{
    public string? CurrentSiteId { get; set; }
}
