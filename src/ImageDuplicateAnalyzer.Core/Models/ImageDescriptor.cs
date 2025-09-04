using Microsoft.Extensions.Logging;

using ImageDuplicateAnalyzer.Core.Services;
using ImageDuplicateAnalyzer.Core.Interfaces;

namespace ImageDuplicateAnalyzer.Core.Models;

public class ImageDescriptor : IImageDescriptor
{
    public string FilePath { get; }
    public float[] Embedding { get; }

    public ImageDescriptor(string pathToFile, float[] embedding)
    {
        FilePath = pathToFile;
        Embedding = embedding;
    }

    public float CompareEmbedding(IImageDescriptor imageDescriptor)
    {
        float[] outEmbedding = imageDescriptor.Embedding;
        if (Embedding.Length != outEmbedding.Length)
        {
            throw new ArgumentException("Embeddings must have the same dimensions");
        }

        return Embedding.Zip(outEmbedding, (x, y) => x * y).Sum();
    }
}

