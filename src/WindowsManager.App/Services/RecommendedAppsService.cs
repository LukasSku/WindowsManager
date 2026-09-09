namespace WindowsManager.App.Services
{
    public sealed record RecommendedApp(string DisplayName, string WingetId, string Description);

    /// <summary>
    /// A small, curated list of popular, generally trusted applications offered as one-click
    /// installs (via winget) on the App Manager page, so users don't have to search for them first.
    /// </summary>
    public static class RecommendedAppsService
    {
        public static readonly IReadOnlyList<RecommendedApp> Apps = new List<RecommendedApp>
        {
            new("Google Chrome", "Google.Chrome", "Popular web browser."),
            new("Mozilla Firefox", "Mozilla.Firefox", "Popular open-source web browser."),
            new("7-Zip", "7zip.7zip", "Free file archiver (zip/rar/7z etc.)."),
            new("VLC media player", "VideoLAN.VLC", "Plays almost any video/audio format."),
            new("Notepad++", "Notepad++.Notepad++", "Lightweight text/code editor."),
            new("Visual Studio Code", "Microsoft.VisualStudioCode", "Popular code editor."),
            new("PowerToys", "Microsoft.PowerToys", "Official Microsoft utilities (window management, etc.)."),
            new("Discord", "Discord.Discord", "Voice/text chat, popular with gaming communities."),
        };
    }
}
