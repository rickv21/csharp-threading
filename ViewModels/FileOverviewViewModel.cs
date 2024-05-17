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
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace FileManager.ViewModels;

/// <summary>
/// Threading: async & await.
/// 
/// In the viewmodels async and await are used to run code without blocking the UI thread.
/// </summary>
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

    public static async Task<string> SelectActionAsync()
    {
        return await Application.Current.MainPage.DisplayActionSheet("Select Action", "Cancel", null, "Copy", "Paste", "Move");
    }

    public static async Task<(string, string?)> PromptUserAsync(string action, bool isDir = false)
    {
        string number = await Application.Current.MainPage.DisplayPromptAsync("Enter Number", $"Number of threads for {action}:", "OK", "Cancel", "0", maxLength: 10, keyboard: Microsoft.Maui.Keyboard.Numeric);
        if (int.Parse(number) > MAX_THREADS || int.Parse(number) < 1)
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

    public async Task ProcessActionAsync(string action, int number, string regex, ContentPage view, IList<Item> items, List<object> selectedItems, string targetPath)
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
                    await CopyItems(selectedItems, number);
                    break;
                case "Paste":
                    await Task.Delay(2000);
                    await PasteItems(targetPath, regex);
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
            return LeftSideViewModel.GetCurrentPath();
        }
        else if (collectionView == _rightCollection)
        {
            return RightSideViewModel.GetCurrentPath();
        }
        return null;
    }

    private List<string> _copiedFilesPaths = [];
    public List<string> CopiedFilesPaths
    {
        get { return _copiedFilesPaths; }
        set { _copiedFilesPaths = value; }
    }

    private readonly string tempCopyDirectory = Path.Combine(Path.GetTempPath(), "FileManagerCopiedItems");

    /// <summary>
    /// Threading: Locks en Task Parallel Library (TPL)
    /// 
    /// For copying multiple files, a combination of locking and the Task Parallel Library (TPL) is used.
    /// The lock ensures that only one thread at a time can access the _copiedFilesPaths list to prevent race conditions.
    /// The TPL is used to perform the copying of each individual file or directory in parallel on the ThreadPool using tasks.
    /// This can lead to better performance, especially when copying many files or large files.
    /// 
    /// TPL is not the same as threadpool. It provides higher-level constructs for parallel programming, while ThreadPool is a low-level mechanism for managing threads.
    /// </summary>
    public async Task CopyItems(List<object> selectedItems, int maxDegreeOfParallelism)
    {
        // Lock to ensure thread safety when modifying shared resources
        lock (_copiedFilesPaths)
        {
            // Clear the list of copied files paths
            _copiedFilesPaths.Clear();

            // Ensure the temporary directory exists
            if (!Directory.Exists(tempCopyDirectory))
            {
                Directory.CreateDirectory(tempCopyDirectory);
            }
        }

        // Create a dictionary to store the thread IDs
        var threadIds = new ConcurrentDictionary<int, bool>();

        await Task.Run(() =>
        {
            // TPL with specific degree of parallelism
            Parallel.ForEach(selectedItems, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item =>
            {
                // Get the current thread ID
                int threadId = Thread.CurrentThread.ManagedThreadId;

                // Add the thread ID to the dictionary if it doesn't exist
                threadIds.TryAdd(threadId, true);

                if (item is FileItem fileItem) // If file
                {
                    string fileName = Path.GetFileName(fileItem.FilePath);
                    string tempFilePath = Path.Combine(tempCopyDirectory, fileName);

                    // Perform file copy synchronously
                    File.Copy(fileItem.FilePath, tempFilePath, true);

                    // Lock within the synchronous method
                    lock (_copiedFilesPaths)
                    {
                        _copiedFilesPaths.Add(tempFilePath);
                    }
                }
                else if (item is DirectoryItem directoryItem) // If directory
                {
                    string dirName = Path.GetFileName(directoryItem.FilePath);
                    string tempDirPath = Path.Combine(tempCopyDirectory, dirName);

                    // Perform directory copy synchronously
                    DirectoryCopy(directoryItem.FilePath, tempDirPath, true);

                    // Lock within the synchronous method
                    lock (_copiedFilesPaths)
                    {
                        _copiedFilesPaths.Add(tempDirPath);
                    }
                }
            });
        });

        // Print the number of threads used
        Debug.WriteLine($"Number of threads used: {threadIds.Count}");
    }

    /// <summary>
    /// Threading: Threadpool
    ///  
    /// This method recursively copies a directory to a new location, copying files and subdirectories in parallel using .NET's ThreadPool.
    /// By using Parallel.ForEach, .NET's underlying ThreadPool is used to distribute iterations across files and directories across multiple threads.
    /// This makes effective use of available CPU cycles and can lead to better performance, especially when copying large amounts of data or using slow storage media.
    /// </summary>
    /// <param name="sourceDirPath"></param>
    /// <param name="destDirPath"></param>
    /// <param name="copySubDirs"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Use ThreadPool to copy files and directories
        var copyTasks = new List<Task>();

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var copyTask = new Task(() =>
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            });

            // Queue the task to the ThreadPool
            ThreadPool.QueueUserWorkItem(_ => copyTask.Start());
            copyTasks.Add(copyTask);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                var copyTask = new Task(() =>
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                });

                // Queue the task to the ThreadPool
                ThreadPool.QueueUserWorkItem(_ => copyTask.Start());
                copyTasks.Add(copyTask);
            }
        }

        // Wait for all copy tasks to complete
        Task.WaitAll(copyTasks.ToArray());
    }

    /// <summary>
    /// Threading: locks
    /// 
    /// For when multiple files are pasted or perhaps files are copied. By the lock 
    /// only 1 file or directory has access to the _copiedFilesPaths list. Here, multiple sources cannot
    /// edit the list.
    /// </summary>
    public async Task PasteItems(string targetPath, string regex)
    {
        lock (_copiedFilesPaths)
        {
            foreach (var sourcePath in _copiedFilesPaths)
            {
                string fileName = Path.GetFileName(sourcePath);
                if (Regex.IsMatch(fileName, regex))
                {

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
            }

            // empty copy list
            _copiedFilesPaths.Clear();
        }
    }

    private static void DirectoryMove(string sourceDirPath, string destDirPath)
    {
        DirectoryInfo dir = new(sourceDirPath);

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
        if (item.Type != ItemType.Dir)
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
        if(path == null)
        {
            return;
        }
        if(path == "")
        {
            await AppShell.Current.DisplayAlert("Error", "The path cannot be empty.", "OK");
            return;
        }
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
                if (linkName == null)
                {
                    return;
                }
                if (linkName == "")
                {
                    await AppShell.Current.DisplayAlert("Error", "The folder name cannot be empty.", "OK");
                    return;
                }
                startInfo.Arguments = "/c mklink /D " + Path.Combine(currentPath, linkName) + " " + path;
            }
            else
            {
                string linkName = await Application.Current.MainPage.DisplayPromptAsync("Enter link name", $"Please enter the name of the new link file.", "OK", "Cancel", null, maxLength: 100);
                if (linkName == null)
                {
                    return;
                }
                if (linkName == "")
                {
                    await AppShell.Current.DisplayAlert("Error", "The file name cannot be empty.", "OK");
                    return;
                }
                startInfo.Arguments = "/c mklink " + Path.Combine(currentPath, linkName) + " " + path;
            }
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                // Handle failure based on exit code.
                await AppShell.Current.DisplayAlert("Error", "Failed to create symbolic link.", "OK");
            }
            else
            {
                await AppShell.Current.DisplayAlert("Success", "Symbolic link has been created. ", "OK");
            }
        } catch (Exception e)
        {
            if (e is Win32Exception exception)
            {
                if(exception.NativeErrorCode == 1223)
                {
                    //Permission canceled.
                    return;
                }
            }
            await AppShell.Current.DisplayAlert("Error", "Something went wrong creating the symbolic link.", "OK");
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