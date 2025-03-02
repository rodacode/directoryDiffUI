using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using DirectoryDiff;

namespace DirectoryDiffUI.ViewModels
{
    public partial class ComparisonViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _directory1 = string.Empty;

        [ObservableProperty]
        private string _directory2 = string.Empty;

        [ObservableProperty]
        private bool _compareContent = false;

        [ObservableProperty]
        private bool _isComparing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        private ComparisonResult? _result;

        // Collections for UI binding
        public ObservableCollection<string> OnlyInFirst { get; } = new();
        public ObservableCollection<string> OnlyInSecond { get; } = new();
        public ObservableCollection<FileDifferenceViewModel> Different { get; } = new();
        public ObservableCollection<string> Identical { get; } = new();

        public string ResultSummary
        {
            get
            {
                if (_result == null)
                    return string.Empty;

                return $"Only in first: {_result.OnlyInFirst.Count}, " +
                       $"Only in second: {_result.OnlyInSecond.Count}, " +
                       $"Different: {_result.Different.Count}, " +
                       $"Identical: {_result.Identical.Count}";
            }
        }

        [RelayCommand]
        private void VerifyDirectory1()
        {
            if (Directory.Exists(Directory1))
            {
                StatusMessage = $"Directory 1 set to: {Directory1}";
            }
            else
            {
                StatusMessage = $"Directory not found: {Directory1}";
            }
        }

        [RelayCommand]
        private void VerifyDirectory2()
        {
            if (Directory.Exists(Directory2))
            {
                StatusMessage = $"Directory 2 set to: {Directory2}";
            }
            else
            {
                StatusMessage = $"Directory not found: {Directory2}";
            }
        }

        [RelayCommand]
        private async Task CompareDirectoriesAsync()
        {
            if (string.IsNullOrEmpty(Directory1) || string.IsNullOrEmpty(Directory2))
            {
                StatusMessage = "Please enter both directory paths";
                return;
            }

            if (!Directory.Exists(Directory1))
            {
                StatusMessage = $"Directory not found: {Directory1}";
                return;
            }

            if (!Directory.Exists(Directory2))
            {
                StatusMessage = $"Directory not found: {Directory2}";
                return;
            }

            try
            {
                IsComparing = true;
                StatusMessage = "Comparing directories...";

                // Clear previous results
                OnlyInFirst.Clear();
                OnlyInSecond.Clear();
                Different.Clear();
                Identical.Clear();

                // Run comparison on a background thread
                _result = await Task.Run(() =>
                {
                    var comparer = new DirectoryComparer(CompareContent);
                    return comparer.CompareDirectories(Directory1, Directory2);
                });

                // Update UI collections on the UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var file in _result.OnlyInFirst.OrderBy(f => f))
                    {
                        OnlyInFirst.Add(file);
                    }

                    foreach (var file in _result.OnlyInSecond.OrderBy(f => f))
                    {
                        OnlyInSecond.Add(file);
                    }

                    foreach (var diff in _result.Different.OrderBy(d => d.FileName))
                    {
                        Different.Add(new FileDifferenceViewModel(diff));
                    }

                    foreach (var file in _result.Identical.OrderBy(f => f))
                    {
                        Identical.Add(file);
                    }

                    StatusMessage = "Comparison complete";
                    OnPropertyChanged(nameof(ResultSummary));
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsComparing = false;
            }
        }
    }

    public class FileDifferenceViewModel : ViewModelBase
    {
        public string FileName { get; }
        public string SizeDifference { get; }
        public string DateDifference { get; }
        public string ContentDifference { get; }

        public FileDifferenceViewModel(FileDifference diff)
        {
            FileName = diff.FileName;

            SizeDifference = diff.Size1 == diff.Size2
                ? $"Same size ({diff.Size1} bytes)"
                : $"Size: {diff.Size1} vs {diff.Size2} bytes";

            DateDifference = diff.Modified1 == diff.Modified2
                ? $"Same date ({diff.Modified1.ToLocalTime():g})"
                : $"Date: {diff.Modified1.ToLocalTime():g} vs {diff.Modified2.ToLocalTime():g}";

            ContentDifference = diff.ContentSame
                ? "Content is identical"
                : "Content differs";
        }
    }
}