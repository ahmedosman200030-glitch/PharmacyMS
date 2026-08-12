namespace PharmacyMS.Application.DTOs;

public class SoundSettings
{
    public bool EnableSystemSounds { get; set; } = true;

    public bool TransactionSuccessEnabled { get; set; } = true;
    public bool ErrorEnabled { get; set; } = true;
    public bool WarningEnabled { get; set; } = true;
    public bool ReceiptPrintEnabled { get; set; } = true;
    public bool BackupCompleteEnabled { get; set; } = true;
    public bool ItemAddedEnabled { get; set; } = true;
    public bool AppStartEnabled { get; set; } = true;
    public bool LogoutEnabled { get; set; } = true;

    public int Volume { get; set; } = 80;
}
