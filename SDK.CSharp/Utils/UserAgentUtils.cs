using System.Runtime.InteropServices;

namespace OpenShock.SDK.CSharp.Utils;

public static class UserAgentUtils
{
    // I hate microsoft sometimes
    public static string GetOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows NT";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
        return "Unknown OS";
    }
}