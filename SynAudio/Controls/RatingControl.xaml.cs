using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SynAudio.Controls
{
    /// <summary>
    /// Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl
    {
        public event EventHandler ValueChanged;

        public ToggleButton[] Buttons { get; }

        public RatingControl()
        {
            InitializeComponent();
            Buttons = new[] { tb1, tb2, tb3, tb4, tb5 };
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(RatingControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(RatingChanged)));
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set
            {
                if (value < 0)
                    SetValue(ValueProperty, 0);
                else if (value > Buttons.Length)
                    SetValue(ValueProperty, Buttons.Length);
                else
                    SetValue(ValueProperty, value);
                ValueChanged?.Invoke(this, new EventArgs());
            }
        }

        private static void RatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            int newValue = (int)e.NewValue;
            var buttons = ((RatingControl)sender).Buttons;
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].IsChecked = i < newValue;
        }

        private void ClickEventHandler(object sender, RoutedEventArgs args)
        {
            int newValue = Array.IndexOf(Buttons, (ToggleButton)sender) + 1;
            Value = newValue == Value ? 0 : newValue;
        }
    }

}
