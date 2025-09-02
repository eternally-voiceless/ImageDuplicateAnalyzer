using System.IO;

using Microsoft.Extensions.Logging;
using ImageDuplicateAnalyzer.Core.Interfaces;

namespace ImageDuplicateAnalyzer.Core.Services;

public class FileService : IFileService
{
    protected readonly ILogger<FileService> _logger;
    protected static readonly string[] _basicImageExtensions = { ".png", ".apng", ".avif", ".gif", ".jpg", ".jpeg", ".jfif", ".pjpeg", ".pjp", ".svg", ".webp" };
    protected static readonly string[] _additionalImageExtensions = { ".bmp", ".ico", ".cur", ".tif", ".tiff" };
    protected static readonly string[] _supportedImageExtensions = { ".qoi", ".jpeg", ".tga", ".gif", ".webp", ".png", ".pbm", ".bmp", ".tiff"};
    protected static readonly string[] _imageExtensions = _basicImageExtensions.Concat(_additionalImageExtensions).Concat(_supportedImageExtensions).ToArray();

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> GetAllFiles(string path, string[]? extensions = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            _logger.LogError("Directory not found: {path}", path);
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        EnumerationOptions options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        return Directory.EnumerateFiles(path, "*", options)
            .Where(file => extensions is null || extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
    }

    public IEnumerable<string> GetAllImages(string root)
    {
        return GetAllFiles(root, _basicImageExtensions.Concat(_additionalImageExtensions).ToArray());
    }

    public IEnumerable<string> GetAllSupportedImages(string root)
    {
        return GetAllFiles(root, _supportedImageExtensions);
    }

    public bool IsImageFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        string extension = Path.GetExtension(path);
        return _imageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}