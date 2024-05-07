using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileManager.Models;
using Windows.Storage.Pickers;

namespace FileManager.ViewModels;

public class FileOverviewViewModel : ViewModelBase
{

    private FileListViewModel _leftSideViewModel;
    public FileListViewModel LeftSideViewModel
    {
        get { return _leftSideViewModel; }
        set
        {
            _leftSideViewModel = value;
            OnPropertyChanged(nameof(LeftSideViewModel));
        }
    }

    private FileListViewModel _rightSideViewModel;
    public FileListViewModel RightSideViewModel
    {
        get { return _rightSideViewModel; }
        set
        {
            _rightSideViewModel = value;
            OnPropertyChanged(nameof(RightSideViewModel));
        }
    }

    private Dictionary<int, FileListViewModel> _LeftSideViewModels;
    public Dictionary<int, FileListViewModel> LeftSideViewModels
    {
        get { return _LeftSideViewModels; }
    }
    private Dictionary<int, FileListViewModel> _RightSideViewModels;
    public Dictionary<int, FileListViewModel> RightSideViewModels
    {
        get { return RightSideViewModels; }
    }

    private readonly ConcurrentDictionary<string, byte[]> _fileIconCache = new ConcurrentDictionary<string, byte[]>();

    public FileOverviewViewModel()
    {
        _LeftSideViewModels = new Dictionary<int, FileListViewModel>();
        _RightSideViewModels = new Dictionary<int, FileListViewModel>();
        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
        _LeftSideViewModels.Add(0, LeftSideViewModel);
        _RightSideViewModels.Add(0, RightSideViewModel);

        OnPropertyChanged(nameof(_LeftSideViewModels));
    }

    public async Task<string> SelectActionAsync()
    {
        return await Application.Current.MainPage.DisplayActionSheet("Select Action", "Cancel", null, "Copy", "Move");
    }

    public async Task<(string, string?)> PromptUserAsync(string action, bool isDir = false)
    {
        string number = await Application.Current.MainPage.DisplayPromptAsync("Enter Number", $"Number of threads for {action}:", "OK", "Cancel", "0", maxLength: 10, keyboard: Keyboard.Numeric);

        if (isDir)
        {
            string directoryName = await Application.Current.MainPage.DisplayPromptAsync("Enter regex", $"Please enter a regex to select files to {action}:", "OK", "Cancel", null, maxLength: 100);

            return (number, directoryName);
        }

        return (number, null);
    }

    public async Task ProcessActionAsync(string action, int number, string regex)
    {
        switch (action)
        {
            case "Move":
                // Perform Move action based on 'number'
                break;
            case "Copy":
                // Perform Copy action based on 'number'
                break;
        }
    }

    public void AddTab(int side)
    {
        if(side == 0)
        {
            _LeftSideViewModels.Add(_LeftSideViewModels.Count, new FileListViewModel(_fileIconCache, 0));
            OnPropertyChanged(nameof(_LeftSideViewModels));
        }
        else
        {
            _RightSideViewModels.Add(_RightSideViewModels.Count, new FileListViewModel(_fileIconCache, 1));
            OnPropertyChanged(nameof(_RightSideViewModels));

        }
    }
}