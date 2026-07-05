using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Sayra.Server.Licensing.Services;

public interface IHardwareFingerprintService
{
    string GetHardwareId();
}

public class HardwareFingerprintService : IHardwareFingerprintService
{
    public string GetHardwareId()
    {
        var sb = new StringBuilder();

        sb.Append(GetCpuId());
        sb.Append(GetMotherboardSerialNumber());
        sb.Append(GetPrimaryMacAddress());

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    private string GetCpuId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var mbs = new ManagementObjectSearcher("Select ProcessorId From Win32_Processor");
                using var mbsList = mbs.Get();
                foreach (var mo in mbsList)
                {
                    var id = mo["ProcessorId"]?.ToString();
                    if (!string.IsNullOrEmpty(id)) return id;
                }
            }
            catch { /* Fallback */ }
        }
        return Environment.ProcessorCount.ToString();
    }

    private string GetMotherboardSerialNumber()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var mbs = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard");
                using var mbsList = mbs.Get();
                foreach (var mo in mbsList)
                {
                    var sn = mo["SerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(sn)) return sn;
                }
            }
            catch { /* Fallback */ }
        }
        return "MB-UNKNOWN";
    }

    private string GetPrimaryMacAddress()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault() ?? "000000000000";
    }
}
