using System.Runtime.InteropServices;
using System.Text;

namespace WindowsManager.App.Services
{
    /// <summary>
    /// Console tools that ship with Windows (powercfg, netsh, powershell.exe) write localized text to
    /// redirected stdout using the OEM codepage, not UTF-8/ANSI. Decoding that output with the wrong
    /// encoding turns umlauts (ä/ö/ü) and other non-ASCII characters into mojibake on German (and other
    /// non-English) Windows installs, so every service that captures such output should decode with
    /// <see cref="OemEncoding"/> instead of the .NET default.
    /// </summary>
    internal static class ConsoleEncodingHelper
    {
        [DllImport("kernel32.dll")]
        private static extern int GetOEMCP();

        public static Encoding OemEncoding
        {
            get
            {
                try
                {
                    return Encoding.GetEncoding(GetOEMCP());
                }
                catch (NotSupportedException)
                {
                    // Falls back to the process default if the OEM codepage can't be resolved
                    // (e.g. CodePagesEncodingProvider wasn't registered).
                    return Encoding.Default;
                }
            }
        }
    }
}
