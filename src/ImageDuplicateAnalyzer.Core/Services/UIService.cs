using System.IO;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ImageDuplicateAnalyzer.Core.Interfaces;
using ImageDuplicateAnalyzer.Core.Configuration;
using ImageDuplicateAnalyzer.Core.Models;

namespace ImageDuplicateAnalyzer.Core.Services;

public class UIService : IUserInterfaceService
{
    private readonly ILogger<UIService> _logger;
    private readonly ModelDownloadOptions _options;
    private readonly TestDirectoryConfiguration _testDataOptions;

    public UIService(ILogger<UIService> logger, IOptions<ModelDownloadOptions> options, IOptions<TestDirectoryConfiguration> testDataOptions)
    {
        _logger = logger;
        _options = options.Value;
        _testDataOptions = testDataOptions.Value;
    }

    public string SelectDirectory(string? title = null)
    {
        string currentDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string parentDir = "📂 ..";
        string confirmation = "✅ Select this directory";
        string preparedDir = "🧪 Test-Data Directory";

        if (title is not null)
        {
            AnsiConsole.MarkupLine($"[green]{title}[/]");
        }

        while (true)
        {
            var directories = Directory.GetDirectories(currentDir)
                .Select(dir => $"📁 {Path.GetFileName(dir)}")
                .Prepend(parentDir)
                .Prepend(preparedDir)
                .Prepend(confirmation)
                .ToArray();

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"📂 [yellow]{currentDir}[/]")
                    .AddChoices(directories)
                    .WrapAround()
            );

            if (selected == confirmation)
            {
                return currentDir;
            }
            else if (selected == parentDir)
            {
                currentDir = Directory.GetParent(currentDir)?.FullName ?? currentDir;
            }
            else if (selected == preparedDir)
            {
                currentDir = GetTestDirectory();
            }
            else
            {
                currentDir = Path.GetFullPath(Path.Combine(currentDir, selected.Replace("📁 ", "")));
            }
        }

    }

    public string SelectImage(IEnumerable<string> availableImages, string? title = null)
    {
        var images = availableImages.Select(image => $"🖼️  {Path.GetFileName(image)}").ToArray();

        if (title is not null)
        {
            AnsiConsole.MarkupLine($"[green]{title}[/]");
        }

        string? currentDir = Path.GetDirectoryName(availableImages.ToArray()[0]);

        if (images.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]No images found in selected directory![/]");
            return string.Empty;
        }

        string selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[gray]Image selection...[/]")
                .AddChoices(images)
                .WrapAround()
        );

        return (currentDir is not null) ? Path.Combine(currentDir, selected.Replace("🖼️  ", "")) : string.Empty;
    }


    public string GetTestDirectory()
    {
        string? testDirName = _testDataOptions.DefaultName;
        if (testDirName is null)
        {
            throw new Exception("Empty testdata directory (see appsetting.json)");
        }
        string projectRootDir = GetProjectRootDir();
        string? testDir = Directory
            .EnumerateDirectories(projectRootDir, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(dir => Path.GetFileName(dir) == testDirName);
        return testDir ?? throw new Exception("Testdata directory not found");
    }

    public void RenderComparisonTable(
        IEnumerable<(IImageDescriptor source, IImageDescriptor target, float similarity)> similarityResults,
        string? caption = null
    )
    {
        try
        {
            var results = similarityResults.ToList();

            if (results.Count == 0)
            {
                throw new ArgumentException("Empty collection");
            }

            var table = new Table()
                .Border(TableBorder.Square)
                .ShowRowSeparators();

            if (caption is not null)
            {
                table.Title($"[yellow]{caption}[/]");
            }

            table.AddColumn(new TableColumn("[yellow]Source File[/]").Centered());
            table.AddColumn(new TableColumn("[yellow]Target File[/]").Centered());
            table.AddColumn(new TableColumn("[yellow]Similarity[/]").Centered());
            table.AddColumn(new TableColumn("[yellow]Full Path[/]").Centered());

            foreach (var r in results)
            {
                (IImageDescriptor source, IImageDescriptor target, float similarity) = r;
                string sourceFileName = Path.GetFileName(source.FilePath);
                string targetFileName = Path.GetFileName(target.FilePath);

                table.AddRow(sourceFileName, targetFileName, $"{similarity}", target.FilePath);
            }

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Displaying error occurred!");
            AnsiConsole.WriteException(ex);
        }
    }
    
    private string GetProjectRootDir()
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
                throw new Exception("Root directory not found");
            }
        }
    }
}