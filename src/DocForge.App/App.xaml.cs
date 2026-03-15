using System.Windows;
using DocForge.App.ViewModels;
using DocForge.App.Views;
using DocForge.Application.Abstractions;
using DocForge.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
namespace DocForge.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        mainWindow.DataContext = mainViewModel;

        await mainViewModel.InitializeAsync();

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddDebug();
        });

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<ITextExportService, TxtExportService>();
        services.AddSingleton<IDocxExportService, DocxExportService>();
        services.AddSingleton<ITextStructureReconstructor, TextStructureReconstructor>();

        services.AddSingleton<ISummaryService>(_ =>
            new OllamaSummaryService(new HttpClient()));
    }
}