﻿using System;
using System.Collections.Generic;
using System.Linq;
using PropertyChanged;
using SynAudio.DAL;
using SynAudio.Utils;

namespace SynAudio.ViewModels
{
    public class NowPlayingViewModel : ViewModelBase
    {
        private static Random Rnd => App.Rnd;

        public delegate void NowPlayingEvent(NowPlayingViewModel sender);

        public event NowPlayingEvent CurrentSongChanged;

        private readonly object _syncRoot = new object();
        private readonly List<SongViewModel> _originalSongList = new List<SongViewModel>();
        private bool _shuffle = false;

        public RangeObservableCollection<SongViewModel> Songs { get; } = new RangeObservableCollection<SongViewModel>();

        [AlsoNotifyFor(nameof(CurrentSong))]
        public SongViewModel CurrentSongVM { get; private set; }

        public SongModel CurrentSong => CurrentSongVM?.Song;

        public bool Repeat { get; set; }

        public string Cover { get; set; }

        public bool Shuffle
        {
            get => _shuffle;
            set
            {
                lock (_syncRoot)
                {
                    if (value != _shuffle)
                    {
                        _shuffle = value;
                        if (_shuffle)
                        {
                            // Mix songs
                            _originalSongList.Clear();
                            _originalSongList.AddRange(Songs);
                            Songs.Clear();
                            Songs.AddRange(_originalSongList.OrderBy(x => Rnd.Next()));
                        }
                        else if (!(_originalSongList is null))
                        {
                            // Revert original order
                            if (Songs.Count != _originalSongList.Count) // Transfer the changes made on the shuffled items
                            {
                                var merged = _originalSongList.Where(x => Songs.Contains(x)).ToList(); // Removed songs
                                merged.AddRange(Songs.Where(x => !_originalSongList.Contains(x)).ToArray());
                                _originalSongList.Clear();
                                _originalSongList.AddRange(merged);
                            }
                            Songs.Clear();
                            Songs.AddRange(_originalSongList);
                            _originalSongList.Clear();
                        }
                    }
                }
            }
        }

        public NowPlayingViewModel() { }

        public void Clear()
        {
            lock (_syncRoot)
            {
                SetCurrentSong(null);
                foreach (var vm in Songs)
                    if (vm.IsPlaying)
                        vm.IsPlaying = false;
                Songs.Clear();
                _originalSongList.Clear();
                Shuffle = false;
            }
        }

        //public void Add(SongModel song)
        //{
        //	lock (_syncRoot)
        //	{
        //		Songs.Add(new SongViewModel(song));
        //	}
        //}

        public void Add(IEnumerable<SongModel> songs)
        {
            lock (_syncRoot)
            {
                Songs.AddRange(songs.Select(x => new SongViewModel(x)));
            }
        }

        public void SetCurrentSong(SongModel song)
        {
            lock (_syncRoot)
            {
                if (!(CurrentSongVM is null))
                    CurrentSongVM.IsPlaying = false;
                if (!(song is null))
                {
                    var vm = Songs.FirstOrDefault(x => x.Song == song) ?? Songs.FirstOrDefault(x => x.Song.Id == song.Id);
                    if (!(vm is null))
                    {
                        CurrentSongVM = vm;
                        CurrentSongVM.IsPlaying = true;
                    }
                }
            }
            RaiseCurrentSongChanged();
        }

        public void SetCurrentSongViewModel(SongViewModel songVM)
        {
            lock (_syncRoot)
            {
                if (!(CurrentSongVM is null))
                    CurrentSongVM.IsPlaying = false;
                if (!(songVM is null))
                {
                    var vm = Songs.Contains(songVM) ? songVM : Songs.FirstOrDefault(x => x.Song.Id == songVM.Song.Id);
                    if (!(vm is null))
                    {
                        CurrentSongVM = vm;
                        CurrentSongVM.IsPlaying = true;
                    }
                }
            }
            RaiseCurrentSongChanged();
        }

        /// <summary>
        /// Jump to the previous song
        /// </summary>
        /// <returns></returns>
        public SongModel Previous()
        {
            lock (_syncRoot)
            {
                SongModel song = null;
                if (Songs.Count > 0)
                {
                    var position = CurrentSongVM is null ? -1 : Songs.IndexOf(Songs.FirstOrDefault(x => x.Song == CurrentSongVM.Song)) - 1;
                    if (position < 0)
                        song = Repeat ? Songs[Songs.Count - 1].Song : null; //If repeat is enabled, jump to the last song. Otherwise there's no previous song.
                    else
                        song = Songs[position].Song;
                }
                if (!(song is null))
                    SetCurrentSong(song);
                return song;
            }
        }

        /// <summary>
        /// Jump to the next song
        /// </summary>
        /// <returns></returns>
        public SongModel Next()
        {
            lock (_syncRoot)
            {
                SongModel song = null;
                if (Songs.Count > 0)
                {
                    var position = CurrentSongVM is null ? 0 : Songs.IndexOf(Songs.FirstOrDefault(x => x.Song == CurrentSongVM.Song)) + 1;
                    if (position >= Songs.Count)
                        song = Repeat ? Songs[0].Song : null; //If repeat is enabled, jump to the first song. Otherwise this is the end of the playlist.
                    else
                        song = Songs[position].Song;
                }
                if (!(song is null))
                    SetCurrentSong(song);
                return song;
            }
        }

        public void LoadState(out TimeSpan playbackPosition)
        {
            lock (_syncRoot)
            {
                Songs.Clear();
                _originalSongList.Clear();

                // Load settings from database
                var currentSongId = App.DbSettings.ReadString(StringValues.NowPlaying_CurrentSongId);
                Repeat = App.DbSettings.ReadInt64(Int64Values.NowPlaying_Repeat) > 0;
                _shuffle = App.DbSettings.ReadInt64(Int64Values.NowPlaying_Shuffle) > 0;

                // Get songs for the playlist
                var playlistSongs = (from s in App.Db.Table<SongModel>()
                                     join npi in App.Db.Table<NowPlayingItem>() on s.Id equals npi.SongId
                                     select s).ToArray();

                var playlistItems = App.Db.Table<NowPlayingItem>().ToArray()
                    .Where(npi => playlistSongs.Any(s => s.Id == npi.SongId))
                    .ToArray();

                if (playlistSongs.Length > 0)
                {
                    Songs.AddRange(playlistSongs.Select(x => new SongViewModel(x)));

                    // Shuffle restore
                    if (playlistItems[0].OriginalPosition >= 0)
                    {
                        _originalSongList.AddRange(playlistItems.OrderBy(x => x.OriginalPosition).Select(x => Songs.First(song => song.Song.Id == x.SongId)));
                    }
                }

                playbackPosition = TimeSpan.FromTicks(App.DbSettings.ReadInt64(Int64Values.Playback_Position) ?? 0);

                SetCurrentSongViewModel(Songs.FirstOrDefault(x => x.Song.Id == currentSongId));
            }
            OnPropertyChanged(nameof(Shuffle));
        }

        public void SaveState(TimeSpan playbackPosition)
        {
            lock (_syncRoot)
            {
                App.Db.RunInTransaction(() =>
                {
                    App.DbSettings.WriteInt64(Int64Values.NowPlaying_Repeat, Repeat ? 1 : 0);
                    App.DbSettings.WriteInt64(Int64Values.NowPlaying_Shuffle, Shuffle ? 1 : 0);
                    App.DbSettings.WriteString(StringValues.NowPlaying_CurrentSongId, CurrentSong?.Id);
                    App.Db.DeleteAll<NowPlayingItem>();
                    App.Db.InsertAll(Songs.Select((v, i) => new NowPlayingItem()
                    {
                        Position = i,
                        SongId = v.Song.Id,
                        OriginalPosition = _originalSongList.Count > 0 ? _originalSongList.IndexOf(v) : -1
                    }).ToArray());
                    App.DbSettings.WriteInt64(Int64Values.Playback_Position, playbackPosition.Ticks);
                });
            }
        }

        private void RaiseCurrentSongChanged() => CurrentSongChanged?.Invoke(this);
    }
}
