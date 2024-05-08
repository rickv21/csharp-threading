namespace FileManager.Models
{
    public class Count
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
    }
}
