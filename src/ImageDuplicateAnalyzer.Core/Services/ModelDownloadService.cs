using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Options;

using ImageDuplicateAnalyzer.Core.Configuration;
using ImageDuplicateAnalyzer.Core.Interfaces;

public class ModelDownloadService : IModelDownloadService
{
    private readonly HttpClient _http;
    private readonly ILogger<ModelDownloadService> _logger;
    private readonly ModelConfiguration _modelConfig;

    public ModelDownloadService(
        HttpClient httpClient,
        ILogger<ModelDownloadService> logger,
        IOptions<ModelConfiguration> modelOptions
    )
    {
        _http = httpClient;
        _logger = logger;
        _modelConfig = modelOptions.Value;
    }

    public async Task DownloadModelsAsync(string modelsDir = "DefaultModelDirectory", IProgress<string>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(modelsDir);
        await DownloadFileAsync(
            _modelConfig.VisualModelUrl,
            Path.Combine(modelsDir, _modelConfig.VisualModelFileName),
            progress, ct
        );
        await DownloadFileAsync(
            _modelConfig.TextModelUrl,
            Path.Combine(modelsDir, _modelConfig.TextModelFileName),
            progress, ct
        );
    }

    private async Task DownloadFileAsync(
        string url, string outputPath, IProgress<string>? progress, CancellationToken ct
    )
    {
        if (File.Exists(outputPath))
        {
            _logger?.LogInformation($"Model already exists: {Path.GetFullPath(outputPath)}");
            progress?.Report($"Model already exists: {Path.GetFullPath(outputPath)}");
            return;
        }

        var fileName = Path.GetFileName(url);
        progress?.Report($"Loading model: {fileName}...");

        using HttpResponseMessage response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        progress?.Report($"Total size: {(totalBytes is not null ? $"{totalBytes / (1024.0 * 1024.0):F1} MB" : "Unknown Size")}");

        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            BufferSize = 1 << 16
        };

        await using FileStream fs = new FileStream(outputPath, options);
        await using Stream rs = await response.Content.ReadAsStreamAsync(ct);

        await CopyStreamWithProgressAsync(rs, fs, totalBytes, fileName, progress, ct);

        progress?.Report($"✅ Successfully downloaded: {fileName}");
        _logger.LogInformation("Downloaded {FileName} to {Path}", fileName, Path.GetFullPath(outputPath));
    }

    private static async Task CopyStreamWithProgressAsync(
        Stream source, Stream destination, long? totalBytes, string fileName, IProgress<string>? progress, CancellationToken ct
    )
    {
        var buffer = new byte[1 << 18].AsMemory();
        long totalRead = 0;
        var lastReport = Stopwatch.StartNew();

        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            await destination.WriteAsync(buffer[..read], ct);
            totalRead += read;

            if (lastReport.Elapsed >= TimeSpan.FromSeconds(10))
            {
                var downloadedMB = totalRead / (1024.0 * 1024.0);

                if (totalBytes.HasValue)
                {
                    var totalMB = totalBytes.Value / (1024.0 * 1024.0);
                    var percentage = totalRead * 100.0 / totalBytes.Value;
                    progress?.Report($"📥 {fileName}: {downloadedMB:F2}/{totalMB:F2} MB ({percentage:F2} %)");
                }
                else
                {
                    progress?.Report($"📥 {fileName}: {downloadedMB:F2} MB");
                }
                lastReport.Restart();
            }
        }
    }
}   

