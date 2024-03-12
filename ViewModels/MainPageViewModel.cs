using FileManager.Models;
using System.ComponentModel;
using System.Windows.Input;

namespace FileManager.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private Count _count;
        private ConfigModel configModel;

        public string CounterText => $"Clicked {_count.Value} {(_count.Value == 1 ? "time" : "times")}";

        private string _labelText;
        public string LabelText
        {
            get { return _labelText; }
            set
            {
                if (_labelText != value)
                {
                    _labelText = value;
                    OnPropertyChanged(nameof(LabelText));
                }
            }
        }

        public ICommand IncrementCountCommand { get; }



        public MainPageViewModel()
        {
            configModel = ConfigModel.LoadOrCreateDefault();
            LabelText = configModel.Option1;
            IncrementCountCommand = new Command(IncrementCount);
            _count = new Count();
            _count.Value = configModel.ClickCount;

            PropertyChanged += (sender, args) =>
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
            configModel.ClickCount = _count.Value;
            configModel.Save();
            SemanticScreenReader.Announce(CounterText);
            OnPropertyChanged(nameof(CounterText));
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}