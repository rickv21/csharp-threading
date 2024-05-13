using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.StartScreen;
using SharpHook;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{

    private FileOverviewViewModel viewModel;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

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
        var activeWindowHandle = GetForegroundWindow();
        var currentProcess = Process.GetCurrentProcess();

        //Stop key events being read when application is not in focus.
        if (activeWindowHandle != currentProcess.MainWindowHandle)
        {
            return;
        }

        //Remove first two characters from key and make it lower case.
        string key = e.Data.KeyCode.ToString()[2..].ToLower();
        
        //Force unfocus of collectionviews to prevent issues with keyboard selections.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LeftCollection.Unfocus();
            RightCollection.Unfocus();
        });

        //Some more keyboard selection prevention.
        if(key == "tab")
        {
            e.SuppressEvent = true;
        }

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
        if (item.Side == 0)
        {
            //Left side.
            LeftPathField.Unfocus();
            LeftBorder.Stroke = Colors.Aqua;
            RightBorder.Stroke = Colors.Transparent;
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
            RightPathField.Unfocus();
            RightBorder.Stroke = Colors.Aqua;
            LeftBorder.Stroke = Colors.Transparent;
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


        if (item.Side == 0)
        {
            //Left side.
            foreach (var debugItem in LeftCollection.SelectedItems)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", LeftCollection.SelectedItems);
            if (!LeftCollection.SelectedItems.Contains(item))
            {
                LeftCollection.SelectedItems.Add(item);
            }

        }
        else if (item.Side == 1)
        {
            //Right side.
            foreach (var debugItem in RightCollection.SelectedItems)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", RightCollection.SelectedItems);
            if (!RightCollection.SelectedItems.Contains(item))
            {
                RightCollection.SelectedItems.Add(item);
            }
        }


    }

    void OnItemDrop(object sender, DropEventArgs e)
    {
        var droppedItems = e.Data.Properties["files"] as IList<object>;


        if (droppedItems != null && droppedItems.Count > 0)
        {
            var itemList = droppedItems.OfType<Item>().ToList();

            _ = OnItemDropAsync(itemList);
        }

    }
    
    
    private void RightContextClick(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        if(item.Text == "Refresh")
        {
            viewModel.RightSideViewModel.Refresh();
        } else if(item.Text == "Rename")
        {
            //TODO
            viewModel.RightSideViewModel.RenameItem(null, null);
        }
    }

    private void LeftContextClick(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        if (item.Text == "Refresh")
        {
            viewModel.LeftSideViewModel.Refresh();
        }
        else if (item.Text == "Rename")
        {
            //TODO
            viewModel.LeftSideViewModel.RenameItem(null, null);
        }
    }
    
    //void OnCollectionViewSizeChanged(object sender, EventArgs e)
    //{
    //    // Replace YourCollectionViewName with the name of your CollectionView
    //    RightCollection.ItemsSource = null;
    //    RightCollection.ItemsSource = Files;
    //}

    async Task OnItemDropAsync(IList<Item> items)
    {
        if(items.Count == 0)
        {
            return;
        }
        var needsRegex = false;
        if (items.Count > 1)
        {
            needsRegex = true;
        }
        else if (items[0].Type == ItemType.Dir)
        {
            needsRegex = true;
        }
        
        string action = await viewModel.SelectActionAsync();
        if (action != null && action != "Cancel")
        {
            (string, string?) userInput = await viewModel.PromptUserAsync(action.ToLower(), needsRegex);
            var numnerOfThreads = userInput.Item1;
            var regex = userInput.Item2;
            if (userInput.Item1 != null)
            {
                if (int.TryParse(numnerOfThreads, out int number) && number > 0 && number <= FileOverviewViewModel.MAX_THREADS)
                {
                    await viewModel.ProcessActionAsync(action, number, regex);
                }
                else if (number > FileOverviewViewModel.MAX_THREADS)
                {
                    await DisplayAlert("Error", $"Number of threads cannot exceed {FileOverviewViewModel.MAX_THREADS}.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Invalid input or non-positive number entered.", "OK");
                }
            }
        }
    }
}

