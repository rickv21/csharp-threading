using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.ViewModels;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{
    
    public FileOverviewPage()
    {
        InitializeComponent();
        BindingContext = new FileOverviewViewModel();

    }

    void OnItemTapped(object sender, EventArgs e)
    {
        var item = ((sender as StackLayout).BindingContext as FileItem);
        if (RightCollection.SelectedItems.Contains(item))
        {
            RightCollection.SelectedItems.Remove(item);
        }
        else
        {
            RightCollection.SelectedItems.Add(item);
        }
    }

    void onDragStarting(object sender, DragStartingEventArgs e)
    {
        var dragGestureRecognizer = (DragGestureRecognizer)sender;
        var stackLayout = (StackLayout)dragGestureRecognizer.Parent;
        var item = (FileItem)stackLayout.BindingContext;

        e.Data.Properties.Add("files", RightCollection.SelectedItems);
        // Replace YourItemType with the type of the items in your CollectionView
        // Replace YourCollectionViewName with the name of your CollectionView
    }

}
