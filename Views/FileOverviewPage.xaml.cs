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
        var item = ((sender as StackLayout).BindingContext as Item);
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
        var item = (Item)stackLayout.BindingContext;

        e.Data.Properties.Add("files", RightCollection.SelectedItems);
        // Replace YourItemType with the type of the items in your CollectionView
        // Replace YourCollectionViewName with the name of your CollectionView
    }

    //void OnCollectionViewSizeChanged(object sender, EventArgs e)
    //{
    //    // Replace YourCollectionViewName with the name of your CollectionView
    //    RightCollection.ItemsSource = null;
    //    RightCollection.ItemsSource = Files;
    //}

}
