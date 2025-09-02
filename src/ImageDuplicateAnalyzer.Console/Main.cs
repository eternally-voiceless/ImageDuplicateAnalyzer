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


    public Application(
        ILogger<Application> logger,
        IModelDownloadService downloadService,
        IOptions<ModelDownloadOptions> modelDownloadOptions,
        IFileService fileService,
        IImageAnalysisService imageAnalysisService,
        IImageEncoder encoder
    )
    {
        _logger = logger;
        _downloadService = downloadService;
        _options = modelDownloadOptions.Value;
        _fileService = fileService;
        _imageAnalysisService = imageAnalysisService;
        _encoder = encoder;
    }

    public async Task RunAsync()
    {
        try
        {

            await Task.Delay(300);

            AnsiConsole.Clear();

            string selectedPath = SelectDirectory("Select source directory");

            AnsiConsole.Write(new Panel($"[yellow]Selected directory:\n{selectedPath}[/]"));

            string selectedImage = SelectImage(selectedPath, "Select source image");

            if (selectedImage != string.Empty)
            {
                AnsiConsole.Write(new Panel($"[yellow]Selected source image:\n{selectedImage}[/]"));
            }
            else
            {
                return;
            }

            string path = SelectDirectory("Select target directory");

            AnsiConsole.Write(new Panel($"[yellow]Target directory:\n{path}[/]"));

            AnsiConsole.MarkupLine("[yellow]Loading...[/]");

            IImageDescriptor defaultDescriptor = _imageAnalysisService.CreateImageDescriptor(selectedImage);

            IEnumerable<string> images = _fileService.GetAllSupportedImages(path);

            if (images.ToArray().Length == 0)
            {
                AnsiConsole.MarkupLine("[red]There is no image in the target directory[/]");
                return;
            }

            IEnumerable<IImageDescriptor> imageDescriptors = images
                .Select(imagePath => _imageAnalysisService.CreateImageDescriptor(imagePath));

            _logger.LogInformation("OK");

            var imagePairs = imageDescriptors
                .Select(
                    descriptor => (defaultDescriptor, descriptor, defaultDescriptor.CompareEmbedding(descriptor))
                )
                .Where(d => d.Item3 > 0.80)
                .OrderByDescending(pair => pair.Item3);

            DisplayImageComparison(imagePairs);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application field");
            AnsiConsole.WriteException(ex);
        }


    }

    private void DisplayImageComparison(
        IEnumerable<(IImageDescriptor descriptor1, IImageDescriptor descriptor2, float similarity)> pairCollection,
        string? tableCaption = null
    )
    {
        try
        {
            List<(IImageDescriptor descriptor1, IImageDescriptor descriptor2, float similarity)> pairs = pairCollection.ToList();

            if (pairs.Count == 0)
            {
                throw new ArgumentException("Empty collection");
            }

            var table = new Table()
                .Border(TableBorder.Square)
                .ShowRowSeparators();

            if (tableCaption is not null)
            {
                table.Title($"[bold yellow]{tableCaption}[/]");
            }

            table.AddColumn(new TableColumn("[yellow]Source file[/]").Centered());
            table.AddColumn(new TableColumn("[yellow]Target file[/]").Centered());
            table.AddColumn(new TableColumn("[yellow]Similarity[/]").Centered());

            foreach (var pair in pairs)
            {
                (IImageDescriptor source, IImageDescriptor target, float similarity) = pair;
                string sourceImageName = Path.GetFileName(source.FilePath);
                string targetImageName = Path.GetFileName(target.FilePath);

                table.AddRow(sourceImageName, targetImageName, $"{similarity:F6}");
            }

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Displaying error occurred");
            AnsiConsole.WriteException(ex);
        }
    }

    private void DisplayFiles(
        IEnumerable<string> fileCollection,
        string? tableCaption = null
    )
    {
        try
        {
            List<string> files = fileCollection.ToList();

            if (files.Count == 0)
            {
                throw new ArgumentException("Empty collection");
            }

            var table = new Table()
                .Border(TableBorder.Square)
                .ShowRowSeparators();
            if (tableCaption is not null)
            {

                table.Title($"[bold yellow]{tableCaption}[/]");
            }

            table.AddColumn(new TableColumn("Files").Centered());

            files.ForEach(file => table.AddRow(Path.GetFileName(file)));

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Displaying error occurred");
            AnsiConsole.WriteException(ex);
        }
    }

    private string GetTestDirectory()
    {
        string testDirName = "testdata";
        string projectRootDir = GetProjectRootDirectory();
        var testDir = Directory
            .EnumerateDirectories(projectRootDir, "*", SearchOption.AllDirectories)
            .FirstOrDefault(dir => Path.GetFileName(dir) == testDirName);
        return testDir ?? throw new Exception("Test directory not found");
    }

    private string GetProjectRootDirectory()
    {
        string? currentDir = Directory.GetCurrentDirectory();
        string target = _options.RootDirName;

        while (true)
        {
            if (Path.GetFileName(currentDir) == target)
            {
                return currentDir;
            }
            currentDir = Directory.GetParent(currentDir)?.FullName;

            if (currentDir is null)
            {
                throw new Exception("Root dir not found");
            }
        }
    }

    private string SelectImage(string path, string? title = null)
    {
        var files = _fileService.GetAllSupportedImages(path);
        var fileNames = files.Select(image => $"🖼️ {Path.GetFileName(image)}").ToArray();

        if (title is not null)
        {
            AnsiConsole.MarkupLine($"[green]{title}[/]");
        }

        if (fileNames.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No images found in selected directory![/]");
            return string.Empty;
        }

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[gray]{fileNames[0]}[/]")
                .AddChoices(fileNames)
                .WrapAround()
        );
        return Path.Combine(path, choice.Replace("🖼️ ", ""));
    }

    private string SelectDirectory(string? title = null)
    {
        string currentDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string test1 = Path.Combine(GetTestDirectory(), "Test1");

        string parentDir = "📂 ..";
        string selection = "✅ Select this directory";
        string prepared = "📚 Prepared test directory (Test1)";

        if (title is not null)
        {
            AnsiConsole.MarkupLine($"[green]{title}[/]");
        }


        while (true)
        {
            var directories = Directory.GetDirectories(currentDir)
                .Select(dir => $"📁 {Path.GetFileName(dir)}")
                .Prepend(parentDir)
                .Prepend(selection)
                .Prepend(prepared)
                .ToArray();

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"📂 [yellow]{currentDir}[/]")
                    .AddChoices(directories)
                    .WrapAround()
            );

            if (selected == selection)
            {
                return currentDir;
            }
            else if (selected == prepared)
            {
                return test1;
            }
            else if (selected == parentDir)
            {
                currentDir = Directory.GetParent(currentDir)?.FullName ?? currentDir;
            }
            else
            {
                currentDir = Path.GetFullPath(Path.Combine(currentDir, selected.Replace("📁 ", "")));
            }
        }
    }
}

