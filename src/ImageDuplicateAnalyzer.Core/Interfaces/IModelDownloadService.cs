namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IModelDownloadService
{
    Task DownloadModelsAsync(string path, IProgress<string>? progress, CancellationToken ct = default);
}