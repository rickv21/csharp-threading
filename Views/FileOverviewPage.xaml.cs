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
        string key = e.Data.KeyCode.ToString()[2..].ToLower();
        Debug.WriteLine(key);
        //Ignore enter if path field is focused.
        if ((key == "enter" || key == "numpadenter"))
        {
            if (viewModel.ActiveSide == 0)
            {
                if(LeftPathField.IsFocused)
                {
                    return;
                }
            }
            else if (viewModel.ActiveSide == 1)
            {
                if (RightPathField.IsFocused)
                {
                    return;
                }
            }


        }
        viewModel.PassClickEvent(key);
    }

    void OnItemTapped(object sender, EventArgs e)
    {
        var item = ((sender as Grid).BindingContext as Item);

        viewModel.ActiveSide = item.Side;

        if(item.Type == ItemType.Drive ||  item.Type == ItemType.TopDir)
        {
            return;
        }

        Debug.WriteLine(item.Side);

        if (item.Side == 0)
        {
            //Left side.
            LeftPathField.Unfocus();
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
            RightPathField.Unfocus();
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

        viewModel.UpdateSelected(LeftCollection.SelectedItems, RightCollection.SelectedItems);
    
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
