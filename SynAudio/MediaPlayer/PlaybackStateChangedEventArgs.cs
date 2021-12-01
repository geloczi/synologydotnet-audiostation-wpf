using System;

namespace SynAudio.MediaPlayer
{
    public class PlaybackStateChangedEventArgs : EventArgs
    {
        public PlaybackStateType OldState { get; }
        public PlaybackStateType NewState { get; }
        public PlaybackStateChangedEventArgs(PlaybackStateType oldState, PlaybackStateType newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}
