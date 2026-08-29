using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace PharmacyMS.Desktop.Services;

/// <summary>
/// The single source of truth for "what version is this app" anywhere in the UI.
///
/// Priority order:
///   1. Velopack's UpdateManager.CurrentVersion — the REAL version of the
///      installed release the client is actually running right now. This is
///      what changes automatically the moment a client updates, with no
///      manual edits needed anywhere in the app.
///   2. The version embedded in the assembly at publish time (set via
///      -p:Version in the release workflow). Used as a fallback when not
///      running from a Velopack-installed copy.
///   3. "Dev" — used for local `dotnet run` builds where neither of the
///      above is set, so it's obvious at a glance this isn't a real release.
/// </summary>
public static class AppVersionService
{
    private const string RepoUrl = "https://github.com/ahmedosman200030-glitch/PharmacyMS";
    private static string? _cached;

    public static string GetVersion()
    {
        if (_cached != null)
            return _cached;

        try
        {
            var mgr = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
            if (mgr.IsInstalled && mgr.CurrentVersion != null)
            {
                _cached = mgr.CurrentVersion.ToString();
                return _cached;
            }
        }
        catch
        {
            // Not installed via Velopack, or the update manager couldn't initialize — fall through.
        }

        var asmVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(asmVersion))
        {
            // .NET appends build metadata like "+abcdef1234..." to the informational
            // version at publish time — strip it so the UI only shows e.g. "1.1.5".
            var plusIndex = asmVersion.IndexOf('+');
            _cached = plusIndex >= 0 ? asmVersion[..plusIndex] : asmVersion;
            return _cached;
        }

        _cached = "Dev";
        return _cached;
    }
}
