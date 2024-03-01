using FileManager.Models;

namespace FileManager
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        ConfigModel configModel;

        public MainPage()
        {
            InitializeComponent();
            //Config test code.
            configModel = ConfigModel.LoadOrCreateDefault();

            TitleLabel.Text = configModel.Option1;
            count = configModel.ClickCount;
            CounterBtn.Text = $"Clicked {count} time";
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
            configModel.ClickCount = count;
            configModel.Save();
        }
    }

}
