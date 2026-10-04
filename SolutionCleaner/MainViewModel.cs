using System.IO;
using System.Windows.Input;
namespace SolutionCleaner;

internal sealed class MainViewModel : ViewModelBase
{
    private readonly IFolderPickerService folderPickerService;
    private readonly IMessageService messageService;
    private readonly SolutionCopy solutionCopy;
    private readonly SettingsManager settingsManager;

    private string sourceDirectoryPath = string.Empty;
    private string destinationDirectoryPath = string.Empty;
    private bool isProcessing;
    private string statusMessage = "フォルダを選択してください。";
    private string currentFileName = string.Empty;
    private double progressPercentage;
    private string progressText = string.Empty;

    private CancellationTokenSource? cancellationTokenSource;

    public string SourceDirectoryPath
    {
        get => sourceDirectoryPath;
        set
        {
            if (SetProperty(ref sourceDirectoryPath, value))
            {
                OnPropertyChanged(nameof(CanStartCopy));
                SaveCurrentSettings();
            }
        }
    }

    public string DestinationDirectoryPath
    {
        get => destinationDirectoryPath;
        set
        {
            if (SetProperty(ref destinationDirectoryPath, value))
            {
                OnPropertyChanged(nameof(CanStartCopy));
                SaveCurrentSettings();
            }
        }
    }

    public bool IsProcessing
    {
        get => isProcessing;
        private set
        {
            if (SetProperty(ref isProcessing, value))
            {
                OnPropertyChanged(nameof(CanStartCopy));
                OnPropertyChanged(nameof(CanSelectDirectory));
            }
        }
    }

    public bool CanSelectDirectory => !isProcessing;

    public bool CanStartCopy =>
        !isProcessing &&
        !string.IsNullOrWhiteSpace(sourceDirectoryPath) &&
        !string.IsNullOrWhiteSpace(destinationDirectoryPath);

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string CurrentFileName
    {
        get => currentFileName;
        private set => SetProperty(ref currentFileName, value);
    }

    public double ProgressPercentage
    {
        get => progressPercentage;
        private set => SetProperty(ref progressPercentage, value);
    }

    public string ProgressText
    {
        get => progressText;
        private set => SetProperty(ref progressText, value);
    }

    public ICommand SelectSourceDirectoryCommand { get; }
    public ICommand SelectDestinationDirectoryCommand { get; }
    public ICommand StartCopyCommand { get; }
    public ICommand CancelCommand { get; }

    public MainViewModel()
    {
        this.folderPickerService = new FolderPickerService();
        this.messageService = new MessageService();
        this.solutionCopy = new SolutionCopy();
        this.settingsManager = new SettingsManager();

        // 前回の設定を復元
        Settings settings = this.settingsManager.LoadSettings();
        sourceDirectoryPath = settings.SourceDirectoryPath;
        destinationDirectoryPath = settings.DestinationDirectoryPath;

        this.solutionCopy.ProgressChanged += OnCopyProgressChanged;
        this.solutionCopy.Completed += OnCopyCompleted;

        SelectSourceDirectoryCommand = new RelayCommand(SelectSourceDirectory, () => CanSelectDirectory);
        SelectDestinationDirectoryCommand = new RelayCommand(SelectDestinationDirectory, () => CanSelectDirectory);
        StartCopyCommand = new RelayCommand(StartCopy, () => CanStartCopy);
        CancelCommand = new RelayCommand(Cancel, () => IsProcessing);
    }

    private void SelectSourceDirectory()
    {
        string? selectedDirectory = folderPickerService.PickFolder(SourceDirectoryPath);
        if (!string.IsNullOrWhiteSpace(selectedDirectory))
        {
            SourceDirectoryPath = selectedDirectory;
        }
    }

    private void SelectDestinationDirectory()
    {
        string? selectedDirectory = folderPickerService.PickFolder(DestinationDirectoryPath);
        if (!string.IsNullOrWhiteSpace(selectedDirectory))
        {
            DestinationDirectoryPath = selectedDirectory;
        }
    }

    private async void StartCopy()
    {
        string fullSourcePath = Path.GetFullPath(SourceDirectoryPath);
        string fullDestinationPath = Path.GetFullPath(DestinationDirectoryPath);

        if (string.Equals(fullSourcePath, fullDestinationPath, StringComparison.OrdinalIgnoreCase))
        {
            messageService.ShowError("コピー元とコピー先には異なるフォルダを指定してください。", "指定エラー");
            return;
        }

        DirectoryInfo sourceDirectoryInformation = new DirectoryInfo(fullSourcePath);
        string resolvedTargetRootPath = Path.Combine(fullDestinationPath, sourceDirectoryInformation.Name);

        if (string.Equals(fullSourcePath, resolvedTargetRootPath, StringComparison.OrdinalIgnoreCase))
        {
            messageService.ShowError("コピー先に出力されるフォルダがコピー元フォルダと同一になります。別のフォルダを指定してください。", "指定エラー");
            return;
        }

        // 出力先に出力と同名のフォルダが存在する場合の上書き確認
        if (Directory.Exists(resolvedTargetRootPath))
        {
            string confirmationMessage =
                $"出力先に「{sourceDirectoryInformation.Name}」フォルダが既に存在します。\n" +
                "既存のフォルダを完全に削除してから上書きコピーを開始しますか？";

            bool isConfirmed = messageService.ShowConfirmation(confirmationMessage, "上書き確認");
            if (!isConfirmed)
            {
                return;
            }
        }

        SaveCurrentSettings();

        IsProcessing = true;
        StatusMessage = "対象ファイルを走査中...";
        CurrentFileName = string.Empty;
        ProgressPercentage = 0;
        ProgressText = string.Empty;

        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await solutionCopy.CopySolutionAsync(
                SourceDirectoryPath,
                DestinationDirectoryPath,
                cancellationTokenSource.Token);
        }
        catch (Exception exception)
        {
            StatusMessage = "エラーが発生しました。";
            messageService.ShowError(exception.Message, "処理失敗");
            IsProcessing = false;
        }
    }

    private void Cancel()
    {
        if (cancellationTokenSource is not null && !cancellationTokenSource.IsCancellationRequested)
        {
            StatusMessage = "キャンセル待機中...";
            cancellationTokenSource.Cancel();
        }
    }

    private void SaveCurrentSettings()
    {
        Settings settings = new Settings(sourceDirectoryPath, destinationDirectoryPath);

        settingsManager.SaveSettings(settings);
    }

    private void OnCopyProgressChanged(object? sender, CopyProgressEventArgs eventArguments)
    {
        CopyProgress progress = eventArguments.Progress;
        CurrentFileName = progress.CurrentFileName;
        ProgressPercentage = progress.TotalFiles > 0
            ? (double)progress.CopiedFiles / progress.TotalFiles * 100
            : 0;
        ProgressText = $"{progress.CopiedFiles} / {progress.TotalFiles} 件";
        StatusMessage = "ファイルをコピー中...";
    }

    private void OnCopyCompleted(object? sender, CopyCompletedEventArgs eventArguments)
    {
        IsProcessing = false;
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;

        CopyResult result = eventArguments.Result;
        if (result.IsSuccess)
        {
            StatusMessage = "コピーが正常に完了しました。";
            CurrentFileName = string.Empty;
            messageService.ShowInformation(
                $"コピーが完了しました。\nコピーしたファイル数: {result.CopiedFiles} 件",
                "処理完了");
        }
        else
        {
            StatusMessage = result.ErrorMessage ?? "処理が中断されました。";
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                messageService.ShowError(result.ErrorMessage, "処理結果");
            }
        }
    }
}