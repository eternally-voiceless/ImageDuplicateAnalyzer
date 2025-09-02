namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IImageDescriptor
{
    string FilePath { get; }
    float[] Embedding { get; }

    float CompareEmbedding(IImageDescriptor imageDescriptor);
}