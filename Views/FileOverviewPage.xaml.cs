using System.Diagnostics;
using System.Runtime.InteropServices;
using FileManager.Models;
using FileManager.ViewModels;
using SharpHook;
using Microsoft.Maui.Controls;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{
    private bool isRosterView = false;
    private readonly FileOverviewViewModel viewModel;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    public FileOverviewPage()
    {
        InitializeComponent();
        viewModel = new FileOverviewViewModel();
        BindingContext = viewModel;

        Task.Run(() => RegisterKeybindingsAsync());
    }

    /// <summary>
    /// Registers keybindings asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RegisterKeybindingsAsync()
    {
        var hook = new SimpleGlobalHook();
        hook.KeyPressed += OnKeyPressed;
        await hook.RunAsync();
    }

    /// <summary>
    /// Handles the key press event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="KeyboardHookEventArgs"/> instance containing the event data.</param>
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
            getCurrentView(0).Unfocus();
            getCurrentView(1).Unfocus();
        });

        //Some more keyboard selection prevention.
        /*    if (key == "tab")
            {
                e.SuppressEvent = true;
            }*/

        if (key == "f2")
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ToggleRosterView_Clicked();
            });
            return;
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

    /// <summary>
    /// Handles the item tap event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    public void OnItemTapped(object sender, EventArgs e)
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
            if (getCurrentView(0).SelectedItems.Contains(item))
            {
                getCurrentView(0).SelectedItems.Remove(item);
            }
            else
            {
                getCurrentView(0).SelectedItems.Add(item);
            }

        }
        else if (item.Side == 1)
        {
            //Right side.
            RightPathField.Unfocus();
            RightBorder.Stroke = Colors.Aqua;
            LeftBorder.Stroke = Colors.Transparent;
            if (getCurrentView(1).SelectedItems.Contains(item))
            {
                getCurrentView(1).SelectedItems.Remove(item);
            }
            else
            {
                getCurrentView(1).SelectedItems.Add(item);
            }
        }

        viewModel.UpdateSelected(getCurrentView(0).SelectedItems, getCurrentView(1).SelectedItems);
    }

    /// <summary>
    /// Handles the drag starting event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    public void OnDragStarting(object sender, DragStartingEventArgs e)
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
            if (!getCurrentView(0).SelectedItems.Contains(item))
            {
                getCurrentView(0).SelectedItems.Add(item);
            }
            viewModel.DroppedFiles = getCurrentView(0).SelectedItems.Cast<Item>();
            foreach (var debugItem in viewModel.DroppedFiles)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            // e.Data.Properties.Add("files", viewModel.DroppedFiles);  
            e.Data.Properties.Add("files", getCurrentView(0).SelectedItems);

        }
        else if (item.Side == 1)
        {
            //Right side.
            if (!getCurrentView(1).SelectedItems.Contains(item))
            {
                getCurrentView(1).SelectedItems.Add(item);
            }

            viewModel.DroppedFiles = getCurrentView(1).SelectedItems.Cast<Item>();
            foreach (var debugItem in viewModel.DroppedFiles)
            {
                System.Diagnostics.Debug.WriteLine(debugItem.ToString());
            }

            e.Data.Properties.Add("files", getCurrentView(1).SelectedItems);
        }
    }

    /// <summary>
    /// Handles the item drop event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="DropEventArgs"/> instance containing the event data.</param>
    public void OnItemDrop(object sender, DropEventArgs e)
    {
        var droppedItems = e.Data.Properties["files"] as IList<object>;

        if (droppedItems != null && droppedItems.Count > 0)
        {
            var itemList = droppedItems.OfType<Item>().ToList();

            _ = OnItemDropAsync(sender, itemList);
        }
    }

    /// <summary>
    /// Handles the right-click context menu event on the right side.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private async void RightContextClick(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        if (item.Text.Equals("Refresh"))
        {
            viewModel.RightSideViewModel.RefreshAsync();
        }
        else if (item.Text.Equals("Rename"))
        {
            var menuItem = (MenuFlyoutItem)sender;
            var selectedItem = (Item)menuItem.CommandParameter;
            viewModel.PopupOpen = true;

            string userInput = await DisplayPromptAsync("Rename", "Enter the new name:", "OK", "Cancel", "name...");
            viewModel.PopupOpen = false;
            if (!string.IsNullOrEmpty(userInput))
            {
                FileInfo fileInfo = new(selectedItem.FilePath);
                string extension = fileInfo.Extension;

                FileListViewModel.RenameItem(selectedItem, userInput, extension);
                viewModel.RightSideViewModel.RefreshFiles();
            }
            else
            {
                await DisplayAlert("Error", "Please enter a new name", "OK");
            }
        }
        else if (item.Text == "Delete")
        {
            if (getCurrentView(1).SelectedItems.Count == 0)
            {
                await DisplayAlert("Alert", "You have to select first to delete", "OK");
                return;
            }
            viewModel.RightSideViewModel.DeleteItem();
        }
        else if (item.Text == "Create symbolic link")
        {
            viewModel.CreateSymbolicLink(1);
        }
    }

    /// <summary>
    /// Handles the left-click context menu event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private async void LeftContextClick(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        if (item.Text.Equals("Refresh"))
        {
            await Task.Run(() => viewModel.LeftSideViewModel.RefreshAsync());
        }
        else if (item.Text == "Rename")
        {
            var menuItem = (MenuFlyoutItem)sender;
            Item selectedItem = (Item)menuItem.CommandParameter;
            viewModel.PopupOpen = true;
            string userInput = await DisplayPromptAsync("Rename", "Enter the new name:", "OK", "Cancel", "name...");
            viewModel.PopupOpen = false;
            if (!string.IsNullOrEmpty(userInput))
            {
                FileInfo fileInfo = new(selectedItem.FilePath);
                string extension = fileInfo.Extension;

                FileListViewModel.RenameItem(selectedItem, userInput, extension);
                viewModel.LeftSideViewModel.RefreshFiles();
            }
            else
            {
                await DisplayAlert("Error", "Please enter a new name", "OK");
            }
        }
        else if (item.Text == "Delete")
        {
            if (getCurrentView(0).SelectedItems.Count == 0)
            {
                await DisplayAlert("Alert", "Please select a file to delete first", "OK");
                return;
            }

            await viewModel.LeftSideViewModel.DeleteItem();
            viewModel.LeftSideViewModel.RefreshFiles();
            viewModel.RightSideViewModel.RefreshFiles();

        }
        else if (item.Text == "Create symbolic link")
        {
            viewModel.CreateSymbolicLink(0);
        }
    }

    /// <summary>
    /// Finds the parent CollectionView instance from a given DropGestureRecognizer.
    /// </summary>
    /// <param name="dropGestureRecognizer">The DropGestureRecognizer instance.</param>
    /// <returns>The parent CollectionView instance, or null if not found.</returns>
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

    /// <summary>
    /// Moves a file or directory to the specified target path.
    /// </summary>
    /// <param name="file">The file or directory to move.</param>
    /// <param name="targetPath">The target path where the file or directory should be moved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
            CopyMoveDirectory(file.FilePath, newFilePath);

            // Delete the original directory
            Directory.Delete(file.FilePath, true);

            // Update the FilePath property of the Item object with the new path
            file.FilePath = newFilePath;
        }


        viewModel.RightSideViewModel.RefreshFiles();
        viewModel.LeftSideViewModel.RefreshFiles();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Adds a new tab to the left side.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    void AddLeftTab(object sender, EventArgs e)
    {
        Task.Run(async () => await viewModel.AddTabAsync(0));
    }

    /// <summary>
    /// Removes a tab from the left side.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>

    void RemoveLeftTab(object sender, EventArgs e)
    {
        Task.Run(async () => await viewModel.RemoveTabAsync(0));
    }

    /// <summary>
    /// Adds a new tab to the right side.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>

    void AddRightTab(object sender, EventArgs e)
    {
        Task.Run(async () => await viewModel.AddTabAsync(1));
    }

    /// <summary>
    /// Removes a tab from the right side.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>

    void RemoveRightTab(object sender, EventArgs e)
    {
        Task.Run(async () => await viewModel.RemoveTabAsync(1));
    }

    /// <summary>
    /// Copies a directory recursively from the source path to the destination path. This is a helper method to be able to move
    /// a directory to a different location. 
    /// </summary>
    /// <param name="sourceDirPath">The path of the source directory.</param>
    /// <param name="destDirPath">The path of the destination directory.</param>
    private void CopyMoveDirectory(string sourceDirPath, string destDirPath)
    {
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
            CopyMoveDirectory(subDirPath, destSubDirPath);
        }
    }

    /// <summary>
    /// Gets the path of the selected folder on the specified side.
    /// </summary>
    /// <param name="side">The side to get the selected folder path from (0 for left, 1 for right).</param>
    /// <returns>The path of the selected folder, or null if no folder is selected.</returns>
    private string GetSelectedFolderPath(int side)
    {
        IList<Item> selectedItems = null;
        if (side == 0)
        {
            selectedItems = LeftListCollection.SelectedItems.Cast<Item>().ToList();
        }
        else if (side == 1)
        {
            selectedItems = RightListCollection.SelectedItems.Cast<Item>().ToList();
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

    /// <summary>
    /// Handles the item drop event asynchronously.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="items">The list of items being dropped.</param>
    /// <returns>A task representing the asynchronous operation.</returns>

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

        string action = await FileOverviewViewModel.SelectActionAsync();

        if (action != null && action != "Cancel" && action != "Move")
        {
            try
            {
                string numerOfThreads = "1";
                string regex = null;
                if(action.ToLower() != "copy")
                {
                    (string, string?) userInput = await FileOverviewViewModel.PromptUserAsync(action.ToLower(), needsRegex);
                    numerOfThreads = userInput.Item1;
                    regex = userInput.Item2;
                }
   
                if (numerOfThreads != null)
                {
                    if (int.TryParse(numerOfThreads, out int number) && number > 0 && number <= FileOverviewViewModel.MAX_THREADS)
                    {
                        if (LeftListCollection.SelectedItems.ToList().Count() >= 1)
                        {
                            await viewModel.ProcessActionAsync(action, number, regex, this, items, LeftListCollection.SelectedItems.ToList(), GetSelectedFolderPath(0));
                        }
                        if (RightListCollection.SelectedItems.ToList().Count() >= 1)
                        {
                            await viewModel.ProcessActionAsync(action, number, regex, this, items, RightListCollection.SelectedItems.ToList(), GetSelectedFolderPath(1));
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
            if (collectionView == RightListCollection)
            {
                targetPath = viewModel.RightSideViewModel.CurrentPath;
            }
            else if (collectionView == LeftListCollection)
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

    /// <summary>
    /// Toggles the roster view on and off.
    /// </summary>
    private void ToggleRosterView_Clicked()
    {
        isRosterView = !isRosterView;
        if (isRosterView)
        {
            LeftListCollection.IsEnabled = false;
            LeftListCollection.IsVisible = false;

            LeftRosterCollection.IsEnabled = true;
            LeftRosterCollection.IsVisible = true;

            RightListCollection.IsEnabled = false;
            RightListCollection.IsVisible = false;

            RightRosterCollection.IsEnabled = true;
            RightRosterCollection.IsVisible = true;
        }
        else
        {
            LeftListCollection.IsEnabled = true;
            LeftListCollection.IsVisible = true;

            LeftRosterCollection.IsEnabled = false;
            LeftRosterCollection.IsVisible = false;

            RightListCollection.IsEnabled = true;
            RightListCollection.IsVisible = true;

            RightRosterCollection.IsEnabled = false;
            RightRosterCollection.IsVisible = false;
        }
    }

    /// <summary>
    /// Gets the current view (CollectionView) based on the side and roster view state.
    /// </summary>
    /// <param name="side">The side to get the view from (0 for left, 1 for right).</param>
    /// <returns>The current CollectionView instance.</returns>

    private CollectionView getCurrentView(int side)
    {
        if (isRosterView)
        {
            if(side == 0)
            {
                return LeftRosterCollection;
            }
            else
            {
                return RightRosterCollection;
            }
        }
        if (side == 0)
        {
            return LeftListCollection;
        }
        else
        {
            return RightListCollection;
        }
    }
}

