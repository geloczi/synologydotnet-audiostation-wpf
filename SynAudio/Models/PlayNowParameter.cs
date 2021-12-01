using System;

namespace SynAudio.Models
{
    class PlayNowParameter
    {
        public object StartPlaybackFrom { get; set; }
        public object[] ItemsToNowPlay { get; set; }
        public Type ElementType { get; set; }

        public PlayNowParameter(Type elementType, object[] itemsToNowPlay, object startPlaybackFrom)
        {
            ElementType = elementType;
            ItemsToNowPlay = itemsToNowPlay;
            StartPlaybackFrom = startPlaybackFrom;
        }
    }
}
