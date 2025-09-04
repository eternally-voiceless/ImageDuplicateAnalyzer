namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IUserInterfaceService
{
    public string SelectDirectory(string? title = null);
    public string SelectImage(IEnumerable<string> availableImages, string? title = null);

}