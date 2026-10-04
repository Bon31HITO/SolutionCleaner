using System.IO;

namespace SolutionCleaner;

internal readonly record struct Settings(
    string SourceDirectoryPath,
    string DestinationDirectoryPath);

internal readonly record struct CopyProgress(
    int CopiedFiles,
    int TotalFiles,
    string CurrentFileName);

internal readonly record struct CopyResult(
    bool IsSuccess,
    int CopiedFiles,
    string? ErrorMessage = null);
