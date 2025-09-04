using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using System;
using System.Linq;
using System.Numerics;

using ImageDuplicateAnalyzer.Core.Interfaces;

// ONNX Runtime Documentation
// https://onnxruntime.ai/docs/get-started/with-csharp.html

namespace ImageDuplicateAnalyzer.Core.Services;

public class ClipImageEncoder : IImageEncoder
{
    private readonly InferenceSession _session;
    private readonly ILogger<ClipImageEncoder>? _logger;
    private const int ImageSize = 224; // Standard size for CLIP ViT-B/32

    // https://github.com/openai/CLIP/issues/20
    // https://arxiv.org/abs/2103.00020
    private readonly float[] _imageMean = { 0.48145466f, 0.4578275f, 0.40821073f };
    private readonly float[] _imageStd = { 0.26862954f, 0.26130258f, 0.27577711f };
    private bool _disposed;

    public ClipImageEncoder(string modelPath, ILogger<ClipImageEncoder> logger)
    {
        _logger = logger;

        // CPU Optimization
        var sessionOptions = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_PARALLEL,
            InterOpNumThreads = Environment.ProcessorCount,
            IntraOpNumThreads = Environment.ProcessorCount
        };

        string modelName = Path.GetFileName(modelPath);
        try
        {
            _session = new InferenceSession(modelPath, sessionOptions);
            _logger?.LogInformation($"Model loaded: {modelName}");

            // Displaying information about the model
            LogModelInfo();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"❌ Model loading error: {ex.Message}");
            throw;
        }
    }

    private void LogModelInfo()
    {
        string modelInputsInfo = "Model inputs:\n";
        foreach (var input in _session.InputMetadata)
        {
            modelInputsInfo += $"\t  - {input.Key}: {string.Join("x", input.Value.Dimensions)}";
        }
        _logger?.LogInformation(modelInputsInfo);

        string modelOutputInfo = "Model outputs:\n";
        foreach (var output in _session.OutputMetadata)
        {
            modelOutputInfo += $"\t  - {output.Key}: {string.Join("x", output.Value.Dimensions)}";
        }
        _logger?.LogInformation(modelOutputInfo);
    }

    /// <summary>
    /// Extracts embedding from an image
    /// </summary>
    public float[] GetImageEmbedding(string imagePath)
    {
        try
        {
            // Load and preprocess the image
            using Image<Rgb24> image = Image.Load<Rgb24>(imagePath);
            var tensor = PreprocessImage(image);

            // Create an input tensor for ONNX
            var inputName = _session.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, tensor)
            };

            // Run inference
            using var results = _session.Run(inputs);

            // Extract embedding
            var outputTensor = results.First().AsTensor<float>();
            var embedding = outputTensor.ToArray();

            // Normalize the vector (important for cosine similarity!)
            NormalizeVector(embedding);

            return embedding;
        }
        catch (Exception ex)
        {
            // Notifier?.Invoke($"❌ Image processing error {imagePath}:\n{ex.Message}");
            _logger?.LogError($"❌ Image processing error {imagePath}:\n{ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Image preprocessing for CLIP
    /// </summary>
    private DenseTensor<float> PreprocessImage(Image<Rgb24> image)
    {
        // 1. Resize while maintaining proportions and center crop
        var resizeOptions = new ResizeOptions
        {
            Size = new Size(ImageSize, ImageSize),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        };
        image.Mutate(x => x.Resize(resizeOptions));

        // 2. Create a tensor [1, 3, 224, 224] - NCHW format [Number of size (batch), Channels, Height, Width]
        var tensor = new DenseTensor<float>(new[] { 1, 3, ImageSize, ImageSize });

        // 3. Converting pixels to tensor with normalization
        for (int i = 0; i < ImageSize; ++i)
        {
            for (int j = 0; j < ImageSize; ++j)
            {
                // Extracts one pixel from the image
                // Contains three properties for channels (Red: pixel.R, Green: pixel.G, Blue: pixel.B)
                Rgb24 pixel = image[i, j]; // i - row (y), j - column (x)

                // Normalization: (pixel / 255.0 - mean) / std
                tensor[0, 0, j, i] = (pixel.R / 255.0f - _imageMean[0]) / _imageStd[0];
                tensor[0, 1, j, i] = (pixel.G / 255.0f - _imageMean[1]) / _imageStd[1];
                tensor[0, 2, j, i] = (pixel.B / 255.0f - _imageMean[2]) / _imageStd[2];

            }
        }

        return tensor;
    }

    /// <summary>
    /// Normalization of a vector for correct calculation of cosine similarity
    /// </summary>
    private void NormalizeVector(float[] vector)
    {
        float magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; ++i)
            {
                vector[i] /= magnitude;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

}