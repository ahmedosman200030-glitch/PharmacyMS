using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly ResendConfigService _resendConfigService;
    private static readonly HttpClient _http = new();

    public SmtpEmailService(ResendConfigService resendConfigService)
    {
        _resendConfigService = resendConfigService;
    }

    public async Task<bool> SendAsync(string toAddress, string subject, string body)
    {
        var config = _resendConfigService.Load();

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            Console.WriteLine("RESEND SEND FAILED: No API key configured.");
            return false;
        }

        try
        {
            var payload = new
            {
                from = config.FromAddress,
                to = new[] { toAddress },
                subject = subject,
                html = $"<p>{body.Replace("\n", "<br/>")}</p>"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"RESEND SEND FAILED: {response.StatusCode} — {responseBody}");
                return false;
            }

            Console.WriteLine("RESEND SEND OK: " + responseBody);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("RESEND SEND FAILED: " + ex);
            return false;
        }
    }
}
