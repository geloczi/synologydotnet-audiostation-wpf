using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SynAudio.ViewModels;

namespace SynAudio.DataTemplateSelectors
{
    public class MainContentTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object o, DependencyObject container)
        {
            if (o is TabContentViewModel tab)
            {
                // Custom data template mappings
                switch (tab.Action)
                {
                    case ActionType.NowPlaying: return FindTemplate("NowPlaying_SongViewModelCollectionTemplate");
                    case ActionType.BrowseByFolders: return FindTemplate("FolderViewModelCollectionTemplate");
                }

                // Automatically lookup the data template by collection element type
                if (tab.Content is ICollection)
                {
                    var itemsType = tab.Content.GetType();
                    var elementType = itemsType.HasElementType ? itemsType.GetElementType() : (itemsType.IsGenericType ? itemsType.GenericTypeArguments.First() : throw new NotSupportedException($"Cannot get collection element type out from '{itemsType.FullName}'"));
                    return FindTemplate($"{elementType.Name}CollectionTemplate");
                }
            }
            return null; //If the content not set yet
        }

        private DataTemplate FindTemplate(string name) => (DataTemplate)Application.Current.FindResource(name);
    }
}
