using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [RelayCommand]
    private void BrowsePdf()
    {
        StatusMessage = "Browse PDF clicked";
    }

    [RelayCommand]
    private void ExtractText()
    {
        ExtractedText = "PDF text preview will appear here.";
        StatusMessage = "Extract Text clicked";
    }

    [RelayCommand]
    private void ExportTxt()
    {
        StatusMessage = "Export TXT clicked";
    }
}