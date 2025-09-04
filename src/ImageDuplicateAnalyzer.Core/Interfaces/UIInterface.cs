using ImageDuplicateAnalyzer.Core.Models;

namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IUserInterfaceService
{
    public string SelectDirectory(string? title = null);
    public string SelectImage(IEnumerable<string> availableImages, string? title = null);
    public string GetTestDirectory();
    public void RenderComparisonTable(
        IEnumerable<(IImageDescriptor source, IImageDescriptor target, float similarity)> similarityResults,
        string? caption = null
    );
}