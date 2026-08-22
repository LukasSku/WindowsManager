using System.IO;

namespace WindowsManager.App.Services
{
    public sealed record TempCleanupResult(long BytesFreed, int FilesDeleted, int FilesSkipped);

    /// <summary>
    /// Deletes temporary files from the user's %TEMP% folder and the system-wide
    /// C:\Windows\Temp folder. Files currently in use are silently skipped instead of failing.
    /// </summary>
    public static class TempCleanupService
    {
        public static TempCleanupResult CleanTempFiles()
        {
            long bytesFreed = 0;
            var deleted = 0;
            var skipped = 0;

            var folders = new[]
            {
                Path.GetTempPath(),
                Environment.ExpandEnvironmentVariables(@"%WINDIR%\Temp"),
            }.Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
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

            return new TempCleanupResult(bytesFreed, deleted, skipped);
        }

        private static IEnumerable<string> SafeEnumerateFiles(string folder)
        {
            try
            {
                return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList();
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
