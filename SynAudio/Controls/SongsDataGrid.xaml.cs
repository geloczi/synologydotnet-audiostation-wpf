using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SynAudio.Utils;
using SynAudio.ViewModels;

namespace SynAudio.Controls
{
    public partial class SongsDataGrid : UserControl
    {
        #region Dependency Properties
        public static DependencyProperty EnablePlayMenuProperty = DependencyProperty.Register(
                nameof(EnablePlayMenu),
                typeof(bool),
                typeof(SongsDataGrid));
        public bool EnablePlayMenu
        {
            get => (bool)GetValue(EnablePlayMenuProperty);
            set => SetValue(EnablePlayMenuProperty, value);
        }

        public static DependencyProperty ItemSourceIsReadOnlyProperty = DependencyProperty.Register(
                nameof(ItemSourceIsReadOnly),
                typeof(bool),
                typeof(SongsDataGrid),
                new UIPropertyMetadata(true));
        public bool ItemSourceIsReadOnly
        {
            get => (bool)GetValue(ItemSourceIsReadOnlyProperty);
            set => SetValue(ItemSourceIsReadOnlyProperty, value);
        }

        public static DependencyProperty HeadersVisibilityProperty = DependencyProperty.Register(
                nameof(HeadersVisibility),
                typeof(DataGridHeadersVisibility),
                typeof(SongsDataGrid),
                new UIPropertyMetadata(DataGridHeadersVisibility.Column));
        public DataGridHeadersVisibility HeadersVisibility
        {
            get => (DataGridHeadersVisibility)GetValue(HeadersVisibilityProperty);
            set => SetValue(HeadersVisibilityProperty, value);
        }

        public static DependencyProperty EnableDropProperty = DependencyProperty.Register(
                nameof(EnableDrop),
                typeof(bool),
                typeof(SongsDataGrid));
        public bool EnableDrop
        {
            get => (bool)GetValue(EnableDropProperty);
            set => SetValue(EnableDropProperty, value);
        }

        public static DependencyProperty EnableDragProperty = DependencyProperty.Register(
                nameof(EnableDrag),
                typeof(bool),
                typeof(SongsDataGrid));
        public bool EnableDrag
        {
            get => (bool)GetValue(EnableDragProperty);
            set => SetValue(EnableDragProperty, value);
        }

        public static DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(SongsDataGrid),
                new UIPropertyMetadata(true));
        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public static DependencyProperty CanUserSortColumnsProperty = DependencyProperty.Register(
                nameof(CanUserSortColumns),
                typeof(bool),
                typeof(SongsDataGrid),
                new UIPropertyMetadata(false));
        public bool CanUserSortColumns
        {
            get => (bool)GetValue(CanUserSortColumnsProperty);
            set => SetValue(CanUserSortColumnsProperty, value);
        }

        public static DependencyProperty ItemDoubleClickCommandProperty = DependencyProperty.Register(
                nameof(ItemDoubleClickCommand),
                typeof(ICommand),
                typeof(SongsDataGrid));
        public ICommand ItemDoubleClickCommand
        {
            get => (ICommand)GetValue(ItemDoubleClickCommandProperty);
            set => SetValue(ItemDoubleClickCommandProperty, value);
        }

        public static DependencyProperty ColumnsProperty = DependencyProperty.Register(
                nameof(Columns),
                typeof(System.Collections.ObjectModel.ObservableCollection<DataGridColumn>),
                typeof(SongsDataGrid));
        public System.Collections.ObjectModel.ObservableCollection<DataGridColumn> Columns
        {
            get => (System.Collections.ObjectModel.ObservableCollection<DataGridColumn>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }
        #endregion

        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private Point? _rowMouseLeftButtonDownPoint;
        private System.ComponentModel.ICollectionView _collectionView;

        public DataGrid DataGrid => grid1;

        public SongsDataGrid()
        {
            InitializeComponent();
            Columns = new System.Collections.ObjectModel.ObservableCollection<DataGridColumn>();
            Loaded += SongsDataGrid_Loaded;
        }

        private bool IsViewSorted() => _collectionView.SortDescriptions.Any();

        public void ScrollSongIntoView(SongViewModel item)
        {
            if (!(item is null))
                grid1.ScrollIntoView(item);
        }

        public void ScrollSongIntoView(string songId)
        {
            var item = GetItems().FirstOrDefault(x => x.Song.Id == songId);
            if (!(item is null))
                grid1.ScrollIntoView(item);
        }

        /// <summary>
        /// Gets the items exactly in the same order as on the UI.
        /// </summary>
        /// <returns></returns>
        private SongViewModel[] GetItems() => _collectionView.Cast<SongViewModel>().ToArray();

        private SongViewModel[] GetSelectedItems() => _collectionView.Cast<SongViewModel>().Where(x => x.IsSelected).ToArray();

        private void ClearSelection()
        {
            foreach (var item in GetSelectedItems())
                item.IsSelected = false;
        }

        private void SetSelectionOnAll(bool selected)
        {
            foreach (var item in GetItems().Where(x => x.IsSelected != selected))
                item.IsSelected = selected;
        }

        private void ClearSortDescriptions()
        {
            if (_collectionView.SortDescriptions.Any())
            {
                _collectionView.SortDescriptions.Clear();
                foreach (DataGridColumn column in grid1.Columns)
                    column.SortDirection = null;
            }
        }

        private static void SetGridFocusedCell(DataGrid grid, int rowIndex, int cellIndex)
        {
            grid.CurrentCell = new DataGridCellInfo(grid.Items[rowIndex], grid.Columns[cellIndex]); //CurrentCell must be set to properly restore input focus
            grid.SelectedIndex = rowIndex; //Selects the full row
        }

        /// <summary>
        /// Returns the unordered collection of selected items with indexes. 
        /// Important: The order depends on the user click direction! So order the returned array by Index as you need it.
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private (int Index, SongViewModel Item)[] GetIndexedSelectedItems()
        {
            var result = new List<(int Index, SongViewModel Item)>();
            var items = GetItems();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].IsSelected)
                    result.Add((i, items[i]));
            }
            return result.ToArray();
        }

        private void SongsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Unsubrscribe from the Loaded event, because it might be fired again, if you connect an external screen to the computer. I could reproduce this with my Hitachi TV.
            Loaded -= SongsDataGrid_Loaded;
            _collectionView = CollectionViewSource.GetDefaultView(grid1.ItemsSource);
            foreach (var col in Columns)
                grid1.Columns.Add(col);

            // Clear selection (the first row IsSelected is true for whatever reason...)
            foreach (var x in GetItems())
                x.IsSelected = false;

            if (!EnablePlayMenu)
                menuPlay.Visibility = Visibility.Collapsed;
            grid1.Focus();
        }

        private void Row_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            // Used to start Drag & Drop with mouse movement tolerance
            _rowMouseLeftButtonDownPoint = e.GetPosition(grid1);

            //If multiple items selected, swallow this event, so the selection stays for Drag&Drop.
            var row = (DataGridRow)sender;
            if (row.IsSelected && GetSelectedItems().Length > 1)
                e.Handled = true;
        }

        private void Row_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            var element = (FrameworkElement)e.OriginalSource;
            if (element.DataContext is SongViewModel songVm)
            {
                void SetSingleSelect()
                {
                    // Multi-Select -> Single-Select transition
                    ClearSelection();
                    songVm.IsSelected = true;
                }

                if (_rowMouseLeftButtonDownPoint.HasValue)
                {
                    var distance = Point.Subtract(e.GetPosition(grid1), _rowMouseLeftButtonDownPoint.Value).Length;
                    if (distance < 1)
                    {
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            // Toggle row selection
                            songVm.IsSelected = !songVm.IsSelected;
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.None) // Simple left click happened (no modifier keys)
                        {
                            SetSingleSelect();
                        }
                    }
                }
                else
                {
                    SetSingleSelect();
                }
            }
        }

        private void Row_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!EnableDrag || !_rowMouseLeftButtonDownPoint.HasValue)
                return;
            var distance = Point.Subtract(e.GetPosition(grid1), _rowMouseLeftButtonDownPoint.Value).Length;
            if (distance > 3 && _rowMouseLeftButtonDownPoint.HasValue && e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released && e.MiddleButton == MouseButtonState.Released)
            {
                var row = (DataGridRow)sender;
                if (row.IsSelected)
                {
                    var selection = GetSelectedItems();
                    var dragData = new DataObject(typeof(DragDropContainer), new DragDropContainer(this, DataContext, selection));
                    DragDrop.DoDragDrop(this, dragData, DragDropEffects.All);
                }
            }
        }

        private bool ValidateDrop(DragEventArgs e, out DragDropContainer container, out DragDropEffects effects)
        {
            container = null;
            effects = DragDropEffects.None;

            // Prevent drop if this instance is ReadOnly or the data is invalid type
            if (!EnableDrop || ItemSourceIsReadOnly || !e.Data.GetDataPresent(typeof(DragDropContainer)))
                return false;

            container = (DragDropContainer)e.Data.GetData(typeof(DragDropContainer));
            if (container.DataContext == _collectionView.SourceCollection && container.Source != this)
            {
                /* This is a special case, here is an example as an explanation. You open a NowPlaying tab, so you see two DtaGrid instances: the main view, 
				   and the NowPlaying panel on the right. The ItemSource points to the same ObservableCollection! If the user tries to drag a song from the 
					main view to the NowPlaying, it must be prevented to avoid data duplication or confusion. */
                return false;
            }
            else if (container.DataContext == DataContext)
            {
                // The drag source and drop target are the same. This will be handled as a re-ordering operation.
                effects = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? DragDropEffects.Move | DragDropEffects.Copy : DragDropEffects.Move;
            }
            else
            {
                // The drag source is different from the drop target, new items will be added.
                effects = DragDropEffects.Copy;
            }
            return true;
        }

        private void grid1_Drop(object sender, DragEventArgs e)
        {
            if (ValidateDrop(e, out var container, out var effects))
            {
                if (IsViewSorted())
                {
                    if (MessageBox.Show("Sorting has to be turned off before modifying items.\nWould you like to turn it off now?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        ClearSortDescriptions();
                }
                else
                {
                    e.Effects = effects;
                    var grid = (DataGrid)sender;
                    var songViewModels = (SongViewModel[])container.Data;
                    var collection = (IList<SongViewModel>)grid.DataContext;
                    var columnIndex = grid.CurrentCell.Column?.DisplayIndex ?? 0;

                    if (effects.HasFlag(DragDropEffects.Move))
                    {
                        // Pop items out of the colletion
                        var dataGridRow = VisualTreeHelper.HitTest(grid, e.GetPosition(grid)).VisualHit.GetParentOfType<DataGridRow>();
                        var insertRowIndex = dataGridRow is null ? -1 : dataGridRow.GetIndex();
                        var removalStartIndex = collection.IndexOf(songViewModels[0]);
                        var items = new List<SongViewModel>();
                        var copy = effects.HasFlag(DragDropEffects.Copy);
                        for (int i = 0; i < songViewModels.Length; i++)
                        {
                            var ci = collection.IndexOf(songViewModels[i]);
                            items.Add(copy ? new SongViewModel(collection[ci].Song) : collection[ci]);
                            if (!copy)
                                collection.RemoveAt(ci);
                        }

                        // Insert items
                        if (insertRowIndex > removalStartIndex)
                            insertRowIndex -= items.Count;
                        if (insertRowIndex >= 0)
                        {
                            for (int i = 0; i < items.Count; i++)
                                collection.Insert(insertRowIndex + i, items[i]);
                            SetGridFocusedCell(grid, insertRowIndex, columnIndex);
                        }
                        else
                        {
                            for (int i = 0; i < items.Count; i++)
                                collection.Add(items[i]);
                            SetGridFocusedCell(grid, grid.Items.Count - 1, columnIndex);
                        }
                    }
                    else if (effects.HasFlag(DragDropEffects.Copy))
                    {
                        foreach (var svm in songViewModels)
                            collection.Add(new SongViewModel(svm.Song));
                        SetGridFocusedCell(grid, grid.Items.Count - 1, columnIndex);
                    }
                    grid.ScrollIntoView(grid.SelectedItem);
                }
            }
            e.Handled = true;
        }

        private void grid1_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = DragDropEffects.None;
            if (ValidateDrop(e, out var container, out var effects))
            {
                e.Effects = effects;
            }
            e.Handled = true;
        }

        private void grid1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var grid = (DataGrid)sender;
                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    switch (e.Key)
                    {
                        case Key.Delete:
                            var itemsSource = grid.ItemsSource as ListCollectionView;
                            if (!ItemSourceIsReadOnly && itemsSource?.CanRemove == true && grid.SelectedIndex >= 0)
                            {
                                var selection = GetIndexedSelectedItems();
                                var rowIndex = selection.OrderBy(x => x.Index).First().Index;
                                var columnIndex = grid.CurrentCell.Column.DisplayIndex;
                                foreach (var x in selection)
                                    itemsSource.Remove(x.Item);
                                rowIndex = Math.Min(rowIndex, grid.Items.Count - 1);
                                if (rowIndex >= 0)
                                    SetGridFocusedCell(grid, rowIndex, columnIndex);
                                e.Handled = true;
                            }
                            break;

                        case Key.Escape:
                            SetSelectionOnAll(false);
                            ClearSortDescriptions();
                            e.Handled = true;
                            break;

                        case Key.Enter:
                            if (EnablePlayMenu && GetItems().Any(x => x.IsSelected))
                            {
                                StaticCommands.PlayNow.Execute(_collectionView);
                                SetSelectionOnAll(false);
                            }
                            e.Handled = true;
                            break;
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    switch (e.Key)
                    {
                        case Key.A:
                            SetSelectionOnAll(true);
                            e.Handled = true;
                            break;

                        case Key.C:
                            var items = GetItems();
                            if (StaticCommands.CopyToClipboard.CanExecute(items))
                                StaticCommands.CopyToClipboard.Execute(items);
                            e.Handled = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private void grid1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This custom implementation is an attempt to resolve the IsSelected property bug while IsVirtualizing = true
            _rowMouseLeftButtonDownPoint = e.GetPosition(grid1);
            var element = (FrameworkElement)e.OriginalSource;
            if (element.DataContext is SongViewModel songVm)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Toggle clicked item selection
                    songVm.IsSelected = !songVm.IsSelected;
                    grid1.Focus();
                    e.Handled = true;
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    // Select items between previously selected and currently clicked item
                    int selectionStart = -1;
                    int selectionEnd = -1;
                    SongViewModel item;
                    var items = GetItems();
                    for (int i = 0; i < items.Length && (selectionStart == -1 || selectionEnd == -1); i++)
                    {
                        item = items[i];
                        if (item == songVm)
                            selectionEnd = i;
                        else if (item.IsSelected)
                            selectionStart = i;
                    }
                    if (selectionStart == -1)
                        selectionStart = 0; // If this is the very first click on the grid, we assume that the first item is the starting point

                    if (selectionEnd < selectionStart)
                    {
                        var x = selectionStart;
                        selectionStart = selectionEnd;
                        selectionEnd = x;
                    }

                    for (int i = selectionStart; i < selectionEnd; i++)
                        items[i].IsSelected = true;

                    songVm.IsSelected = true;
                    e.Handled = true;
                    grid1.Focus();
                }
            }
        }

        private void grid1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && ItemDoubleClickCommand?.CanExecute(_collectionView) == true)
            {
                ItemDoubleClickCommand.Execute(_collectionView);
            }
        }

    }
}
