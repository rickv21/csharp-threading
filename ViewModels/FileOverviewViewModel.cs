using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using FileManager.Models;
using FileManager.Views.Popups;
using Application = Microsoft.Maui.Controls.Application;
using System.Collections.ObjectModel;

namespace FileManager.ViewModels;

public class FileOverviewViewModel : ViewModelBase
{
    public static int MAX_THREADS = 255;

    private Boolean _popupOpen = false;

    public Boolean PopupOpen
    {
        get { return _popupOpen; }
        set
        {
            _popupOpen = value;
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

    private readonly ConcurrentDictionary<string, byte[]> _fileIconCache = new();

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
    private CollectionView _leftCollection;

    private CollectionView _rightCollection;

    public FileOverviewViewModel(CollectionView leftCollection, CollectionView rightCollection)
    {
        _leftCollection = leftCollection;
        _rightCollection = rightCollection;

        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
        
        ItemDoubleTappedCommand = new Command<Item>(OnItemDoubleTapped);
        
        _leftSideViewModels = new ObservableCollection<FileListViewModel>();
        _rightSideViewModels = new ObservableCollection<FileListViewModel>();
        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);

        _leftSideViewModels.Add(LeftSideViewModel);
        _rightSideViewModels.Add(RightSideViewModel);

        OnPropertyChanged(nameof(LeftSideViewModels));
        OnPropertyChanged(nameof(RightSideViewModel));
    }

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
        OnPropertyChanged(nameof(RightSideViewModel));
    }

    // List needs to be manipulated in order for the Picker values to be updated
    async void OnItemDoubleTapped(Item item)
    {
        if (item.Side == 0)
        {
            LeftSideViewModel.ItemDoubleTappedCommand.Execute(item);
            LeftSideViewModel = UpdateTab(LeftSideViewModels, LeftSideViewModel);
        }
        else
        {
            RightSideViewModel.ItemDoubleTappedCommand.Execute(item);
            RightSideViewModel = UpdateTab(RightSideViewModels, RightSideViewModel);
        }
    }

    private FileListViewModel UpdateTab(ObservableCollection<FileListViewModel> viewModels, FileListViewModel viewModel)
    {
        int index = viewModels.IndexOf(viewModel);
        FileListViewModel copy = viewModel as FileListViewModel;
        viewModels.RemoveAt(index);
        viewModels.Insert(index, copy);

        return copy;

        // Wait for the UI to update before proceeding
        //await Task.Delay(100);

    }

    public async Task<string> SelectActionAsync()
    {
        return await Application.Current.MainPage.DisplayActionSheet("Select Action", "Cancel", null, "Copy", "Move");
    }

    public async Task<(string, string?)> PromptUserAsync(string action, bool isDir = false)
    {
        string number = await Application.Current.MainPage.DisplayPromptAsync("Enter Number", $"Number of threads for {action}:", "OK", "Cancel", "0", maxLength: 10, keyboard: Microsoft.Maui.Keyboard.Numeric);
        if(int.Parse(number) > MAX_THREADS || int.Parse(number) < 1)
        {
            return (number, null);
        }

        if (isDir)
        {
            string directoryName = await Application.Current.MainPage.DisplayPromptAsync("Enter regex", $"Please enter a regex to select files to {action}:", "OK", "Cancel", null, maxLength: 100);

            return (number, directoryName);
        }

        return (number, null);
    }

    public async Task ProcessActionAsync(string action, int number, string regex, ContentPage view, IList<Item> items)
    {
        var fileCount = 0;
        foreach (var item in items)
        {
            fileCount += CountFiles(item);
        }
        var popup = new PopupPage();
        view.ShowPopup(popup);
        for (var i = 0; i <= fileCount; i++)
        {
            var label = popup.Content.FindByName("Label") as Label;
            label.Text = $"Files done:{i}/{fileCount}";
            label.TextColor = Colors.Black;
            var progressBar = popup.FindByName("ProgressBar") as ProgressBar;
            progressBar.Progress = (double)(((double)i / (double)fileCount) * 1);
            switch (action)
            {
                case "Move":
                    await Task.Delay(1000);
                    break;
                case "Copy":
                    await Task.Delay(2000);
                    // Perform Copy action based on 'number'
                    break;
            }
        }
        popup.Close();
    }


    public async Task AddTabAsync(int side)
    {
        if (side == 0)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _leftSideViewModels.Add(new FileListViewModel(_fileIconCache, 0));
                LeftSideViewModel = LeftSideViewModels[LeftSideViewModels.Count - 1];
                OnPropertyChanged(nameof(LeftSideViewModels));
            });
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _rightSideViewModels.Add(new FileListViewModel(_fileIconCache, 1));
                RightSideViewModel = RightSideViewModels[RightSideViewModels.Count - 1];
                OnPropertyChanged(nameof(RightSideViewModels));
            });
        }
    }

    public async Task RemoveTabAsync(int side)
    {
        if (side == 0)
        {
            if (_leftSideViewModels.Count <= 1)
            {
                return;
            }
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LeftSideViewModels.Remove(LeftSideViewModel);
                LeftSideViewModel = LeftSideViewModels[0];
            });
        }
        else
        {
            if (_rightSideViewModels.Count <= 1)
            {
                return;
            }
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RightSideViewModels.Remove(RightSideViewModel);
                RightSideViewModel = RightSideViewModels[0];
            });
        }
    }

    public void PassClickEvent(string key)
    {
        Debug.WriteLine("Pass click event " + ActiveSide);
        if(PopupOpen)
        {
            return;
        }
        if (ActiveSide == 0)
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
        RightSideViewModel.SelectedItems = rightSelectedItems;
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

    /// <summary>
    /// Threading manier: locks
    /// 
    /// Voor wanneer meerdere bestanden worden gekopïeerd. Door de lock 
    /// heeft alleen 1 bestand of map toegang tot de _copiedFilesPaths list. 
    /// </summary>
    public void CopyItems(List<object> selectedItems)
    {
        lock (_copiedFilesPaths)
        {
            // Clear the list of copied files paths
            _copiedFilesPaths.Clear();

            // Ensure the temporary directory exists
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
    }

    /// <summary>
    /// Threadming manier: Threadpool
    /// 
    /// Deze methode kopieert een directory recursief naar een nieuwe locatie, waarbij bestanden en submappen parallel worden gekopieerd met behulp van de ThreadPool van .NET.
    /// Door Parallel.ForEach te gebruiken, wordt de onderliggende ThreadPool van .NET gebruikt om de iteraties over bestanden en directories te verdelen over meerdere threads.
    /// Dit maakt effectief gebruik van beschikbare CPU-cycli en kan leiden tot betere prestaties, vooral bij het kopiëren van grote hoeveelheden data of het gebruik van langzame opslagmedia.
    /// </summary>
    /// <param name="sourceDirPath"></param>
    /// <param name="destDirPath"></param>
    /// <param name="copySubDirs"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private void DirectoryCopy(string sourceDirPath, string destDirPath, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDirPath);

        // Use Parallel.ForEach to copy files in parallel
        Parallel.ForEach(dir.GetFiles(), file =>
        {
            string tempPath = Path.Combine(destDirPath, file.Name);
            file.CopyTo(tempPath, true);
        });

        if (copySubDirs)
        {
            // Use Parallel.ForEach to copy subdirectories in parallel
            Parallel.ForEach(dirs, subdir =>
            {
                string tempPath = Path.Combine(destDirPath, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            });
        }
    }

    /// <summary>
    /// Threading manier: locks
    /// 
    /// Voor wanneer meerdere bestanden worden geplakt of misschien nog bestanden worden gekopieerd. Door de lock 
    /// heeft alleen 1 bestand of map toegang tot de _copiedFilesPaths list. Hierbij kunnen niet meerdere bronnen
    /// de lijst bewerken.
    /// </summary>
    public void PasteItems(string targetPath)
    {
        lock (_copiedFilesPaths)
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

    private int CountFiles(Item item)
    {
        if(item.Type != ItemType.Dir)
        {
            return 1;
        }
        var dir = item as DirectoryItem;
        return dir.ItemCount;
    }

    /// <summary>
    /// This asynchronous function creates a symbolic link (symlink) for a file or folder based on user input.
    /// </summary>
    /// <param name="side">Specifies the side of the file explorer where the link will be created. 
    ///     * 0: Left side
    ///     * 1: Right side
    /// </param>
    /// <returns>Task: An asynchronous task representing the operation.</returns>
    public async void CreateSymbolicLink(int side)
    {
        PopupOpen = true;
        string currentPath = (side == 0 ? LeftSideViewModel.CurrentPath : RightSideViewModel.CurrentPath).Replace("/", "\\");
        string path = await Application.Current.MainPage.DisplayPromptAsync("Enter source path", $"Please enter the path of the file to create a symbolic link of. The symbolic link will be created in the current folder.", "OK", "Cancel", null, maxLength: 100);

        path = path.Replace("/", "\\");

        try
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            if (Directory.Exists(path))
            {
                string linkName = await Application.Current.MainPage.DisplayPromptAsync("Enter link name", $"Please enter the name of the new link folder.", "OK", "Cancel", null, maxLength: 100);
                startInfo.Arguments = "/c mklink /D " + Path.Combine(currentPath, linkName) + " " + path;
            }
            else
            {
                string linkName = await Application.Current.MainPage.DisplayPromptAsync("Enter link name", $"Please enter the name of the new link file.", "OK", "Cancel", null, maxLength: 100);
                startInfo.Arguments = "/c mklink " + Path.Combine(currentPath, linkName) + " " + path;
            }
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                // Handle failure based on exit code (check mklink documentation for specific codes)
                await AppShell.Current.DisplayAlert("Error", "Failed to create symbolic link, error code: " + process.ExitCode, "OK");
            }
            else
            {
                await AppShell.Current.DisplayAlert("Success", "Symbolic link has been created. ", "OK");
            }
        } catch (Exception e)
        {
            await AppShell.Current.DisplayAlert("Error", "Failed to create symbolic link: " + e.Message, "OK");
        }
        PopupOpen = false;
        if(side == 0)
        {
            await LeftSideViewModel.RefreshAsync();
        } else
        {
            await RightSideViewModel.RefreshAsync();
        }
    }
}