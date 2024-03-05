using System.ComponentModel;

namespace FileManager.Models
{
    public class Count : INotifyPropertyChanged
    {
        private int _value;

        public int Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public Count(int initialValue = 0)
        {
            _value = initialValue;
        }

        public void Increment()
        {
            Value++;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
