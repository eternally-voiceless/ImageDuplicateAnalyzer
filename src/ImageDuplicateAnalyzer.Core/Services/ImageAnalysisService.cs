using Microsoft.Extensions.Logging;

using ImageDuplicateAnalyzer.Core.Interfaces;
using ImageDuplicateAnalyzer.Core.Services;
using ImageDuplicateAnalyzer.Core.Models;

namespace ImageDuplicateAnalyzer.Core.Services;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly ILogger<ImageAnalysisService> _logger;
    private readonly IFileService _fileService;
    private readonly IImageEncoder _encoder;
    public ImageAnalysisService(ILogger<ImageAnalysisService> logger, IFileService fileService, IImageEncoder encoder)
    {
        _logger = logger;
        _fileService = fileService;
        _encoder = encoder;
    }

    public IImageDescriptor CreateImageDescriptor(string path)
    {
        try
        {
            if (!_fileService.IsImageFile(path))
            {
                throw new ArgumentException("File is not an image");
            }

            float[] embedding = _encoder.GetImageEmbedding(path) ?? Enumerable.Empty<float>().ToArray();

            return new ImageDescriptor(path, embedding);
        }
        catch (ArgumentException ex)
        {
            _logger?.LogError(ex, "It seems the path does not point to an image");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create ImageDescriptor for path: {FilePath}", path);
            throw;
        }
    }

}