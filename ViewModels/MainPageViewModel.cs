using FileManager.Models;
using System.ComponentModel;
using System.Windows.Input;

namespace FileManager.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private Count _count;
        

        public string CounterText => $"Clicked {_count.Value} {(_count.Value == 1 ? "time" : "times")}";

        public ICommand IncrementCountCommand { get; }

        public MainPageViewModel()
        {
            IncrementCountCommand = new Command(IncrementCount);
            _count = new Count();
            _count.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Count.Value))
                {
                    OnPropertyChanged(nameof(CounterText));
                }
            };
        }

        private void IncrementCount()
        {
            _count.Increment();
            SemanticScreenReader.Announce(CounterText);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}