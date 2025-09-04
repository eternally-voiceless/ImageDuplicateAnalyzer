using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ImageDuplicateAnalyzer.Core.Configuration;
using ImageDuplicateAnalyzer.Core.Interfaces;
using ImageDuplicateAnalyzer.Core.Services;

namespace ImageDuplicateAnalyzer.Console.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageAnalyzerServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ModelDownloadOptions>(config.GetSection(ModelDownloadOptions.SectionName));
        services.Configure<ModelConfiguration>(config.GetSection(ModelConfiguration.SectionName));
        services.Configure<TestDirectoryConfiguration>(config.GetSection(TestDirectoryConfiguration.SectionName));

        services.AddHttpClient<IModelDownloadService, ModelDownloadService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ModelDownloadOptions>>().Value;
            client.Timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        });

        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IUserInterfaceService, UIService>();

        services.AddSingleton<IImageEncoder>(serviceProvider =>
        {
            var modelPath = serviceProvider.GetRequiredService<IOptions<ModelDownloadOptions>>().Value.ModelsDirectory;
            var visualModelName = serviceProvider.GetRequiredService<IOptions<ModelConfiguration>>().Value.VisualModelFileName;
            var path = Path.GetFullPath(Path.Combine(modelPath, visualModelName));

            var logger = serviceProvider.GetRequiredService<ILogger<ClipImageEncoder>>();

            return new ClipImageEncoder(path, logger);
        });

        services.AddTransient<IImageAnalysisService, ImageAnalysisService>();

        return services;
    }
}
