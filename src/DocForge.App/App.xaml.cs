using System.Windows;
using DocForge.App.ViewModels;
using DocForge.App.Views;
using DocForge.Application.Abstractions;
using DocForge.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DocForge.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                "logs/docforge-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
                services.AddSingleton<ITextExportService, TxtExportService>();
                services.AddSingleton<IDocxExportService, DocxExportService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        Log.Information("Application started");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}