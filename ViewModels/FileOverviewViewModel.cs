using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileManager.Models;

namespace FileManager.ViewModels;

public class FileOverviewViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Item> _files;
    private String _currentPath;
    public ObservableCollection<Item> Files
    {
        get { return _files; }
        set
        {
            _files = value;
        }
    }
    public String CurrentPath
    {
        get { return _currentPath; }
        set
        {
            _currentPath = value;
            OnPropertyChanged(nameof(CurrentPath));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand ItemDoubleTappedCommand { get; }

    public FileOverviewViewModel()
    {
        CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DirectoryInfo d = new DirectoryInfo(_currentPath);
        _files = new ObservableCollection<Item>();

        ItemDoubleTappedCommand = new Command<Item>(OnItemDoubleTapped);
        FillList(d);
    }

    void OnItemDoubleTapped(Item item)
    {
        System.Diagnostics.Debug.WriteLine("Testing - " + item.FilePath);
        if (item is DirectoryItem)
        {
            if (item.FileName == "...")
            {
                DirectoryInfo directoryInfo = Directory.GetParent(_currentPath);
                CurrentPath = directoryInfo.FullName;
                _files.Clear();
                FillList(directoryInfo);
                return;
            }
            if (Directory.Exists(item.FilePath))
            {
                System.Diagnostics.Debug.WriteLine("Exists");
                // If the item is a folder, update the Files collection to show the contents of the folder
                DirectoryInfo directoryInfo = new DirectoryInfo(item.FilePath);
                CurrentPath = item.FilePath;
                _files.Clear();
                FillList(directoryInfo);
                return;
            }
        } else if(item is FileItem)
        {
            FileInfo fileInfo = new FileInfo(item.FilePath);
            Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
        }
    }


    private void FillList(DirectoryInfo d)
    {
        if (d.Parent != null)
        {
            _files.Add(new DirectoryItem("...", "", 0));
        }
        try
        {

            ConcurrentBag<Item> fileSystemInfos = new ConcurrentBag<Item>();

            Parallel.ForEach(d.EnumerateDirectories(), dir =>
            {
                // It's a directory
                DirectoryInfo dirInfo = new DirectoryInfo(dir.FullName);
                fileSystemInfos.Add(new DirectoryItem(dirInfo.Name, dirInfo.FullName, 0));
            });

            Parallel.ForEach(d.EnumerateFiles(), file =>
            {
                FileInfo fileInfo = new FileInfo(file.FullName);
                fileSystemInfos.Add(new FileItem(fileInfo.Name, fileInfo.FullName, 0, ""));
            });

       

            foreach (var item in fileSystemInfos.OrderBy(fsi => !(fsi is DirectoryInfo)).ThenBy(fsi => fsi.FileName))
            {
                _files.Add(item);
            }

        } catch {
        //Check for no permission.
        
        }
    }


    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}