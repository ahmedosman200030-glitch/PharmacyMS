using System;
using System.Security.Cryptography;
using System.Text;

// SECRET must match the one embedded in LicenseService.cs
const string Secret = "REPLACE_WITH_LONG_RANDOM_SECRET_KEY_KEEP_PRIVATE";

Console.Write("Customer name: ");
var customer = Console.ReadLine();

string planCode;
string planName;
while (true)
{
    Console.Write("License plan (monthly/annual): ");
    var planInput = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
    if (planInput == "monthly" || planInput == "m")
    {
        planCode = "M";
        planName = "Monthly";
        break;
    }
    if (planInput == "annual" || planInput == "a" || planInput == "yearly")
    {
        planCode = "A";
        planName = "Annual";
        break;
    }
    Console.WriteLine("Please type \"monthly\" or \"annual\".");
}

Console.Write("License duration in days (e.g. 30 for monthly, 365 for annual): ");
var days = int.Parse(Console.ReadLine()!);

var expiry = DateTime.UtcNow.Date.AddDays(days);
var datePart = expiry.ToString("yyyyMMdd");

var key = GenerateLicenseKey(datePart, planCode, Secret);

Console.WriteLine();
Console.WriteLine($"Customer: {customer}");
Console.WriteLine($"Plan:     {planName}");
Console.WriteLine($"Expires:  {expiry:dd MMM yyyy}");
Console.WriteLine($"License Key: {key}");
Console.WriteLine();
Console.WriteLine($"Note: this key will ONLY activate on the {planName} plan screen.");

static string GenerateLicenseKey(string datePart, string planCode, string secret)
{
    var payload = $"{datePart}-{planCode}";
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var sig = Convert.ToHexString(hash)[..8];
    return $"PMS-{datePart}-{planCode}-{sig}";
}
