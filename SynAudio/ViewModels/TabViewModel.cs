using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SynAudio.Utils;
using Utils;
using Utils.Commands;

namespace SynAudio.ViewModels
{
    public class TabViewModel : ViewModelBase
    {
        public event EventHandler<NavigationItem> NavigationRequest;

        public TabItem TabItem { get; }
        public TabContentViewModel Content { get; set; }
        public RangeObservableCollection<NavigationItem> NavigationItems { get; } = new RangeObservableCollection<NavigationItem>();
        public ICommand NavigateCommand { get; set; }
        public NavigationItem CurrentNavigationItem => NavigationItems.LastOrDefault();
        public TabViewModel()
        {
            TabItem = new TabItem() { Header = string.Empty, Content = this };
            NavigateCommand = new RelayCommand(NavigateCommand_Action);
        }

        private void NavigateCommand_Action(object o)
        {
            if (o is NavigationItem navigationItem)
            {
                if (NavigationItems.Last() != navigationItem)
                    NavigationRequest.Fire(this, navigationItem);
            }
            else
                throw new ArgumentException($"{nameof(o)} is not {nameof(NavigationItem)}");
        }

        public void Navigate(object content, NavigationItem navigationItem, bool resetNavigation = false)
        {
            if (!(CurrentNavigationItem is null) && !(Content is null))
            {
                CurrentNavigationItem.SelectedItem = Content.SelectedItem;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (resetNavigation)
                    NavigationItems.Clear();
                if (!(navigationItem is null))
                {
                    // Check if we are navigating backwards
                    var currentItemIndex = NavigationItems.IndexOf(navigationItem);
                    if (currentItemIndex >= 0)
                    {
                        // Remove items after this one
                        for (int i = NavigationItems.Count - 1; i > currentItemIndex; i--)
                            NavigationItems.RemoveAt(i);
                    }
                    else
                    {
                        // Add new item
                        if (NavigationItems.Count > 0)
                            NavigationItems.Add(new NavigationItem(true) { Title = ">" });
                        NavigationItems.Add(navigationItem);
                    }
                }
                Content = new TabContentViewModel()
                {
                    Action = navigationItem.Action,
                    Content = content,
                    SelectedItem = navigationItem.SelectedItem
                };
                TabItem.Header = navigationItem;
            });
        }
    }
}
