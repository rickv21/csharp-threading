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

    private readonly ConcurrentDictionary<string, byte[]> _fileIconCache = new ConcurrentDictionary<string, byte[]>();

    private int _activeSide;

    public int ActiveSide
    {
        get { return _activeSide; }
        set
        {
            _activeSide = value;
        }
    }

    private IEnumerable<Item> _droppedFiles;
    public IEnumerable<Item> DroppedFiles
    {
        get => _droppedFiles;
        set
        {
            _droppedFiles = value;
            OnPropertyChanged(nameof(DroppedFiles));
        }
    }

    // Constructor 
    public FileOverviewViewModel()
    {
        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
        ActiveSide = 0;
    }

    public void PassClickEvent(string key)
    {
        Debug.WriteLine("Pass click event " + ActiveSide);
        if(ActiveSide == 0)
        {
            LeftSideViewModel.HandleClick(key);
        } 
        else
        {
            RightSideViewModel.HandleClick(key);
        }
    }

    public void UpdateSelected(IList<object> leftSelectedItems, IList<object> rightSelectedItems)
    {
        LeftSideViewModel.SelectedItems = leftSelectedItems;
        RightSideViewModel.SelectedItems= rightSelectedItems;
    }
}