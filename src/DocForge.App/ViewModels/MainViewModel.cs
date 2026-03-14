using CommunityToolkit.Mvvm.ComponentModel;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";
}