using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand ItemDoubleTappedCommand { get; }

    public FileOverviewViewModel()
    {
        _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        DirectoryInfo d = new DirectoryInfo(_currentPath);
        _files = new ObservableCollection<Item>();

        ItemDoubleTappedCommand = new Command<Item>(OnItemDoubleTapped);
        FillList(d);
    }

    void OnItemDoubleTapped(Item item)
    {
        System.Diagnostics.Debug.WriteLine("Testing - " + item.FilePath);
        if(item.FileName == "...")
        {
        
            DirectoryInfo directoryInfo = Directory.GetParent(_currentPath);
            _currentPath = directoryInfo.FullName;
            _files.Clear();
            FillList(directoryInfo);
            return;
        }
        if (Directory.Exists(item.FilePath))
        {
            System.Diagnostics.Debug.WriteLine("Exists");
            // If the item is a folder, update the Files collection to show the contents of the folder
            DirectoryInfo directoryInfo = new DirectoryInfo(item.FilePath);
            _currentPath = item.FilePath;
            _files.Clear();
            FillList(directoryInfo);
        }
    }


    private void FillList(DirectoryInfo d)
    {
        _files.Add(new DirectoryItem("...", "", 0));
        try
        {

            foreach (FileSystemInfo item in d.GetFileSystemInfos())
            {
                System.Diagnostics.Debug.WriteLine(item.FullName);
                if ((item.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    // It's a directory
                    DirectoryInfo dirInfo = new DirectoryInfo(item.FullName);
                    _files.Add(new DirectoryItem(dirInfo.Name, dirInfo.FullName, 0));
                }
                else
                {
                    // It's a file
                    FileInfo fileInfo = new FileInfo(item.FullName);
                    _files.Add(new FileItem(fileInfo.Name, fileInfo.FullName, 0, ""));
                }
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