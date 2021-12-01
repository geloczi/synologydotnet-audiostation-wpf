using System;
using System.Windows;
using System.Windows.Media;

namespace SynAudio.Utils
{
    public static class MyVisualTreeHelper
    {
        public static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }

    public static class DependencyObjectExtension
    {
        public static T GetParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            Type type = typeof(T);
            if (element == null) return null;
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent == null && ((FrameworkElement)element).Parent is DependencyObject) parent = ((FrameworkElement)element).Parent;
            if (parent == null) return null;
            else if (parent.GetType() == type || parent.GetType().IsSubclassOf(type)) return parent as T;
            return GetParentOfType<T>(parent);
        }
    }
}
