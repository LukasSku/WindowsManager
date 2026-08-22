using Velopack;
using Velopack.Sources;

namespace WindowsManager.App.Services
{
    public sealed record UpdateCheckResult(bool UpdateAvailable, string? NewVersion, string? Error);

    /// <summary>
    /// Wraps Velopack's update check/download/apply flow, pointed at this repository's GitHub
    /// Releases. Velopack reads the release assets it itself published via "vpk pack"/"vpk upload"
    /// (a "releases" feed + delta packages), so this only works once at least one release has been
    /// published that way - see .github/copilot-instructions.md for the release process.
    /// </summary>
    public static class UpdateService
    {
        private const string GitHubRepoUrl = "https://github.com/LukasSku/WindowsManager";

        private static UpdateManager CreateManager() => new(new GithubSource(GitHubRepoUrl, null, false));

        /// <summary>
        /// Checks for a newer version. Returns UpdateAvailable=false (no error) when already on the
        /// latest version, or when running outside of an installed Velopack app (e.g. during local
        /// development via "dotnet build"), since there is nothing to update in that case.
        /// </summary>
        public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                var manager = CreateManager();
                if (!manager.IsInstalled)
                {
                    return new UpdateCheckResult(false, null, null);
                }

                var updateInfo = await manager.CheckForUpdatesAsync();
                if (updateInfo is null)
                {
                    return new UpdateCheckResult(false, null, null);
                }

                return new UpdateCheckResult(true, updateInfo.TargetFullRelease.Version.ToString(), null);
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult(false, null, ex.Message);
            }
        }

        /// <summary>
        /// Downloads and applies the pending update, then restarts the app into the new version.
        /// Must only be called after CheckForUpdatesAsync reported an update is available.
        /// </summary>
        public static async Task<string?> DownloadAndApplyUpdateAsync()
        {
            try
            {
                var manager = CreateManager();
                var updateInfo = await manager.CheckForUpdatesAsync();
                if (updateInfo is null)
                {
                    return "No update available.";
                }

                await manager.DownloadUpdatesAsync(updateInfo);
                manager.ApplyUpdatesAndRestart(updateInfo);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
