namespace ImageDuplicateAnalyzer.Core.Configuration;

public class ModelConfiguration
{
    public const string SectionName = "ModelConfiguration";

    public string VisualModelUrl { get; set; } = string.Empty;
    public string TextModelUrl { get; set; } = string.Empty;
    public string VisualModelFileName { get; set; } = string.Empty;
    public string TextModelFileName { get; set; } = string.Empty;
}

public class ModelDownloadOptions
{
    public const string SectionName = "ModelDownloadOptions";

    public int TimeoutMinutes { get; set; } = 1;
    public string UserAgent { get; set; } = string.Empty;
    public string ModelsDirectory { get; set; } = string.Empty;
    public string RootDirName { get; set; } = string.Empty;

}

public class TestDirectoryConfiguration
{
    public const string SectionName = "TestData";

    public string DefaultName { get; set; } = string.Empty;
    public string DefaultTestSet { get; set; } = string.Empty;
    public string TestSet1 { get; set; } = string.Empty;
    public string TestSet2 { get; set; } = string.Empty;
    public string TestSet3 { get; set; } = string.Empty;
}