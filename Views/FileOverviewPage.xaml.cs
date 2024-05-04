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
        BindingContext = new FileOverviewViewModel();
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

        System.Diagnostics.Debug.WriteLine("meep");

        if (collectionView != null)
        {
            System.Diagnostics.Debug.WriteLine("bleep");

            // Use the first selected item from the list of selected items
            var item = selectedItems.FirstOrDefault();
            var viewModel = BindingContext as FileOverviewViewModel;
            var droppedFiles = viewModel.DroppedFiles;

            System.Diagnostics.Debug.WriteLine("hallo " + item);

            System.Diagnostics.Debug.WriteLine(droppedFiles);

            if (droppedFiles != null && item != null)
            {

                var targetPath = GetTargetPath(item, collectionView);
                


                foreach (var file in droppedFiles)
                {
                    System.Diagnostics.Debug.WriteLine("bloop" + targetPath);
                    System.Diagnostics.Debug.WriteLine("HIER" + file);
                    MoveFile(file, targetPath);
                }
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
        // Move the file from its current FilePath to the new targetPath
        var newFilePath = Path.Combine(targetPath, file.FileName);
        System.Diagnostics.Debug.WriteLine("meep " + file.FileName);

        if (File.Exists(file.FilePath))
        {
            File.Move(file.FilePath, newFilePath);
        }
        else if (Directory.Exists(file.FilePath))
        {
            // Move the directory to the new location
        }

        // Update the FilePath property of the Item object with the new path
        file.FilePath = newFilePath;
    }

    private string GetTargetPath(object dropPointObj, CollectionView targetCollection)
    {
        if (dropPointObj is Item targetItem)
        {
            // If the drop point is an item (file or folder)
            if (targetItem.Type == ItemType.File)
            {
                // If it's a file, use the directory path of the file
                return Path.GetDirectoryName(targetItem.FilePath);
            }
            else
            {
                // If it's a folder, use the folder path
                return targetItem.FilePath;
            }
        }
        else
        {
            // If the drop point is not an item, use the current path of the corresponding side
            var viewModel = targetCollection.BindingContext as FileOverviewViewModel;
            if (targetCollection == LeftCollection)
            {
                return viewModel?.LeftSideViewModel.CurrentPath;
            }
            else if (targetCollection == RightCollection)
            {
                return viewModel?.RightSideViewModel.CurrentPath;
            }
        }

        // If all else fails, return a default path (e.g., the user's desktop)
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
}