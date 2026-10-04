using System.IO;

namespace SolutionCleaner;

internal sealed class SolutionCopy
{
    public event EventHandler<CopyProgressEventArgs>? ProgressChanged;
    public event EventHandler<CopyCompletedEventArgs>? Completed;

    public async Task CopySolutionAsync(
        string sourceDirectoryPath,
        string destinationDirectoryPath,
        CancellationToken cancellationToken)
    {
        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;

        try
        {
            await Task.Run(() =>
            {
                DirectoryInfo sourceDirectory = new DirectoryInfo(sourceDirectoryPath);
                DirectoryInfo destinationDirectory = new DirectoryInfo(destinationDirectoryPath);

                if (!sourceDirectory.Exists)
                {
                    throw new DirectoryNotFoundException($"指定されたソリューションフォルダが存在しません: {sourceDirectoryPath}");
                }

                if (!destinationDirectory.Exists)
                {
                    destinationDirectory.Create();
                }

                // 出力先フォルダ配下にソリューション名と同名のルートフォルダパスを決定
                string destinationRootPath = Path.Combine(destinationDirectory.FullName, sourceDirectory.Name);
                DirectoryInfo destinationRootDirectory = new DirectoryInfo(destinationRootPath);

                // 既存フォルダが存在する場合は完全に削除して初期化
                if (destinationRootDirectory.Exists)
                {
                    destinationRootDirectory.Delete(recursive: true);

                    // OSのファイルハンドル解放およびディレクトリ削除完了を待機
                    int retryCount = 0;
                    while (Directory.Exists(destinationRootPath) && retryCount < 20)
                    {
                        Thread.Sleep(50);
                        retryCount++;
                    }
                }

                destinationRootDirectory.Create();

                List<FileInfo> targetFiles = new List<FileInfo>();
                CollectTargetFiles(sourceDirectory, targetFiles, cancellationToken);

                int totalFiles = targetFiles.Count;
                int copiedCount = 0;

                foreach (FileInfo sourceFile in targetFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativePath = Path.GetRelativePath(sourceDirectory.FullName, sourceFile.FullName);
                    string destinationFilePath = Path.Combine(destinationRootDirectory.FullName, relativePath);

                    string? destinationFileDirectoryName = Path.GetDirectoryName(destinationFilePath);
                    if (destinationFileDirectoryName is not null && !Directory.Exists(destinationFileDirectoryName))
                    {
                        Directory.CreateDirectory(destinationFileDirectoryName);
                    }

                    sourceFile.CopyTo(destinationFilePath, overwrite: true);
                    copiedCount++;

                    CopyProgress progress = new CopyProgress(copiedCount, totalFiles, sourceFile.Name);
                    DispatchProgress(synchronizationContext, progress);
                }

                CopyResult successResult = new CopyResult(true, copiedCount);
                DispatchCompleted(synchronizationContext, successResult);

            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            CopyResult cancelResult = new CopyResult(false, 0, "コピー処理がキャンセルされました。");
            DispatchCompleted(synchronizationContext, cancelResult);
        }
        catch (Exception exception)
        {
            CopyResult errorResult = new CopyResult(false, 0, exception.Message);
            DispatchCompleted(synchronizationContext, errorResult);
        }
    }

    private void CollectTargetFiles(
        DirectoryInfo directory,
        List<FileInfo> fileCollector,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (FileInfo file in directory.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsExcludedFile(file))
            {
                fileCollector.Add(file);
            }
        }

        foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsExcludedDirectory(subDirectory))
            {
                CollectTargetFiles(subDirectory, fileCollector, cancellationToken);
            }
        }
    }

    private bool IsExcludedDirectory(DirectoryInfo directory)
    {
        string directoryName = directory.Name;

        if (directoryName.StartsWith('.'))
        {
            return true;
        }

        if (string.Equals(directoryName, "bin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(directoryName, "obj", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private bool IsExcludedFile(FileInfo file)
    {
        if (string.Equals(file.Extension, ".slnx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(file.Extension, ".sln", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return file.Name.StartsWith('.');
    }

    private void DispatchProgress(SynchronizationContext? synchronizationContext, CopyProgress progress)
    {
        if (synchronizationContext is not null)
        {
            synchronizationContext.Post(
                _ => ProgressChanged?.Invoke(this, new CopyProgressEventArgs(progress)),
                null);
        }
        else
        {
            ProgressChanged?.Invoke(this, new CopyProgressEventArgs(progress));
        }
    }

    private void DispatchCompleted(SynchronizationContext? synchronizationContext, CopyResult result)
    {
        if (synchronizationContext is not null)
        {
            synchronizationContext.Post(
                _ => Completed?.Invoke(this, new CopyCompletedEventArgs(result)),
                null);
        }
        else
        {
            Completed?.Invoke(this, new CopyCompletedEventArgs(result));
        }
    }
}