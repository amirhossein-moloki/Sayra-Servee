using Sayra.Server.Licensing.Models;

namespace Sayra.Server.FeatureGating.Services;

public interface IFeatureManager
{
    bool IsFeatureEnabled(string featureName);
    int GetLimit(string limitName);
}

public class FeatureManager : IFeatureManager
{
    private readonly LicenseTier _activeTier;

    public FeatureManager(LicenseTier activeTier)
    {
        _activeTier = activeTier;
    }

    public bool IsFeatureEnabled(string featureName)
    {
        return featureName switch
        {
            "MultiSite" => _activeTier >= LicenseTier.Pro,
            "AdvancedReports" => _activeTier >= LicenseTier.Pro,
            "ApiAccess" => _activeTier >= LicenseTier.Pro,
            "BasicOperations" => true,
            _ => false
        };
    }

    public int GetLimit(string limitName)
    {
        return limitName switch
        {
            "MaxPCs" => _activeTier switch
            {
                LicenseTier.Trial => 5,
                LicenseTier.Standard => 20,
                LicenseTier.Pro => 100,
                LicenseTier.Enterprise => int.MaxValue,
                _ => 0
            },
            "MaxSites" => _activeTier switch
            {
                LicenseTier.Pro => 2,
                LicenseTier.Enterprise => int.MaxValue,
                _ => 1
            },
            _ => 0
        };
    }
}
