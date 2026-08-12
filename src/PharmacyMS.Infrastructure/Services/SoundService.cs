using System;
using System.Diagnostics;
using System.Collections.Generic;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Enums;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Infrastructure.Services;

public class SoundService : ISoundService
{
    private readonly ISoundSettingsRepository _settingsRepo;

    private static readonly Dictionary<SoundEvent, string> FileMap = new()
    {
        [SoundEvent.TransactionSuccess] = "transaction-success.wav",
        [SoundEvent.Error]              = "error.wav",
        [SoundEvent.Warning]            = "warning.wav",
        [SoundEvent.ReceiptPrint]       = "print.wav",
        [SoundEvent.BackupComplete]     = "backup-complete.wav",
        [SoundEvent.ItemAdded]          = "item-added.wav",
        [SoundEvent.AppStart]           = "app-start.wav",
        [SoundEvent.Logout]             = "logout.wav",
    };

    public SoundService(ISoundSettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    public void Play(SoundEvent soundEvent)
    {
        var settings = _settingsRepo.Load();
        if (!settings.EnableSystemSounds) return;
        if (!IsEventEnabled(settings, soundEvent)) return;
        PlayFile(soundEvent, settings.Volume);
    }

    public void TestSound(SoundEvent soundEvent)
    {
        var settings = _settingsRepo.Load();
        PlayFile(soundEvent, settings.Volume);
    }

    private static bool IsEventEnabled(SoundSettings s, SoundEvent e) => e switch
    {
        SoundEvent.TransactionSuccess => s.TransactionSuccessEnabled,
        SoundEvent.Error              => s.ErrorEnabled,
        SoundEvent.Warning            => s.WarningEnabled,
        SoundEvent.ReceiptPrint       => s.ReceiptPrintEnabled,
        SoundEvent.BackupComplete     => s.BackupCompleteEnabled,
        SoundEvent.ItemAdded          => s.ItemAddedEnabled,
        SoundEvent.AppStart           => s.AppStartEnabled,
        SoundEvent.Logout             => s.LogoutEnabled,
        _                             => true
    };

    private void PlayFile(SoundEvent soundEvent, int volumePercent)
    {
        try
        {
            var fileName = FileMap[soundEvent];
            var path = System.IO.Path.Combine(
                AppContext.BaseDirectory, "Assets", "Sounds", fileName);

            if (!System.IO.File.Exists(path)) return;

            var vol = Math.Clamp(volumePercent / 100.0, 0.0, 1.0).ToString("0.00");
            Process.Start(new ProcessStartInfo
            {
                FileName        = "afplay",
                Arguments       = $"-v {vol} \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow  = true
            });
        }
        catch
        {
            // never crash the app over a missing sound
        }
    }
}
