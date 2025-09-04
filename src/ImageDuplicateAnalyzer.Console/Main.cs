using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

using ImageDuplicateAnalyzer.Core;
using ImageDuplicateAnalyzer.Core.Configuration;
using ImageDuplicateAnalyzer.Core.Services;
using ImageDuplicateAnalyzer.Core.Interfaces;

using ImageDuplicateAnalyzer.Console.Extensions;

namespace ImageDuplicateAnalyzer.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddImageAnalyzerServices(context.Configuration);
                services.AddTransient<Preparation>();
                services.AddTransient<Application>();
            })
            .Build();
        await host.Services.GetRequiredService<Preparation>().DownloadModels();
        await host.Services.GetRequiredService<Application>().RunAsync();

    }
}

public class Preparation
{
    private readonly ILogger<Preparation> _logger;
    private readonly IModelDownloadService _downloadService;
    private readonly ModelDownloadOptions _options;

    public Preparation(ILogger<Preparation> logger, IModelDownloadService downloadService, IOptions<ModelDownloadOptions> options)
    {
        _logger = logger;
        _downloadService = downloadService;
        _options = options.Value;
    }

    public async Task DownloadModels()
    {
        var progress = new Progress<string>(message => AnsiConsole.MarkupLine($"[yellow]{message}[/]"));
        await _downloadService.DownloadModelsAsync(_options.ModelsDirectory, progress);
    }
}

public class Application
{
    private readonly ILogger<Application> _logger;
    private readonly IModelDownloadService _downloadService;
    private readonly ModelDownloadOptions _options;
    private readonly IFileService _fileService;
    private readonly IImageEncoder _encoder;
    private readonly IImageAnalysisService _imageAnalysisService;
    private readonly IUserInterfaceService _UI;


    public Application(
        ILogger<Application> logger,
        IModelDownloadService downloadService,
        IOptions<ModelDownloadOptions> modelDownloadOptions,
        IFileService fileService,
        IImageAnalysisService imageAnalysisService,
        IImageEncoder encoder,
        IUserInterfaceService userInterface
    )
    {
        _logger = logger;
        _downloadService = downloadService;
        _options = modelDownloadOptions.Value;
        _fileService = fileService;
        _imageAnalysisService = imageAnalysisService;
        _encoder = encoder;
        _UI = userInterface;
    }

    public async Task RunAsync()
    {
        try
        {
            await Task.Delay(100);

            AnsiConsole.Clear();

            string selectedPath = _UI.SelectDirectory("Select source directory");

            AnsiConsole.Write(new Panel($"[yellow]Selected directory:\n{selectedPath}[/]"));

            string selectedImage = _UI.SelectImage(_fileService.GetAllImages(selectedPath), "Select source image");

            if (selectedImage != string.Empty)
            {
                AnsiConsole.Write(new Panel($"[yellow]Selected source image:\n{selectedImage}[/]"));
            }
            else
            {
                AnsiConsole.MarkupLine("[Red]Incorrect selected image![/]");
                return;
            }

            string path = _UI.SelectDirectory("Select target directory");

            AnsiConsole.Write(new Panel($"[yellow]Target directory:\n{path}[/]"));

            AnsiConsole.MarkupLine("[yellow]Loading...[/]");

            IImageDescriptor defaultDescriptor = _imageAnalysisService.CreateImageDescriptor(selectedImage);

            IEnumerable<string> images = _fileService.GetAllSupportedImages(path);

            if (images.ToArray().Length == 0)
            {
                AnsiConsole.MarkupLine("[red]There is no image in the target directory![/]");
                return;
            }

            IEnumerable<IImageDescriptor> imageDescriptors = images
                .Select(imagePath => _imageAnalysisService.CreateImageDescriptor(imagePath));

            _logger.LogInformation("OK");

            var comparisonResults = imageDescriptors
                .Select(
                    descriptor => (defaultDescriptor, descriptor, defaultDescriptor.CompareEmbedding(descriptor))
                )
                .Where(d => d.Item3 > 0.80)
                .OrderByDescending(pair => pair.Item3);

            _UI.RenderComparisonTable(comparisonResults, "Comparison Results");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application field");
            AnsiConsole.WriteException(ex);
        }
    }
}

