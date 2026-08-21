using Velopack;
using Velopack.Sources;

namespace PharmacyMS.Desktop.Services;

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateInstalledAndRestarting,
    NotInstalled,
    Error
}

public record UpdateCheckResult(UpdateCheckStatus Status, string? Message = null);

public static class UpdateService
{
    private const string RepoUrl = "https://github.com/ahmedosman200030-glitch/PharmacyMS";

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var source = new GithubSource(RepoUrl, accessToken: null, prerelease: false);
            var mgr = new UpdateManager(source);

            if (!mgr.IsInstalled)
                return new UpdateCheckResult(UpdateCheckStatus.NotInstalled, "Not running from an installed copy.");

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
                return new UpdateCheckResult(UpdateCheckStatus.UpToDate, "You're on the latest version.");

            await mgr.DownloadUpdatesAsync(newVersion);
            mgr.ApplyUpdatesAndRestart(newVersion);
            return new UpdateCheckResult(UpdateCheckStatus.UpdateInstalledAndRestarting, "Update installed. Restarting...");
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(UpdateCheckStatus.Error, $"Update check failed: {ex.Message}");
        }
    }
}
