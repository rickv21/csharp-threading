using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.ViewModels;
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
        if (key == "tab")
        {
            e.SuppressEvent = true;
        }

        //Ignore enter if path field is focused.
        if ((key == "enter" || key == "numpadenter"))
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

        viewModel.UpdateSelected(LeftCollection.SelectedItems, RightCollection.SelectedItems);
    }


    void OnDragStarting(object sender, DragStartingEventArgs e)
    {
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
            if (!LeftCollection.SelectedItems.Contains(item))
            {
                LeftCollection.SelectedItems.Add(item);
            }

            viewModel.DroppedFiles = LeftCollection.SelectedItems.Cast<Item>();
            foreach (var debugItem in viewModel.DroppedFiles)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", viewModel.DroppedFiles);

        }
        else if (item.Side == 1)
        {
            //Right side.
            if (!RightCollection.SelectedItems.Contains(item))
            {
                RightCollection.SelectedItems.Add(item);
            }

            viewModel.DroppedFiles = RightCollection.SelectedItems.Cast<Item>();
            foreach (var debugItem in viewModel.DroppedFiles)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", viewModel.DroppedFiles);
        }
    }

    private void RightContextClick(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        if (item.Text == "Refresh")
        {
            viewModel.RightSideViewModel.Refresh();
        }
        else if (item.Text == "Rename")
        {
            //TODO
            viewModel.RightSideViewModel.RenameItem(null, null);
        }
        else if (item.Text == "Delete")
        {
            viewModel.RightSideViewModel.DeleteItem();
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
        else if (item.Text == "Delete")
        {
            viewModel.RightSideViewModel.DeleteItem();
        }
    }

    void FileDrop(object sender, DropEventArgs e)
    {
        var dropGestureRecognizer = (DropGestureRecognizer)sender;
        var collectionView = FindParentCollectionView(dropGestureRecognizer);

        if (collectionView != null)
        {
            var viewModel = BindingContext as FileOverviewViewModel;

            // Get target path
            string targetPath = string.Empty;
            if (collectionView == RightCollection)
            {
                targetPath = viewModel.RightSideViewModel.CurrentPath;
            }
            else if (collectionView == LeftCollection)
            {
                targetPath = viewModel.LeftSideViewModel.CurrentPath;
            }

            // Ensure thread safety with locking
            lock (viewModel)
            {
                // Iterate over dropped files and move them to the target path
                foreach (var file in viewModel.DroppedFiles)
                {
                    // Ensure that the file is not null and the target path is valid
                    if (file != null && !string.IsNullOrEmpty(targetPath))
                    {
                        MoveFile(file, targetPath);
                    }
                }

                viewModel.RightSideViewModel.RefreshFiles();
                viewModel.LeftSideViewModel.RefreshFiles();
            }
        }
    }

    private CollectionView FindParentCollectionView(DropGestureRecognizer dropGestureRecognizer)
    {
        var parent = dropGestureRecognizer.Parent;
        while (parent != null)
        {
            if (parent is CollectionView collectionView)
            {
                return collectionView;
            }
            parent = (parent as Element)?.Parent;
        }
        return null;
    }

    private void MoveFile(Item file, string targetPath)
    {
        // Get only the file name from the file path
        string fileName = Path.GetFileName(file.FilePath);

        // Move the file from its current FilePath to the new targetPath
        var newFilePath = Path.Combine(targetPath, fileName);
        if (File.Exists(file.FilePath))
        {
            File.Move(file.FilePath, newFilePath);
        }
        else if (Directory.Exists(file.FilePath))
        {
            // Move the directory to the new location
            Directory.Move(file.FilePath, newFilePath);
        }

        // Update the FilePath property of the Item object with the new path
        file.FilePath = newFilePath;
    }

    //void OnCollectionViewSizeChanged(object sender, EventArgs e)
    //{
    //    // Replace YourCollectionViewName with the name of your CollectionView
    //    RightCollection.ItemsSource = null;
    //    RightCollection.ItemsSource = Files;
    //}

}
