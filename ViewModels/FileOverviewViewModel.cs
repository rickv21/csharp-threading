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
        _files = new ObservableCollection<FileItem>();
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));
        _files.Add(new FileItem("asdfghjk", "Resources\\Images\\folder_icon.jpg"));


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