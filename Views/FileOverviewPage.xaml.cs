using System;
using System.IO;
using FileManager.Models;
using FileManager.ViewModels;
using FileManager.Extensions;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Media;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{
    private List<Item> selectedItems = new List<Item>();

    public FileOverviewPage()
    {
        InitializeComponent();
        //BindingContext = new FileOverviewViewModel();

        var viewModel = new FileOverviewViewModel(LeftCollection, RightCollection);
        BindingContext = viewModel;
    }

    void OnItemTapped(object sender, EventArgs e)
    {
        var item = ((sender as Grid)?.BindingContext as Item);

        if (item?.Type == ItemType.Drive || item?.Type == ItemType.TopDir)
        {
            return;
        }

        var viewModel = BindingContext as FileOverviewViewModel;

        if (item.Side == 0)
        {
            // Left side
            viewModel.ToggleItemSelection(item, LeftCollection);
        }
        else if (item.Side == 1)
        {
            // Right side
            viewModel.ToggleItemSelection(item, RightCollection);
        }

        // Print the name of the selected item to the console
        System.Diagnostics.Debug.WriteLine("Selected item: " + item.FileName);

        // Update the list of selected items
        if (selectedItems.Contains(item))
        {
            selectedItems.Remove(item);
        }
        else
        {
            selectedItems.Add(item);
        }
    }

    void OnDragStarting(object sender, DragStartingEventArgs e)
    {
        var dragGestureRecognizer = (DragGestureRecognizer)sender;
        var grid = (Grid)dragGestureRecognizer.Parent;
        var item = (Item)grid.BindingContext;

        if (item?.Type == ItemType.Drive || item?.Type == ItemType.TopDir)
        {
            e.Cancel = true;
            return;
        }

        var viewModel = BindingContext as FileOverviewViewModel;

        // Update the DropPointObj with the current item
        viewModel.DropPointObj = item;

        if (item.Side == 0)
        {
            // Left side
            viewModel.DroppedFiles = LeftCollection.SelectedItems.Cast<Item>();
        }
        else if (item.Side == 1)
        {
            // Right side
            viewModel.DroppedFiles = RightCollection.SelectedItems.Cast<Item>();
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

    private string GetTargetPath(object dropPointObj, CollectionView sourceCollection, CollectionView targetCollection)
    {
        if (dropPointObj is Item targetItem)
        {
            // If the drop point is an item (file or folder)
            if (targetItem.Type == ItemType.File)
            {
                // If it's a file, use the directory path of the file
                return Path.GetDirectoryName(targetItem.FilePath);
            }
        }
        else
        {
            // If the drop point is not an item, use the current path of the target collection
            var viewModel = targetCollection.BindingContext as FileOverviewViewModel;
            return viewModel?.GetCurrentPath(targetCollection);
        }

        // If all else fails, return a default path (e.g., the user's desktop)
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
}