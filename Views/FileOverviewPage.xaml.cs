using FileManager.Models;
using FileManager.ViewModels;
using SharpHook;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        Task.Run(() => RegisterKeybindingsAsync());
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
        if (key == "tab")
        {
            e.SuppressEvent = true;
        }

        //Ignore enter if path field is focused.
        if ((key == "enter" || key == "numpadenter") || key == "backspace")
        {
            if (viewModel.ActiveSide == 0)
            {
                if (LeftPathField.IsFocused)
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

        if (item.Type == ItemType.Drive || item.Type == ItemType.TopDir)
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

        //viewModel.UpdateSelected(LeftCollection.SelectedItems, RightCollection.SelectedItems);
    }


    void OnDragStarting(object sender, DragStartingEventArgs e)
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

        }
        else if (item.Side == 1)
        {
            //Right side.
            foreach (var debugItem in RightCollection.SelectedItems)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", RightCollection.SelectedItems);
        }


    }

    private void RightContextClick(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        if (item.Text == "Refresh")
        {
            Task.Run(() => viewModel.RightSideViewModel.RefreshAsync());
        }
        else if (item.Text == "Rename")
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
            Task.Run(() => viewModel.LeftSideViewModel.RefreshAsync());
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

}
