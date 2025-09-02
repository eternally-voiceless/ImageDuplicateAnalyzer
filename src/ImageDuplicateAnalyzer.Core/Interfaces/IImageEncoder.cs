namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IImageEncoder : IDisposable
{
    float[] GetImageEmbedding(string imagePath);

    /// <summary>
    /// Calculates the cosine similarity between two embeddings
    /// </summary>
    public static float CosineSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
        {
            throw new ArgumentException("Embeddings must have the same dimensions");
        }

        float dotProduct = 0;
        for (int i = 0; i < embedding1.Length; ++i)
        {
            dotProduct += embedding1[i] * embedding2[i];
        }

        return dotProduct; // Vectors are already normalized
    }
}