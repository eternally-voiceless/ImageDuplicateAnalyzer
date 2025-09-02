namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IFileService
{
    IEnumerable<string> GetAllFiles(string path, string[]? extensions = null);
    IEnumerable<string> GetAllImages(string path);
    IEnumerable<string> GetAllSupportedImages(string root);
    bool IsImageFile(string path);
}