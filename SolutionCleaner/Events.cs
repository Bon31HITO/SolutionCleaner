namespace SolutionCleaner;

internal sealed class CopyProgressEventArgs : EventArgs
{
    public CopyProgress Progress { get; }

    public CopyProgressEventArgs(CopyProgress progress)
    {
        Progress = progress;
    }
}
internal sealed class CopyCompletedEventArgs : EventArgs
{
    public CopyResult Result { get; }

    public CopyCompletedEventArgs(CopyResult result)
    {
        Result = result;
    }
}
