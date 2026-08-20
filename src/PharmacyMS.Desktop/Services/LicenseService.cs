using System;
using System.Security.Cryptography;
using System.Text;

namespace PharmacyMS.Desktop.Services;

public record LicenseInfo(bool IsValid, DateTime? ExpiryDate, string? ErrorMessage);

public static class LicenseService
{
    // Must match the secret used in the offline generator tool
    private const string Secret = "REPLACE_WITH_LONG_RANDOM_SECRET_KEY_KEEP_PRIVATE";

    public static LicenseInfo Validate(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return new LicenseInfo(false, null, "No license key entered.");

        var parts = licenseKey.Trim().Split('-');
        if (parts.Length != 3 || parts[0] != "PMS")
            return new LicenseInfo(false, null, "Invalid license key format.");

        var payload = parts[1];
        var providedSig = parts[2];

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSig = Convert.ToHexString(hash)[..8];

        if (!string.Equals(providedSig, expectedSig, StringComparison.OrdinalIgnoreCase))
            return new LicenseInfo(false, null, "License key is invalid or has been tampered with.");

        if (!DateTime.TryParseExact(payload, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var expiry))
            return new LicenseInfo(false, null, "License key is corrupted.");

        if (expiry < DateTime.UtcNow.Date)
            return new LicenseInfo(false, expiry, "License has expired.");

        return new LicenseInfo(true, expiry, null);
    }
}
