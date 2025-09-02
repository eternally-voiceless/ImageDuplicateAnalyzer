namespace ImageDuplicateAnalyzer.Core.Interfaces;

public interface IImageAnalysisService
{
    IImageDescriptor CreateImageDescriptor(string path);
}