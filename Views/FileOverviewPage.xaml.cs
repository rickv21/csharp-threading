using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.ViewModels;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{
    
    public FileOverviewPage()
    {
        InitializeComponent();
        BindingContext = new FileOverviewViewModel();

    }
}