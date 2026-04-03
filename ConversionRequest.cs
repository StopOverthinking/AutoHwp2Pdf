namespace AutoHwp2Pdf;

public sealed record ConversionRequest(
    string SourcePath,
    string OutputDirectory,
    string OutputBaseName,
    OutputFormat OutputFormat,
    int Attempt,
    int MaxAttempts);
