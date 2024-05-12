using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows;
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

    private CollectionView _leftCollection;
    
    private CollectionView _rightCollection;

    public FileOverviewViewModel(CollectionView leftCollection, CollectionView rightCollection)
    {
        _leftCollection = leftCollection;
        _rightCollection = rightCollection;

        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
    }

    // Constructor without collections
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

    public string GetCurrentPath(CollectionView collectionView)
    {
        if (collectionView == _leftCollection)
        {
            return LeftSideViewModel.CurrentPath;
        }
        else if (collectionView == _rightCollection)
        {
            return RightSideViewModel.CurrentPath;
        }
        else
        {
            return null;
        }
    }

    private List<string> _copiedFilesPaths = new List<string>();
    public List<string> CopiedFilesPaths
    {
        get { return _copiedFilesPaths; }
        set { _copiedFilesPaths = value; }
    }
    private string tempCopyDirectory = Path.Combine(Path.GetTempPath(), "FileManagerCopiedItems");

    public void CopyItems(List<object> selectedItems)
    {
        // make sure list is empty first
        _copiedFilesPaths.Clear();

        // if temporary directory doesnt exist, make one
        if (!Directory.Exists(tempCopyDirectory))
        {
            Directory.CreateDirectory(tempCopyDirectory);
        }

        foreach (var item in selectedItems)
        {
            if (item is FileItem fileItem) // if file
            {
                // Copy file to temporary directory
                string fileName = Path.GetFileName(fileItem.FilePath);
                string tempFilePath = Path.Combine(tempCopyDirectory, fileName);
                File.Copy(fileItem.FilePath, tempFilePath, true);

                // Add path of file to list
                _copiedFilesPaths.Add(tempFilePath);
            }
            else if (item is DirectoryItem directoryItem) // if directory
            {
                // Copy folder to temporary directory
                string dirName = Path.GetFileName(directoryItem.FilePath);
                string tempDirPath = Path.Combine(tempCopyDirectory, dirName);
                DirectoryCopy(directoryItem.FilePath, tempDirPath, true);

                // Add path of copied folder to list
                _copiedFilesPaths.Add(tempDirPath);
            }
        }
    }

    private void DirectoryCopy(string sourceDirPath, string destDirPath, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDirPath);

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirPath, file.Name);
            file.CopyTo(tempPath, true);
        }

        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirPath, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

    public void PasteItems(string targetPath)
    {

        foreach (var sourcePath in _copiedFilesPaths)
        {
            string fileName = Path.GetFileName(sourcePath);
            string destFilePath = Path.Combine(targetPath, fileName);


            if (File.Exists(sourcePath))
            {
                if (File.Exists(destFilePath))
                {
                    MessageBox.Show("File already exists in target path: " + destFilePath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Copy file
                File.Copy(sourcePath, destFilePath, true);
            }
            else if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destFilePath))
                {
                    MessageBox.Show("Directory already exists in target path: " + destFilePath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Copy directory
                DirectoryCopy(sourcePath, destFilePath, true);
            }
        }

        // Refresh
        RightSideViewModel.RefreshFiles();
        LeftSideViewModel.RefreshFiles();

        // empty copy list
        _copiedFilesPaths.Clear();
    }


    private void DirectoryMove(string sourceDirPath, string destDirPath)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDirPath);

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirPath, file.Name);
            file.MoveTo(tempPath);
        }

        foreach (DirectoryInfo subdir in dirs)
        {
            string tempPath = Path.Combine(destDirPath, subdir.Name);
            DirectoryMove(subdir.FullName, tempPath);
        }

        Directory.Delete(sourceDirPath, false);
    }
}