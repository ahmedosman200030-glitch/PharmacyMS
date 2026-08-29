namespace PharmacyMS.Application.Interfaces.Services;

public interface IEmailService
{
    Task<bool> SendAsync(string toAddress, string subject, string body);
}
