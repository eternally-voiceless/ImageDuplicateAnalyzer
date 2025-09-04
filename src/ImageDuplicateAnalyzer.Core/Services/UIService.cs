using System.IO;
using Spectre.Console;

using ImageDuplicateAnalyzer.Core.Interfaces;

namespace ImageDuplicateAnalyzer.Core.Services;

public class UIService : IUserInterfaceService
{
    public string SelectDirectory(string? title = null)
    {
        string currentDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string parentDir = "📂 ..";
        string confirmation = "✅ Select this directory";

        if (title is not null)
        {
            AnsiConsole.MarkupLine($"[green]{title}[/]");
        }

        while (true)
        {
            var directories = Directory.GetDirectories(currentDir)
                .Select(dir => $"📁 {Path.GetFileName(dir)}")
                .Prepend(parentDir)
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
            else
            {
                currentDir = Path.GetFullPath(Path.Combine(currentDir, selected.Replace("📁 ", "")));
            }
        }

    }

    public string SelectImage(IEnumerable<string> availableImages, string? title = null)
    {
        var images = availableImages.Select(image => $"🖼️ {Path.GetFileName(image)}").ToArray();

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

        return (currentDir is not null) ? Path.Combine(currentDir, selected.Replace("🖼️ ", "")) : string.Empty;
    }
}