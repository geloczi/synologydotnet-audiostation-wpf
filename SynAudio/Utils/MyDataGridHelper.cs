using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SynAudio.Utils
{
    public class MyDataGridHelper : DependencyObject
    {
        private static readonly DependencyProperty TextColumnStyleProperty = DependencyProperty.RegisterAttached("TextColumnStyle", typeof(Style), typeof(MyDataGridHelper), new PropertyMetadata
        {
            PropertyChangedCallback = (obj, e) =>
            {
                var grid = (DataGrid)obj;
                if (e.OldValue == null && e.NewValue != null)
                    grid.Columns.CollectionChanged += (obj2, e2) =>
                    {
                        UpdateColumnStyles(grid);
                    };
            }
        });

        public static void SetTextColumnStyle(DependencyObject element, Style value)
        {
            element.SetValue(TextColumnStyleProperty, value);
        }
        public static Style GetTextColumnStyle(DependencyObject element)
        {
            return (Style)element.GetValue(TextColumnStyleProperty);
        }

        private static void UpdateColumnStyles(DataGrid grid)
        {
            var origStyle = GetTextColumnStyle(grid);
            foreach (var column in grid.Columns.OfType<DataGridTextColumn>())
            {
                //may not add setters to a style which is already in use
                //therefore we need to create a new style merging
                //original style with setters from attached property
                var newStyle = new Style();
                newStyle.BasedOn = column.ElementStyle;
                newStyle.TargetType = origStyle.TargetType;
                foreach (var setter in origStyle.Setters.OfType<Setter>())
                    newStyle.Setters.Add(setter);
                column.ElementStyle = newStyle;
            }
        }
    }
}
