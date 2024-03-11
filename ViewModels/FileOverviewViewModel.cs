using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileManager.ViewModels;

public class FileOverviewViewModel : INotifyPropertyChanged
{
    private ObservableCollection<string> files;
    public ObservableCollection<string> Files
    {
        get { return files; }
        set
        {
            files = value;
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public FileOverviewViewModel()
    {
        Files = new ObservableCollection<string>();
        Files.Add("aap");
        Files.Add("beer");
        Files.Add("neushoorn");
        Files.Add("youri");
        Files.Add("blue eyes white dragon");
        Files.Add("moss giant");
        Files.Add("lvl 13 skeleton");
        Files.Add("aap 2");
        Files.Add("aap");
        Files.Add("beer");
        Files.Add("neushoorn");
        Files.Add("youri");
        Files.Add("blue eyes white dragon");
        Files.Add("moss giant");
        Files.Add("lvl 13 skeleton");
        Files.Add("aap");
        Files.Add("beer");
        Files.Add("neushoorn");
        Files.Add("youri");
        Files.Add("blue eyes white dragon");
        Files.Add("moss giant");
        Files.Add("lvl 13 skeleton");
        Files.Add("aap 2");
        Files.Add("aap");
        Files.Add("beer");
        Files.Add("neushoorn");
        Files.Add("youri");
        Files.Add("blue eyes white dragon");
        Files.Add("moss giant");
        Files.Add("lvl 13 skeleton");
        Files.Add("aap");
        Files.Add("beer");
        Files.Add("neushoorn");
        Files.Add("youri");
        Files.Add("blue eyes white dragon");
        Files.Add("moss giant");
        Files.Add("lvl 13 skeleton");
        Files.Add("aap");
        Files.Add("beer");
        Files.Add("neushoorn");
        Files.Add("youri");
        Files.Add("blue eyes white dragon");
        Files.Add("moss giant");
        Files.Add("lvl 13 skeleton");
        Files.Add("aap 2");
        Files.Add("aap 2");
        Files.Add("aap 2");
        Files.Add("aap 2");
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