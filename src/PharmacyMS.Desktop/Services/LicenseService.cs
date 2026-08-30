using System;
using System.Security.Cryptography;
using System.Text;

namespace PharmacyMS.Desktop.Services;

public record LicenseInfo(bool IsValid, DateTime? ExpiryDate, string? ErrorMessage, string? PlanType = null);

public static class LicenseService
{
    // Must match the secret used in the offline generator tool
    private const string Secret = "REPLACE_WITH_LONG_RANDOM_SECRET_KEY_KEEP_PRIVATE";

    // Validates a license key. expectedPlan is optional ("Monthly" or "Annual"):
    // when given, a key that embeds a specific plan type must match it, or
    // validation fails with a clear "wrong plan" message. Legacy 3-part keys
    // (no plan embedded — e.g. the free trial key) are plan-agnostic and
    // always pass the plan check.
    public static LicenseInfo Validate(string licenseKey, string? expectedPlan = null)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return new LicenseInfo(false, null, "No license key entered.");

        var parts = licenseKey.Trim().Split('-');
        if ((parts.Length != 3 && parts.Length != 4) || parts[0] != "PMS")
            return new LicenseInfo(false, null, "Invalid license key format.");

        string payload;
        string providedSig;
        string? planCode = null;

        if (parts.Length == 4)
        {
            // New format: PMS-{expiryYYYYMMDD}-{planCode}-{sig}
            planCode = parts[2].ToUpperInvariant();
            if (planCode != "M" && planCode != "A")
                return new LicenseInfo(false, null, "Invalid license key format.");
            payload = $"{parts[1]}-{planCode}";
            providedSig = parts[3];
        }
        else
        {
            // Legacy format: PMS-{expiryYYYYMMDD}-{sig}. Previously treated as
            // valid for either plan; now deliberately rejected so any client
            // still on an old-format key is forced back to the Plans screen
            // to pick Monthly/Annual and activate a new plan-locked key.
            return new LicenseInfo(false, null,
                "Your license needs to be renewed. Please choose a plan to continue.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSig = Convert.ToHexString(hash)[..8];

        if (!string.Equals(providedSig, expectedSig, StringComparison.OrdinalIgnoreCase))
            return new LicenseInfo(false, null, "License key is invalid or has been tampered with.");

        if (!DateTime.TryParseExact(parts[1], "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var expiry))
            return new LicenseInfo(false, null, "License key is corrupted.");

        var planName = PlanNameFromCode(planCode);

        if (expiry < DateTime.UtcNow.Date)
            return new LicenseInfo(false, expiry, "License has expired.", planName);

        if (expectedPlan != null && planName != null &&
            !string.Equals(planName, expectedPlan, StringComparison.OrdinalIgnoreCase))
        {
            return new LicenseInfo(false, expiry,
                $"This license key is for the {planName} plan and cannot be used for {expectedPlan}.", planName);
        }

        return new LicenseInfo(true, expiry, null, planName);
    }

    private static string? PlanNameFromCode(string? code) => code switch
    {
        "M" => "Monthly",
        "A" => "Annual",
        _ => null
    };

    // Generates a real, self-signed license key locked to a specific plan
    // (Monthly or Annual). A Monthly key will fail Validate(key, "Annual")
    // and vice versa. Use this for testing the plan-lock behavior, e.g.:
    //   LicenseService.GenerateLicenseKey("Monthly", 30)
    //   LicenseService.GenerateLicenseKey("Annual", 365)
    public static string GenerateLicenseKey(string planType, int days)
    {
        var planCode = planType.Equals("Annual", StringComparison.OrdinalIgnoreCase) ? "A" : "M";
        var expiry = DateTime.UtcNow.Date.AddDays(days);
        var datePart = expiry.ToString("yyyyMMdd");
        var payload = $"{datePart}-{planCode}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sig = Convert.ToHexString(hash)[..8];

        return $"PMS-{datePart}-{planCode}-{sig}";
    }

    // Generates a real, self-signed 30-day license key using this app's own
    // Secret. Used for the free-trial flow so it needs no separate tracking -
    // it expires and gets re-validated exactly like a purchased key. Uses the
    // legacy plan-agnostic format on purpose, since the trial isn't Monthly
    // or Annual.
    public static string GenerateTrialLicenseKey(int days = 30)
    {
        var expiry = DateTime.UtcNow.Date.AddDays(days);
        var payload = expiry.ToString("yyyyMMdd");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sig = Convert.ToHexString(hash)[..8];

        return $"PMS-{payload}-{sig}";
    }
}
