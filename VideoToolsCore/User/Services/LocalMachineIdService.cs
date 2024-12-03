using System.Management;

namespace VideoToolsCore.User.Services;

public static class LocalMachineIdService
{
    public static string GetUUID()
    {
        using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
        var obj = searcher.Get().OfType<ManagementObject>().FirstOrDefault();
        return obj?["UUID"]?.ToString() ?? string.Empty;
    }
    public static string GetName()
    {
        return Environment.MachineName;
    }
}
