namespace PharmacyMS.Infrastructure.Data;

public class ResendConfig
{
    public string? ApiKey { get; set; }
    public string FromAddress { get; set; } = "onboarding@resend.dev";
}
