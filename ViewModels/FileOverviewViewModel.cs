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
    private IEnumerable<Item> _selectedItems;
    public IEnumerable<Item> SelectedItems
    {
        get => _selectedItems;
        set
        {
            _selectedItems = value;
            OnPropertyChanged(nameof(SelectedItems));
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

    private object _dropPointObj;
    public object DropPointObj
    {
        get => _dropPointObj;
        set
        {
            _dropPointObj = value;
            OnPropertyChanged(nameof(DropPointObj));
        }
    }

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


    private CollectionView leftCollection;
    private CollectionView rightCollection;

    // Constructor with collections
    public FileOverviewViewModel(CollectionView leftCollection, CollectionView rightCollection)
    {
        leftCollection = leftCollection;
        rightCollection = rightCollection;

        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
    }

    // Constructor without collections
    public FileOverviewViewModel()
    {
        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
    }

    public void ToggleItemSelection(Item item, CollectionView collectionView)
    {
        if (collectionView.SelectedItems.Contains(item))
        {
            collectionView.SelectedItems.Remove(item);
        }
        else
        {
            collectionView.SelectedItems.Add(item);
        }
    }

    public string GetCurrentPath(CollectionView collectionView)
    {
        if (collectionView == leftCollection)
        {
            return LeftSideViewModel.CurrentPath;
        }
        else if (collectionView == rightCollection)
        {
            return RightSideViewModel.CurrentPath;
        }
        else
        {
            return null;
        }
    }
}