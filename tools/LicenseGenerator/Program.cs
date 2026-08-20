using System;
using System.Security.Cryptography;
using System.Text;

// SECRET must match the one embedded in LicenseService.cs
const string Secret = "REPLACE_WITH_LONG_RANDOM_SECRET_KEY_KEEP_PRIVATE";

Console.Write("Customer name: ");
var customer = Console.ReadLine();

Console.Write("License duration in days (e.g. 365): ");
var days = int.Parse(Console.ReadLine()!);

var expiry = DateTime.UtcNow.Date.AddDays(days);
var payload = expiry.ToString("yyyyMMdd");

var key = GenerateLicenseKey(payload, Secret);

Console.WriteLine();
Console.WriteLine($"Customer: {customer}");
Console.WriteLine($"Expires:  {expiry:dd MMM yyyy}");
Console.WriteLine($"License Key: {key}");

static string GenerateLicenseKey(string payload, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var sig = Convert.ToHexString(hash)[..8];
    return $"PMS-{payload}-{sig}";
}
