using System.Windows;
using DocForge.App.ViewModels;
using DocForge.App.Views;
using DocForge.Application.Abstractions;
using DocForge.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocForge.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
                services.AddSingleton<ITextExportService, TxtExportService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}