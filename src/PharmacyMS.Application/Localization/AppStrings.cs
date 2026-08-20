namespace PharmacyMS.Application.Localization;

public static class AppStrings
{
    // key -> (English, Somali)
    private static readonly Dictionary<string, (string En, string So)> Map = new()
    {
        // Receipt
        ["Invoice"] = ("Invoice", "Rasiidka"),
        ["Date"] = ("Date", "Taariikhda"),
        ["Cashier"] = ("Cashier", "Iibiyaha"),
        ["Customer"] = ("Customer", "Macmiilka"),
        ["WalkInCustomer"] = ("Walk-in Customer", "Macmiil Booqasho ah"),
        ["Item"] = ("Item", "Alaabta"),
        ["Qty"] = ("Qty", "Tirada"),
        ["Price"] = ("Price", "Qiimaha"),
        ["Total"] = ("Total", "Wadarta"),
        ["Subtotal"] = ("Subtotal", "Wadarta Hoose"),
        ["Discount"] = ("Discount", "Dhimis"),
        ["Tax"] = ("Tax", "Canshuur"),
        ["TOTAL"] = ("TOTAL", "WADARTA GUUD"),
        ["Payment"] = ("Payment", "Lacag bixin"),
        ["Change"] = ("Change", "Baaqiga"),
        ["ThankYou"] = ("Thank you for your purchase!", "Waad ku mahadsan tahay iibsigaaga!"),
        ["AdminSystem"] = ("Admin System", "Nidaamka Maamulaha"),

        // Common UI labels (Phase 2 will consume these)
        ["Settings"] = ("Settings", "Dejinta"),
        ["Save"] = ("Save", "Kaydi"),
        ["Cancel"] = ("Cancel", "Jooji"),
        ["Search"] = ("Search", "Raadi"),
        ["Medicine"] = ("Medicine", "Daawada"),
        ["Category"] = ("Category", "Qaybta"),
        ["Quantity"] = ("Quantity", "Tirada"),
        ["Stock"] = ("Stock", "Kaydka"),
        ["Add"] = ("Add", "Kudar"),
        ["Remove"] = ("Remove", "Ka saar"),
        ["Reason"] = ("Reason", "Sababta"),
        ["Notes"] = ("Notes", "Faallooyin"),
    };

    public static string Get(string key, string lang)
    {
        if (!Map.TryGetValue(key, out var pair)) return key;
        return string.Equals(lang, "so", StringComparison.OrdinalIgnoreCase) ? pair.So : pair.En;
    }
}
