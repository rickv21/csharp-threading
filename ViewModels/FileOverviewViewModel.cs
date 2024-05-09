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


    private ObservableCollection<FileListViewModel> _leftSideViewModels;
    public ObservableCollection<FileListViewModel> LeftSideViewModels
    {
        get { return _leftSideViewModels; }
    }
    private ObservableCollection<FileListViewModel> _rightSideViewModels;
    public ObservableCollection<FileListViewModel> RightSideViewModels
    {
        get { return _rightSideViewModels; }
    }
    public ICommand ItemDoubleTappedCommand { get; }


    private readonly ConcurrentDictionary<string, byte[]> _fileIconCache = new ConcurrentDictionary<string, byte[]>();

    public FileOverviewViewModel()
    {
        ItemDoubleTappedCommand = new Command<Item>(OnItemDoubleTapped);
        _leftSideViewModels = new ObservableCollection<FileListViewModel>();
        _rightSideViewModels = new ObservableCollection<FileListViewModel>();
        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
        _leftSideViewModels.Add(LeftSideViewModel);
        _rightSideViewModels.Add(RightSideViewModel);

        OnPropertyChanged(nameof(LeftSideViewModels));
    }

    // List needs to be manipulated in order for the Picker values to be updated
    void OnItemDoubleTapped(Item item)
    {
        if (item.Side == 0)
        {
            if (item.FileName != "...")
            {
                LeftSideViewModel.CurrentPath = item.FilePath;
            }
            else
            {
                LeftSideViewModel.CurrentPath = Directory.GetParent(LeftSideViewModel.CurrentPath).FullName;
            }
            int index = LeftSideViewModels.IndexOf(LeftSideViewModel);
            FileListViewModel copy = LeftSideViewModel as FileListViewModel;
            LeftSideViewModels.RemoveAt(index);
            LeftSideViewModel = copy;
            LeftSideViewModels.Insert(index, copy);
            LeftSideViewModel = copy;
            LeftSideViewModel.ItemDoubleTappedCommand.Execute(item);
        }
        else
        {
            if (item.FileName != "...")
            {
                RightSideViewModel.CurrentPath = item.FilePath;
            }
            else
            {
                RightSideViewModel.CurrentPath = Directory.GetParent(RightSideViewModel.CurrentPath).FullName;
            }
            int index = RightSideViewModels.IndexOf(RightSideViewModel);
            FileListViewModel copy = RightSideViewModel as FileListViewModel;
            RightSideViewModels.RemoveAt(index);
            RightSideViewModel = copy;
            RightSideViewModels.Insert(index, copy);
            RightSideViewModel = copy;
            RightSideViewModel.ItemDoubleTappedCommand.Execute(item);
        }
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
            _leftSideViewModels.Add(new FileListViewModel(_fileIconCache, 0));
            LeftSideViewModel = LeftSideViewModels[LeftSideViewModels.Count - 1];
            OnPropertyChanged(nameof(LeftSideViewModels));
        }
        else
        {
            _rightSideViewModels.Add(new FileListViewModel(_fileIconCache, 1));
            OnPropertyChanged(nameof(RightSideViewModels));

        }
    }

    public void RemoveTab(int side)
    {
        if(side == 0)
        {
            LeftSideViewModels.Remove(LeftSideViewModel);
            LeftSideViewModel = LeftSideViewModels[0];
        }
        else
        {
            RightSideViewModels.Remove(RightSideViewModel);
            RightSideViewModel = RightSideViewModels[0];
        }
    }
}