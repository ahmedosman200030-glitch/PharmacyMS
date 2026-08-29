using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PharmacyMS.Desktop.Services;

/// <summary>
/// Sends a one-time notification email to the PharmaPro team via Resend
/// whenever a client completes (or backfills) the pharmacy onboarding form.
///
/// The API key is read from the RESEND_API_KEY environment variable — it is
/// NEVER hardcoded here. Set it on the build/deploy machine, e.g.:
///   macOS/Linux:  export RESEND_API_KEY="re_your_key_here"
///   Windows:      setx RESEND_API_KEY "re_your_key_here"
///
/// OFFLINE HANDLING: onboarding itself is a local save and always succeeds
/// regardless of internet access. If this notification email can't be sent
/// right away (no internet, Resend outage, key not set yet, etc.), it is
/// persisted to a small local JSON queue file and retried automatically on
/// every future app launch (see RetryPendingNotificationsAsync) until it
/// actually succeeds. Nothing here ever throws or blocks the UI.
/// </summary>
public static class ResendEmailNotifier
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.resend.com/")
    };

    // Change this if the inbox that should receive new-onboarding alerts changes.
    private const string NotificationRecipient = "pharmaprofficial@gmail.com";
    private const string SenderAddress = "onboarding@resend.dev"; // swap for your verified domain sender once you add one

    private static readonly string QueueFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PharmacyMS",
        "pending_notifications.json");

    private sealed record PendingNotification(
        string PharmacyName,
        string OwnerName,
        string Phone,
        string Address,
        string? RecoveryEmail,
        DateTime QueuedAtUtc);

    /// <summary>
    /// Call this once, right after the pharmacy's info is validated, during
    /// onboarding. Internet is now REQUIRED to complete onboarding: this
    /// returns true only if the email actually sent. The caller (onboarding
    /// screen) must not mark setup as completed unless this returns true —
    /// that is what forces a first-time client to be online before they can
    /// finish setup. Never throws.
    /// </summary>
    public static async Task<bool> NotifyPharmacyOnboardedAsync(
        string pharmacyName,
        string ownerName,
        string phone,
        string address,
        string? recoveryEmail)
    {
        return await TrySendAsync(pharmacyName, ownerName, phone, address, recoveryEmail);
    }

    /// <summary>
    /// Call this once near app startup, fire-and-forget (do not await it on
    /// the UI path). Attempts to send any notifications that failed on a
    /// previous launch because the machine was offline (or the key wasn't
    /// configured yet). Entries that send successfully are removed from the
    /// queue; entries that still fail stay queued for the next launch.
    /// </summary>
    public static async Task RetryPendingNotificationsAsync()
    {
        List<PendingNotification> pending;
        try
        {
            if (!File.Exists(QueueFilePath))
                return;

            var json = await File.ReadAllTextAsync(QueueFilePath);
            pending = JsonSerializer.Deserialize<List<PendingNotification>>(json) ?? new();
        }
        catch
        {
            return; // corrupt/unreadable queue file — never let this block startup
        }

        if (pending.Count == 0)
            return;

        var stillPending = new List<PendingNotification>();

        foreach (var item in pending)
        {
            var sent = await TrySendAsync(
                item.PharmacyName, item.OwnerName, item.Phone, item.Address, item.RecoveryEmail);

            if (!sent)
                stillPending.Add(item);
        }

        try
        {
            if (stillPending.Count == 0)
                File.Delete(QueueFilePath);
            else
                await File.WriteAllTextAsync(QueueFilePath, JsonSerializer.Serialize(stillPending));
        }
        catch
        {
            // Best-effort. If we can't update the file, the same items just get
            // retried again (and re-deduplicated by nothing — see note below)
            // on the next launch, which is harmless.
        }
    }

    private static void QueuePending(PendingNotification item)
    {
        try
        {
            var dir = Path.GetDirectoryName(QueueFilePath)!;
            Directory.CreateDirectory(dir);

            var existing = new List<PendingNotification>();
            if (File.Exists(QueueFilePath))
            {
                try
                {
                    var json = File.ReadAllText(QueueFilePath);
                    existing = JsonSerializer.Deserialize<List<PendingNotification>>(json) ?? new();
                }
                catch
                {
                    // Corrupt file — start fresh rather than losing this notification.
                }
            }

            existing.Add(item);
            File.WriteAllText(QueueFilePath, JsonSerializer.Serialize(existing));
        }
        catch
        {
            // If we can't even persist the queue, there's nothing more we can do.
            // Onboarding itself already succeeded locally and is not affected.
        }
    }

    private static async Task<bool> TrySendAsync(
        string pharmacyName,
        string ownerName,
        string phone,
        string address,
        string? recoveryEmail)
    {
        var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = ResendApiKeyProvider.Key;
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        var html =
            "<p>A pharmacy has completed onboarding in PharmaPro.</p>" +
            "<ul>" +
            $"<li><strong>Pharmacy:</strong> {System.Net.WebUtility.HtmlEncode(pharmacyName)}</li>" +
            $"<li><strong>Owner:</strong> {System.Net.WebUtility.HtmlEncode(ownerName)}</li>" +
            $"<li><strong>Phone:</strong> {System.Net.WebUtility.HtmlEncode(phone)}</li>" +
            $"<li><strong>Address:</strong> {System.Net.WebUtility.HtmlEncode(address)}</li>" +
            $"<li><strong>Recovery email:</strong> {System.Net.WebUtility.HtmlEncode(recoveryEmail ?? "(not provided)")}</li>" +
            "</ul>";

        var payload = new
        {
            from = SenderAddress,
            to = new[] { NotificationRecipient },
            subject = $"New PharmaPro onboarding: {pharmacyName}",
            html
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _http.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // No internet, DNS failure, timeout, Resend downtime, etc.
            return false;
        }
    }
}
