using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.ViewModels;
using SharpHook;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{

    private FileOverviewViewModel viewModel;

    public FileOverviewPage()
    {
        InitializeComponent();
        viewModel = new FileOverviewViewModel();
        BindingContext = viewModel;
        RegisterKeybindingsAsync();
    }

    private async Task RegisterKeybindingsAsync()
    {
        var hook = new SimpleGlobalHook();
        hook.KeyPressed += OnKeyPressed;
        await hook.RunAsync();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        string key = e.Data.KeyCode.ToString().Substring(2).ToLower();
        Debug.WriteLine(key);
        switch(key)
        {
            case "f5":
                //Refresh
                return;
            case "f6":
                //Copy
                return;
            case "f7":
                //Move
                return;
            case "f2":
                //Rename current item.
                return;
            case "f8":
                //Delete current item.
                return;
            case "backspace":
                //Parent folder.
                return;
            case "enter":
                //Open current file/folder.
                return;
        }
    }

    void OnItemTapped(object sender, EventArgs e)
    {
        var item = ((sender as Grid).BindingContext as Item);

        if(item.Type == ItemType.Drive ||  item.Type == ItemType.TopDir)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine(item.Side);

        if (item.Side == 0)
        {
            //Left side.
            if (LeftCollection.SelectedItems.Contains(item))
            {
                LeftCollection.SelectedItems.Remove(item);
            }
            else
            {
                LeftCollection.SelectedItems.Add(item);
            }

        }
        else if (item.Side == 1)
        {
            //Right side.
            if (RightCollection.SelectedItems.Contains(item))
            {
                RightCollection.SelectedItems.Remove(item);
            }
            else
            {
                RightCollection.SelectedItems.Add(item);
            }
        }


    
    }

    void onDragStarting(object sender, DragStartingEventArgs e)
    {
        //TODO: Add current dragged item to selected items.
        var dragGestureRecognizer = (DragGestureRecognizer)sender;
        var grid = (Grid)dragGestureRecognizer.Parent;
        var item = (Item)grid.BindingContext;

        if (item.Type == ItemType.Drive || item.Type == ItemType.TopDir)
        {
            e.Cancel = true;
            return;
        }

      
        if(item.Side == 0)
        {
            //Left side.
            foreach (var debugItem in LeftCollection.SelectedItems)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", LeftCollection.SelectedItems);

        }
        else if(item.Side == 1)
        {
            //Right side.
            foreach (var debugItem in RightCollection.SelectedItems)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", RightCollection.SelectedItems);
        }


    }

    //void OnCollectionViewSizeChanged(object sender, EventArgs e)
    //{
    //    // Replace YourCollectionViewName with the name of your CollectionView
    //    RightCollection.ItemsSource = null;
    //    RightCollection.ItemsSource = Files;
    //}

}
