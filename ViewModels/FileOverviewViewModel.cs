using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FileManager.Models;

namespace FileManager.ViewModels;

public class FileOverviewViewModel : INotifyPropertyChanged
{
    private ObservableCollection<FileItem> _files;
    public ObservableCollection<FileItem> Files
    {
        get { return _files; }
        set
        {
            _files = value;
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

   
    public FileOverviewViewModel()
    {
        DirectoryInfo d = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        _files = new ObservableCollection<FileItem>();

        FileInfo[] Files = d.GetFiles(); //Getting Text files
 

        foreach (FileInfo file in Files)
        {
            _files.Add(new FileItem(file.Name, "folder_icon.jpg"));
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