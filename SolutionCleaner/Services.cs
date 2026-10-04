using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace SolutionCleaner;

internal interface IFolderPickerService
{
    string? PickFolder(string? initialDirectory = null);
}

internal interface IMessageService
{
    void ShowInformation(string message, string title);
    void ShowError(string message, string title);
    bool ShowConfirmation(string message, string title);
}

internal sealed class FolderPickerService : IFolderPickerService
{
    public string? PickFolder(string? initialDirectory = null)
    {
        OpenFolderDialog dialog = new OpenFolderDialog
        {
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        bool? dialogResult = dialog.ShowDialog();
        return dialogResult == true ? dialog.FolderName : null;
    }
}

internal sealed class MessageService : IMessageService
{
    public void ShowInformation(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirmation(string message, string title)
    {
        MessageBoxResult messageBoxResult = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return messageBoxResult == MessageBoxResult.Yes;
    }
}