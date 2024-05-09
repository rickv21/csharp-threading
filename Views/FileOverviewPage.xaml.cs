using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManager.Models;
using FileManager.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.StartScreen;

namespace FileManager.Views;

public partial class FileOverviewPage : ContentPage
{
    private FileOverviewViewModel viewModel;

    public FileOverviewPage()
    {
        InitializeComponent();
        viewModel = new FileOverviewViewModel();
        BindingContext = viewModel;
    }


    void OnItemTapped(object sender, EventArgs e)
    {
        var item = ((sender as Grid).BindingContext as Item);

        if (item.Type == ItemType.Drive || item.Type == ItemType.TopDir)
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

    void AddLeftTab(object sender, EventArgs e)
    {
        viewModel.AddTab(0);
    }

    void RemoveLeftTab(object sender, EventArgs e)
    {
        viewModel.RemoveTab(0);
    }

    void AddRightTab(object sender, EventArgs e)
    {
        viewModel.AddTab(1);
    }

    void RemoveRightTab(object sender, EventArgs e)
    {
        viewModel.RemoveTab(1);
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
                if (int.TryParse(numnerOfThreads, out int number) && number > 0 && number <= 7)
                {
                    await viewModel.ProcessActionAsync(action, number, regex);
                }
                else if (number > 7)
                {
                    await DisplayAlert("Error", "Number of threads cannot exceed 7.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Invalid input or non-positive number entered.", "OK");
                }
            }
        }
    }
}

