using System.IO;
using System.Runtime.InteropServices;

namespace WindowsManager.App.Services
{
    public sealed record TempCleanupResult(long BytesFreed, int FilesDeleted, int FilesSkipped);

    /// <summary>
    /// One-click cleanup of common "safe to delete" junk: user/system Temp folders, the Windows
    /// Update download cache, the Delivery Optimization cache, Explorer's thumbnail/icon cache, and
    /// the Recycle Bin. Files currently in use (e.g. thumbnail cache files held open by Explorer)
    /// are silently skipped instead of failing.
    /// </summary>
    public static class TempCleanupService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQueryRBInfo pSHQueryRBInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHQueryRBInfo
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        private const uint SherbNoConfirmation = 0x00000001;
        private const uint SherbNoProgressUi = 0x00000002;
        private const uint SherbNoSound = 0x00000004;

        public static TempCleanupResult CleanTempFiles()
        {
            long bytesFreed = 0;
            var deleted = 0;
            var skipped = 0;

            var folders = new[]
            {
                Path.GetTempPath(),
                Environment.ExpandEnvironmentVariables(@"%WINDIR%\Temp"),
                Environment.ExpandEnvironmentVariables(@"%WINDIR%\SoftwareDistribution\Download"),
                Environment.ExpandEnvironmentVariables(@"%WINDIR%\SoftwareDistribution\DeliveryOptimization"),
            }.Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                CleanFolder(folder, ref bytesFreed, ref deleted, ref skipped);
            }

            CleanThumbnailCache(ref bytesFreed, ref deleted, ref skipped);
            EmptyRecycleBin(ref bytesFreed, ref deleted);

            return new TempCleanupResult(bytesFreed, deleted, skipped);
        }

        private static void CleanFolder(string folder, ref long bytesFreed, ref int deleted, ref int skipped)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var file in SafeEnumerateFiles(folder))
            {
                try
                {
                    var info = new FileInfo(file);
                    var size = info.Length;
                    info.Delete();
                    bytesFreed += size;
                    deleted++;
                }
                catch
                {
                    // File is in use, access denied, or already gone - skip it.
                    skipped++;
                }
            }

            foreach (var dir in SafeEnumerateDirectories(folder))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch
                {
                    // Directory not empty (files skipped) or in use - ignore.
                }
            }
        }

        private static void CleanThumbnailCache(ref long bytesFreed, ref int deleted, ref int skipped)
        {
            var folder = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Microsoft\Windows\Explorer");
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var pattern in new[] { "thumbcache_*.db", "iconcache_*.db" })
            {
                foreach (var file in SafeEnumerateFiles(folder, pattern))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var size = info.Length;
                        info.Delete();
                        bytesFreed += size;
                        deleted++;
                    }
                    catch
                    {
                        // Explorer normally has these open - most are only removable after a restart.
                        skipped++;
                    }
                }
            }
        }

        private static void EmptyRecycleBin(ref long bytesFreed, ref int deleted)
        {
            try
            {
                var info = new SHQueryRBInfo { cbSize = Marshal.SizeOf<SHQueryRBInfo>() };
                if (SHQueryRecycleBin(null, ref info) == 0)
                {
                    bytesFreed += info.i64Size;
                    deleted += (int)info.i64NumItems;
                }

                SHEmptyRecycleBin(IntPtr.Zero, null, SherbNoConfirmation | SherbNoProgressUi | SherbNoSound);
            }
            catch
            {
                // Best-effort - an inaccessible/empty Recycle Bin should not fail the whole cleanup.
            }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string folder, string searchPattern = "*")
        {
            try
            {
                return Directory.EnumerateFiles(folder, searchPattern, SearchOption.AllDirectories).ToList();
            }
            catch
            {
                return [];
            }
        }

        private static IEnumerable<string> SafeEnumerateDirectories(string folder)
        {
            try
            {
                return Directory.EnumerateDirectories(folder).ToList();
            }
            catch
            {
                return [];
            }
        }
    }
}

