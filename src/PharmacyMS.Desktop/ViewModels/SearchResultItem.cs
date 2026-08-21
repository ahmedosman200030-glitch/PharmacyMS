using System;
using System.Windows.Input;

namespace PharmacyMS.Desktop.ViewModels;

public class SearchResultItem
{
    public string Icon { get; set; } = "🔎";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public ICommand NavigateCommand { get; set; } = null!;
}
