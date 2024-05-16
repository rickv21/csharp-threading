using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FileManager.Models;
using FileManager.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.StartScreen;
using SharpHook;
using CommunityToolkit.Maui.Views;
using FileManager.Views.Popups;
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

        //Ignore enter and backspace if path entry field is focused.
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

            // e.Data.Properties.Add("files", viewModel.DroppedFiles);  
            e.Data.Properties.Add("files", LeftCollection.SelectedItems);

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

            e.Data.Properties.Add("files", RightCollection.SelectedItems);
        }
    }

    void OnItemDrop(object sender, DropEventArgs e)
    {
        var droppedItems = e.Data.Properties["files"] as IList<object>;


        if (droppedItems != null && droppedItems.Count > 0)
        {
            var itemList = droppedItems.OfType<Item>().ToList();

            _ = OnItemDropAsync(sender, itemList);
        }

    }


    private async void RightContextClick(object sender, EventArgs e)
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
        else if (item.Text == "Delete")
        {
            if (RightCollection.SelectedItems.Count == 0)
            {
                await DisplayAlert("Alert", "You have to select first to delete", "OK");
                return;
            }

            viewModel.RightSideViewModel.DeleteItem();
        }
        else if (item.Text == "Copy")
        {
            viewModel.CopyItems(RightCollection.SelectedItems.ToList());
        }
        else if (item.Text == "Paste")
        {
            viewModel.PasteItems(viewModel.RightSideViewModel.CurrentPath);
        }
    }

    private async void LeftContextClick(object sender, EventArgs e)
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
        else if (item.Text == "Delete")
        {
            if (LeftCollection.SelectedItems.Count == 0)
            {
                await DisplayAlert("Alert", "You have to select first to delete", "OK");
                return;
            }

            viewModel.LeftSideViewModel.DeleteItem();
        }
        else if (item.Text == "Copy")
        {
            viewModel.CopyItems(LeftCollection.SelectedItems.ToList());
        }
        else if (item.Text == "Paste")
        {
            viewModel.PasteItems(viewModel.LeftSideViewModel.CurrentPath);
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
                        //TODO: Needs to be connected to popups (@Monique).
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

    private async Task MoveFile(Item file, string targetPath)
    {
        // Get only the file name from the file path
        string fileName = Path.GetFileName(file.FilePath);

        // Combine the targetPath with the file name to get the new file path
        var newFilePath = Path.Combine(targetPath, fileName);

        // Check if it's a file or directory
        if (File.Exists(file.FilePath))
        {
            // Move the file to the new location
            File.Move(file.FilePath, newFilePath);

            // Update the FilePath property of the Item object with the new path
            file.FilePath = newFilePath;
        }
        else if (Directory.Exists(file.FilePath))
        {
            // Create the destination directory if it doesn't exist
            if (!Directory.Exists(newFilePath))
            {
                Directory.CreateDirectory(newFilePath);
            }

            // Copy the directory and its contents recursively
            CopyDirectory(file.FilePath, newFilePath);

            // Delete the original directory
            Directory.Delete(file.FilePath, true);

            // Update the FilePath property of the Item object with the new path
            file.FilePath = newFilePath;
        }


        viewModel.RightSideViewModel.RefreshFiles();
        viewModel.LeftSideViewModel.RefreshFiles();

        await Task.CompletedTask;
    }


    // Helper method to copy directory recursively
    private void CopyDirectory(string sourceDirPath, string destDirPath)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

        // Create the destination directory if it doesn't exist
        if (!Directory.Exists(destDirPath))
        {
            Directory.CreateDirectory(destDirPath);
        }

        // Copy files
        foreach (string filePath in Directory.GetFiles(sourceDirPath))
        {
            string fileName = Path.GetFileName(filePath);
            string destFilePath = Path.Combine(destDirPath, fileName);
            File.Copy(filePath, destFilePath, true);
        }

        // Copy subdirectories recursively
        foreach (string subDirPath in Directory.GetDirectories(sourceDirPath))
        {
            string dirName = Path.GetFileName(subDirPath);
            string destSubDirPath = Path.Combine(destDirPath, dirName);
            CopyDirectory(subDirPath, destSubDirPath);
        }
    }

    private string GetSelectedFolderPath(int side)
    {
        IList<Item> selectedItems = null;
        if (side == 0)
        {
            selectedItems = LeftCollection.SelectedItems.Cast<Item>().ToList();
        }
        else if (side == 1)
        {
            selectedItems = RightCollection.SelectedItems.Cast<Item>().ToList();
        }

        if (selectedItems != null && selectedItems.Count == 1)
        {
            var selectedItem = selectedItems[0];
            if (selectedItem.Type == ItemType.Dir)
            {
                return selectedItem.FilePath; // Return the path of the selected folder
            }
        }

        return null;
    }

    //void OnCollectionViewSizeChanged(object sender, EventArgs e)
    //{
    //    // Replace YourCollectionViewName with the name of your CollectionView
    //    RightCollection.ItemsSource = null;
    //    RightCollection.ItemsSource = Files;
    //}

    async Task OnItemDropAsync(object sender, IList<Item> items)
    {
        if (items.Count == 0)
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

        if (action != null && action != "Cancel" && action != "Move")
        {
            try
            {
                (string, string?) userInput = await viewModel.PromptUserAsync(action.ToLower(), needsRegex);
                var numnerOfThreads = userInput.Item1;
                var regex = userInput.Item2;
                if (userInput.Item1 != null)
                {
                    if (int.TryParse(numnerOfThreads, out int number) && number > 0 && number <= FileOverviewViewModel.MAX_THREADS)
                    {
                        if (LeftCollection.SelectedItems.ToList().Count() >= 1)
                        {
                            Debug.WriteLine("Target L to R " + GetSelectedFolderPath(0));
                            await viewModel.ProcessActionAsync(action, number, regex, this, items, LeftCollection.SelectedItems.ToList(), GetSelectedFolderPath(1));
                        }
                        if (RightCollection.SelectedItems.ToList().Count() >= 1)
                        {
                            Debug.WriteLine("Target R to L " + GetSelectedFolderPath(0));

                            await viewModel.ProcessActionAsync(action, number, regex, this, items, RightCollection.SelectedItems.ToList(), GetSelectedFolderPath(0));
                        }
                    }
                    else if (number < 1)
                    {
                        await DisplayAlert("Error", $"Number of threads must be 1 or more.", "OK");
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
            catch (Exception e)
            {
                await DisplayAlert("Error", "Invalid input or non-positive number entered.", "OK");
            }
        }
        else if (action != null && action == "Move")
        {
            var dropGestureRecognizer = (DropGestureRecognizer)sender;
            var collectionView = FindParentCollectionView(dropGestureRecognizer);

            string targetPath = string.Empty;
            if (collectionView == RightCollection)
            {
                targetPath = viewModel.RightSideViewModel.CurrentPath;
            }
            else if (collectionView == LeftCollection)
            {
                targetPath = viewModel.LeftSideViewModel.CurrentPath;
            }


            // Iterate over dropped files and move them to the target path
            foreach (var file in viewModel.DroppedFiles)
            {
                // Ensure that the file is not null and the target path is valid
                if (file != null && !string.IsNullOrEmpty(targetPath))
                {
                    //TODO: Needs to be connected to popups (@Monique).
                    await MoveFile(file, targetPath);
                }
            }
        }
    }
}

