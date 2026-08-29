namespace PharmacyMS.Desktop.Services;

/// <summary>
/// Holds the Resend API key baked in at build time by GitHub Actions.
/// In local dev this is empty — the RESEND_API_KEY environment variable is used instead.
/// </summary>
internal static class ResendApiKeyProvider
{
    internal const string Key = "";
}
