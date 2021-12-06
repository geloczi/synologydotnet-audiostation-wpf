using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using SqlCeLibrary;
using SynAudio.DAL;
using SynAudio.DAL.Models;
using SynAudio.Library;
using SynAudio.Library.Exceptions;
using SynAudio.MediaPlayer;
using SynAudio.Models;
using SynAudio.Models.Config;
using SynAudio.Utils;
using SynAudio.Views;
using SynologyDotNet.AudioStation;
using Utils;
using Utils.Commands;

namespace SynAudio.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        #region [Events]
        public delegate void MainWindowViewModelEvent(MainWindowViewModel sender);

        public event MainWindowViewModelEvent PlaybackStarted, PlaybackStopped;
        #endregion

        #region [Fields]
        private int _volumeBeforeMuted = 100;
        private readonly object _syncRoot = new object();
        private readonly HashSet<string> _quickSyncedArtists = new HashSet<string>();
        private TimeSpan _restoreSongPosition;
        #endregion

        #region [Properties]

        public SettingsModel Settings { get; }
        public ErrorDialogErrorHandler ErrorHandler { get; } = new ErrorDialogErrorHandler();
        public bool Connected { get; private set; }
        public TranscodeMode[] TranscodeModes { get; } = ((TranscodeMode[])Enum.GetValues(typeof(TranscodeMode))).Where(x => x != TranscodeMode.None).ToArray();
        public RangeObservableCollection<TabItem> Tabs { get; } = new RangeObservableCollection<TabItem>();
        public TabItem CurrentTabItem { get; set; }
        public MusicPlayer Player { get; private set; }
        public TabViewModel CurrentTabVM => CurrentTabItem?.Content as TabViewModel;
        public AudioLibrary Library { get; private set; }
        public NowPlayingViewModel NowPlaying { get; } = new NowPlayingViewModel();
        public int PlayerVolume
        {
            get => Settings.Volume;
            set
            {
                if (value != Settings.Volume)
                {
                    Settings.Volume = value;
                    Player.Volume = PercentageToRatio(PlayerVolume);
                }
            }
        }

        public bool IsLibraryUpdating { get; set; }
        public bool LibraryLoaded { get; set; }
        public StatusViewModel Status { get; } = new StatusViewModel();
        public bool IsNowPlayingVisible { get; set; } = true;
        #endregion

        #region [Commands]
        public ICommand Player_PlayCommand { get; }
        public ICommand Player_PauseCommand { get; }
        public ICommand Player_StopCommand { get; }
        public ICommand Player_PlayNextCommand { get; }
        public ICommand Player_PlayPrevCommand { get; }
        public ICommand Player_ShuffleCommand { get; }
        public ICommand Player_RepeatCommand { get; }
        public ICommand Player_MuteCommand { get; }
        public ICommand SynchronizeLibraryCommand { get; set; }
        public ICommand Navigation_PlayNowCommand { get; set; }
        public ICommand BrowseLibraryItemCommand { get; set; }
        public ICommand PlayNowCommand { get; set; }
        public ICommand BrowseByArtistsCommand { get; set; }
        public ICommand BrowseByFoldersCommand { get; set; }
        public ICommand OpenNewTabCommand { get; set; }
        public ICommand CloseTabCommand { get; set; }
        public ICommand OpenNowPlayingCommand { get; set; }
        public ICommand OpenSettingsCommand { get; set; }
        public ICommand NowPlaying_ChangeCurrentSongCommand { get; set; }
        public ICommand OpenContainingFolderCommand { get; set; }
        public ICommand DeleteSelectedSongsFromLibraryCommand { get; set; }
        public ICommand CopyToClipboardCommand { get; set; }
        #endregion

        #region [Public Methods]
        public MainWindowViewModel(SettingsModel settings)
        {
            Settings = settings;

            Player_PlayCommand = new RelayCommand(PlayCommand_Action, o => Connected && !(NowPlaying.CurrentSongVM is null) && Player?.PlaybackState != PlaybackStateType.Playing);
            Player_PauseCommand = new RelayCommand(Player_PauseCommand_Action, o => Player?.IsPlaying == true);
            Player_StopCommand = new RelayCommand(o => Player?.Stop());
            Player_PlayNextCommand = new RelayCommand(PlayNextCommand_Action);
            Player_PlayPrevCommand = new RelayCommand(PlayPrevCommand_Action);
            Player_ShuffleCommand = new RelayCommand(Player_ShuffleCommand_Action);
            Player_RepeatCommand = new RelayCommand(Player_RepeatCommand_Action);
            Player_MuteCommand = new RelayCommand(Player_MuteCommand_Action);
            SynchronizeLibraryCommand = new RelayCommand(SynchronizeLibraryCommand_Action, o => Connected);
            OpenNewTabCommand = new RelayCommand(OpenNewTabCommand_Action, o => Connected);
            CloseTabCommand = new RelayCommand(CloseTabCommand_Action);
            OpenNowPlayingCommand = new RelayCommand(OpenNowPlayingCommand_Action);
            OpenSettingsCommand = new RelayCommand(OpenSettingsCommand_Action);

            // StaticCommands
            PlayNowCommand = new RelayCommand(PlayNowCommand_Action, o => Connected);
            BrowseLibraryItemCommand = new RelayCommand(BrowseLibraryItemCommand_Action, o => Connected);
            BrowseByArtistsCommand = new RelayCommand(BrowseByArtistsCommand_Action, o => Connected);
            BrowseByFoldersCommand = new RelayCommand(BrowseByFoldersCommand_Action, o => Connected);
            NowPlaying_ChangeCurrentSongCommand = new RelayCommand(o => PlaySong(((SongViewModel)((CollectionView)o).CurrentItem)));
            OpenContainingFolderCommand = new RelayCommand(OpenContainingFolderCommand_Action, o => App.MusicFolderAvailableOnLan);
            DeleteSelectedSongsFromLibraryCommand = new RelayCommand(DeleteSelectedSongsFromLibraryCommand_Action, o => App.MusicFolderAvailableOnLan);
            CopyToClipboardCommand = new RelayCommand(CopyToClipboardCommand_Action, o => App.MusicFolderAvailableOnLan);
        }

        private void CopyToClipboardCommand_Action(object o)
        {
            if (o is IEnumerable<SongViewModel> songs)
            {
                if (App.MusicFolderAvailableOnLan)
                {
                    var fileList = new List<string>();
                    foreach (var item in songs.Where(x => x.IsSelected))
                        if (App.ExistsOnHost(item.Song.Path, out var uncPath))
                            fileList.Add(uncPath);
                    if (fileList.Any())
                    {
                        var filesStringCollection = new System.Collections.Specialized.StringCollection();
                        filesStringCollection.AddRange(fileList.ToArray());
                        Clipboard.Clear();
                        Clipboard.SetFileDropList(filesStringCollection);
                    }
                    else
                        MessageBox.Show("Files are not available on the local network!", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
                else
                    MessageBox.Show("The server is not available on the local network!", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private void OpenContainingFolderCommand_Action(object o)
        {
            try
            {
                var songVm = ((System.Collections.IList)o).Cast<SongViewModel>().FirstOrDefault();
                if (!(songVm is null) && TryGetSongFileFromNetworkPath(songVm.Song, out var songPath))
                    WindowsHelper.HighlightFileInFileExplorer(songPath);
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private void DeleteSelectedSongsFromLibraryCommand_Action(object o)
        {
            try
            {
                var songs = (IList<SongViewModel>)o;
                var songsToDelete = songs.Where(x => x.IsSelected).ToArray();
                var errors = new List<SongModel>();
                var successfullyDeletedSongFile = new List<SongModel>();

                if (MessageBox.Show(Application.Current.MainWindow, $"{songsToDelete.Length} song files will be deleted.\nAre you sure?", "Delete songs", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                foreach (var s in songsToDelete)
                {
                    try
                    {
                        if (TryGetSongFileFromNetworkPath(s.Song, out var songPath))
                        {
                            File.Delete(songPath);
                            songs.Remove(s);
                            successfullyDeletedSongFile.Add(s.Song);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                        errors.Add(s.Song);
                    }
                }
                if (successfullyDeletedSongFile.Count > 0)
                    Library.DeleteSongsFromCache(successfullyDeletedSongFile);
                if (errors.Count > 0)
                    MessageBox.Show(Application.Current.MainWindow, $"{errors.Count} songs out of {songsToDelete.Length} could not be deleted.", "Song delete error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private void OpenSettingsCommand_Action(object o)
        {
            var d = new SettingsDialog(new SettingsDialogModel(this))
            {
                Owner = Application.Current.MainWindow
            };
            d.ShowDialog();
        }

        private RangeObservableCollection<T> ToObservableCollection<T>(IEnumerable<T> items) => !(items is null) ? new RangeObservableCollection<T>(items) : null;

        private void OpenNowPlayingCommand_Action(object o)
        {
            lock (_syncRoot)
            {
                FocusExistingOrCreateNewTab(false, new NavigationItem(ActionType.NowPlaying, "Now playing", null), NavigationNodePosition.Current,
                    () => NowPlaying.Songs);
            }
        }

        private void BrowseByArtistsCommand_Action(object o)
        {
            lock (_syncRoot)
            {
                FocusExistingOrCreateNewTab(Keyboard.Modifiers == ModifierKeys.Control, new NavigationItem(ActionType.BrowseByArtists, "Artists", null), NavigationNodePosition.Root,
                    () => ToObservableCollection(Library.GetArtists()));
            }
        }

        private RangeObservableCollection<ViewModelBase> FolderContentsModelToObservable(FolderContentsModel m)
        {
            var result = new RangeObservableCollection<ViewModelBase>();
            result.AddRange(m.Folders.Select(x => new FolderViewModel(x)));
            result.AddRange(m.Songs.Select(x => new SongViewModel(x)));
            return result;
        }

        private void BrowseByFoldersCommand_Action(object o)
        {
            lock (_syncRoot)
            {
                FocusExistingOrCreateNewTab(Keyboard.Modifiers == ModifierKeys.Control, new NavigationItem(ActionType.BrowseByFolders, "Folders", null), NavigationNodePosition.Root,
                () => FolderContentsModelToObservable(Library.GetFolderContents(string.Empty)));
            }
        }

        private IEnumerable<TabViewModel> FindTabs(Func<TabViewModel, bool> predicate) => Tabs.Select(x => (TabViewModel)x.Content).Where(predicate);

        private void FocusExistingOrCreateNewTab(bool forceNew, NavigationItem navigation, NavigationNodePosition searchPosition, Func<object> itemsProvider)
        {
            NavigationItem GetItemFromTab(TabViewModel tvm)
            {
                return searchPosition switch
                {
                    NavigationNodePosition.Current => tvm.CurrentNavigationItem,
                    NavigationNodePosition.Root => tvm.NavigationItems.FirstOrDefault(),
                    _ => throw new NotImplementedException(),
                };
            }

            lock (_syncRoot)
            {
                // Try to find an opened tab with the desired navigation target
                var t = FindTabs(x => GetItemFromTab(x).Action == navigation.Action && GetItemFromTab(x).EntityId == navigation.EntityId).FirstOrDefault();
                if (t is null || forceNew)
                {
                    // New tab has to be opened
                    OpenNewTab().Navigate(itemsProvider(), navigation);
                }
                else
                {
                    // Tab found, so focus it
                    CurrentTabItem = t.TabItem;
                    if (CurrentTabVM.CurrentNavigationItem.Action != navigation.Action)
                    {
                        // The found tab contains a navigation item what matches the target, so reset it to that
                        // The goal here is to re-use existing tab instances when possible, so if the user starts clicking the buttons like crazy, the app won't create a lot of tabs.
                        CurrentTabVM.Navigate(itemsProvider(), navigation, true);
                    }
                }
            }
        }

        private void OpenNewTabCommand_Action(object o)
        {
            lock (_syncRoot)
            {
                var t = OpenNewTab();
                t.Navigate(ToObservableCollection(Library.GetArtists()), new NavigationItem(ActionType.BrowseByArtists, "Artists", null));
            }
        }

        private void CloseTabCommand_Action(object o)
        {
            if (o is NavigationItem ni)
            {
                var t = Tabs.First(x => x.Header == ni);
                var vm = (TabViewModel)t.Content;
                CloseTab(vm);
            }
        }

        private TabViewModel OpenNewTab()
        {
            var vm = new TabViewModel();
            vm.NavigationRequest += Tab_NavigationRequest;
            Tabs.Add(vm.TabItem);
            CurrentTabItem = vm.TabItem;
            return vm;
        }

        private void CloseTab(TabViewModel vm)
        {
            vm.NavigationRequest -= Tab_NavigationRequest;
            Tabs.Remove(vm.TabItem);
        }

        private void Tab_NavigationRequest(object sender, NavigationItem e)
        {
            if (e.IsSeparator)
                return;
            if (!(sender is TabViewModel tvm))
                throw new ArgumentException(nameof(sender));

            lock (_syncRoot)
            {
                switch (e.Action)
                {
                    case ActionType.BrowseByArtists:
                        tvm.Navigate(ToObservableCollection(Library.GetArtists()), e);
                        break;
                    case ActionType.BrowseByFolders:
                        tvm.Navigate(FolderContentsModelToObservable(Library.GetFolderContents(e.EntityId)), e);
                        break;
                    case ActionType.OpenArtistAlbums:
                        tvm.Navigate(ToObservableCollection(Library.GetAlbums(e.EntityId)), e);
                        break;
                    case ActionType.OpenArtistSongs:
                        tvm.Navigate(ToObservableCollection(Library.GetSongs(e.EntityId, 0).Select(x => new SongViewModel(x))), e);
                        break;
                    case ActionType.OpenAlbumSongs:
                        tvm.Navigate(ToObservableCollection(Library.GetSongs(null, int.Parse(e.EntityId)).Select(x => new SongViewModel(x))), e);
                        break;

                    default: throw new NotImplementedException(e.Action.ToString());
                }
            }
        }

        private void BrowseLibraryItemCommand_Action(object o)
        {
            try
            {
                lock (_syncRoot)
                {
                    switch (o)
                    {
                        case ArtistModel artist:
                            CurrentTabVM.Navigate(ToObservableCollection(Library.GetAlbums(artist.Name)), new NavigationItem(ActionType.OpenArtistAlbums, artist.DisplayName, artist.Name));
                            if (!string.IsNullOrEmpty(artist.Name) && _quickSyncedArtists.Add(artist.Name))
                                Task.Run(async () => await Library.SyncSongsAsync(artist.Name)).FireAndForgetSafe();
                            break;

                        case AlbumModel album:
                            var songs = ToObservableCollection(Library.GetSongs(album.Artist, album.Id).Select(x => new SongViewModel(x)));
                            if (album.Id > 0)
                                CurrentTabVM.Navigate(songs, new NavigationItem(ActionType.OpenAlbumSongs, $"{album.DisplayName}", album.Id.ToString()));
                            else
                                CurrentTabVM.Navigate(songs, new NavigationItem(ActionType.OpenArtistSongs, $"{album.Artist} (All songs)", album.Artist));
                            break;

                        case FolderViewModel folder:
                            CurrentTabVM.Navigate(FolderContentsModelToObservable(Library.GetFolderContents(folder.Path)), new NavigationItem(ActionType.BrowseByFolders, folder.Name, folder.Path));
                            break;

                        default: throw new NotImplementedException();
                    }
                }
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private void PlayNowCommand_Action(object o)
        {
            try
            {
                if (o is null)
                    throw new NullReferenceException();
                switch (o)
                {
                    case TabContentViewModel tabContent:
                        if (tabContent.Content is IEnumerable<AlbumModel> albums)
                            PlayNowCommand_Action(albums.First(x => x.Name.Equals(tabContent.SelectedItem)));
                        else if (tabContent.Content is IEnumerable<ArtistModel> artists)
                            PlayNowCommand_Action(artists.First(x => x.Name.Equals(tabContent.SelectedItem)));
                        else
                            throw new NotImplementedException();
                        break;

                    // Play single song
                    case SongModel song:
                        AddToNowPlayingAndStartPlayback(new[] { song });
                        break;
                    case SongViewModel songVm:
                        AddToNowPlayingAndStartPlayback(new[] { songVm.Song });
                        break;

                    // Play song set
                    case SongsToPlayModel songsToPlay:
                        AddToNowPlayingAndStartPlayback(songsToPlay.Songs.Select(x => x.Song).ToArray(), songsToPlay.StartPlaybackWith.Song);
                        break;

                    case AlbumModel album:
                        AddToNowPlayingAndStartPlayback(Library.GetSongs(album.Artist, album.Id));
                        break;

                    case ArtistModel artist:
                        AddToNowPlayingAndStartPlayback(Library.GetSongs(artist.Name));
                        break;

                    case ListCollectionView listCollectionView:
                        // Play a collection of items, start playback from the current item
                        switch (listCollectionView.CurrentItem)
                        {
                            case ArtistModel artist:
                                throw new NotImplementedException();

                            case AlbumModel album:
                                throw new NotImplementedException();

                            case SongModel song:
                                AddToNowPlayingAndStartPlayback(listCollectionView.Cast<SongModel>().ToArray(), song);
                                break;

                            case SongViewModel songVm:
                                var songVms = listCollectionView.Cast<SongViewModel>().ToArray();
                                var selectedSongVms = songVms.Where(x => x.IsSelected).ToArray();
                                if (selectedSongVms.Length >= 2)
                                    AddToNowPlayingAndStartPlayback(selectedSongVms.Select(x => x.Song).ToArray(), songVm.Song);
                                else
                                    AddToNowPlayingAndStartPlayback(songVms.Select(x => x.Song).ToArray(), songVm.Song);
                                break;

                            default:
                                throw new NotSupportedException(o.GetType().FullName);
                        }
                        break;

                    default:
                        throw new NotSupportedException(o.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private void AddToNowPlayingAndStartPlayback(SongModel[] itemsToPlaylist, SongModel startPlaybackFrom = null)
        {
            lock (_syncRoot)
            {
                NowPlaying.Clear();
                if (!itemsToPlaylist.Any())
                    return;
                NowPlaying.Add(itemsToPlaylist);
                NowPlaying.SetCurrentSong(startPlaybackFrom ?? itemsToPlaylist.First());
            }
            StartPlaypack(NowPlaying.CurrentSong);
        }

        private void StartPlaypack(SongModel song)
        {
            _log.Debug($"{nameof(StartPlaypack)}, {song.Id}, Transcoding={Settings.Transcoding}");
            try
            {
                Player.StreamSong(song, Settings.Transcoding);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                song.PlaybackError = true;
                MessageBox.Show("Playback cannot be started.", "Playback error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LogAndShowException(Exception ex, string message = null)
        {
            if (!string.IsNullOrEmpty(message))
                _log.Error(ex, message);
            else
                _log.Error(ex);
            ShowException(ex, message);
        }

        public void ShowException(Exception ex, string message = null)
        {
            var text = ex.GetType().Name + Environment.NewLine + ex.Message;
            if (!string.IsNullOrEmpty(message))
                text = message + Environment.NewLine + text;
            MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void PlaySong(SongModel song)
        {
            _log.Debug($"{nameof(PlaySong)}, {song.Id}");
            NowPlaying.SetCurrentSong(song);
            StartPlaypack(NowPlaying.CurrentSong);
        }
        public void PlaySong(SongViewModel songVM)
        {
            _log.Debug($"{nameof(PlaySong)}, {songVM.Song.Id}");
            NowPlaying.SetCurrentSongViewModel(songVM);
            StartPlaypack(NowPlaying.CurrentSong);
        }

        public void Disconnect()
        {
            Library.Logout();
            Application.Current.Shutdown();
        }

        public void BackupUserData()
        {
            try
            {
                var sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    FileName = $"SynAudio_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak"
                };
                if (sfd.ShowDialog() == true)
                {
                    var package = Library.BackupUserData();
                    File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(package));
                    MessageBox.Show("OK");
                }
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        public void RestoreUserData()
        {
            if (Library.IsUpdatingInBackground)
            {
                MessageBox.Show("Can't restore backup while synchronization is running.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var ofd = new OpenFileDialog()
                {
                    Filter = "Backup files|*.bak"
                };
                if (ofd.ShowDialog() == true)
                {
                    UserDataBackupModel package = null;
                    try
                    {
                        package = JsonConvert.DeserializeObject<UserDataBackupModel>(File.ReadAllText(ofd.FileName));
                        if (!(package?.SongBackups?.Length > 0))
                            throw new Exception("The backup file is invalid.");
                    }
                    catch (Exception ex)
                    {
                        package = null;
                        _log.Warn(ex, $"Error while deserializing {nameof(UserDataBackupModel)}");
                    }
                    if (package is null)
                    {
                        MessageBox.Show("The backup file is invalid.", "Invalid file", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Restore procedure started in the background.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                        Library.RestoreUserData(package);
                    }
                }
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private bool TryGetSongFileFromNetworkPath(SongModel song, out string songFilePath)
        {
            songFilePath = null;
            try
            {
                if (App.ExistsOnHost(song.Path, out var uncPath))
                    songFilePath = uncPath;
            }
            catch
            {
                // File is not accessible
            }
            return !(songFilePath is null);
        }

        private void SynchronizeLibraryCommand_Action(object o)
        {
            _log.Debug(nameof(SynchronizeLibraryCommand_Action));
            if (Library.Connected)
            {
                Library.SyncDatabaseAsync(true);
            }
        }

        public async Task Open()
        {
            _log.Debug(nameof(Open));

            using (var state = Status.Create("Connecting..."))
            {
                try
                {
                    // Connect to library
                    using (var cur = new CursorChange(Cursors.Wait))
                    {
                        Library = new AudioLibrary(Settings, Status);
                        // Load artists
                        OpenNewTabCommand_Action(null);
                    }

                    await TryToConnect();

                    _log.Debug("Connection initialized");

                    state.Text = "Preparing...";
                    using (var cur = new CursorChange(Cursors.Wait))
                    {
                        Player = new MusicPlayer(Library)
                        {
                            Volume = PercentageToRatio(Settings.Volume)
                        };
                        Player.PlaybackStateChanged += Player_PlaybackStateChanged;

                        // Subscribe to events after successful initialization
                        NowPlaying.CurrentSongChanged += NowPlaying_CurrentSongChanged;
                        Library.ExceptionThrown += Library_ExceptionThrown;
                        Library.Updating += Library_Updating;
                        Library.Updated += Library_Updated;
                        Library.ArtistsUpdated += Library_ArtistsUpdated;
                        Library.AlbumsUpdated += Library_AlbumsUpdated;
                        Library.SyncCompleted += Library_SyncCompleted;
                        Library.AlbumCoverUpdated += Library_AlbumUpdated;
                        Library.SongsUpdated += Library_SongsUpdated;
                        LibraryLoaded = true;

                        // Load NowPlaying state
                        // If the songs inside the state deleted from the library, they will be removed from the playlist automatically
                        try
                        {
                            state.Text = "Loading saved state...";
                            NowPlaying.LoadState(out _restoreSongPosition);
                            if (_restoreSongPosition != TimeSpan.Zero)
                                Player.RestoreState(NowPlaying.CurrentSong, Settings.Transcoding, _restoreSongPosition);
                            _log.Debug("Loaded NowPlaying state");

                            // Sync songs on NowPlaying
                            if (NowPlaying.Songs.Any())
                            {
                                var artistAlbumsToSync = new Dictionary<string, List<string>>();
                                foreach (var song in NowPlaying.Songs.Select(x => x.Song))
                                {
                                    if (!artistAlbumsToSync.ContainsKey(song.Artist))
                                        artistAlbumsToSync.Add(song.Artist, new List<string>(new[] { song.Album }));
                                    else if (!artistAlbumsToSync[song.Artist].Contains(song.Album))
                                        artistAlbumsToSync[song.Artist].Add(song.Album);
                                }

                                Task.Run(async () =>
                                {
                                    if (artistAlbumsToSync.Count == 1)
                                    {
                                        // Sync entire artist
                                        await Library.SyncSongsAsync(artistAlbumsToSync.Keys.First());
                                    }
                                    else
                                    {
                                        // Sync artist-album pairs
                                        foreach (var item in artistAlbumsToSync)
                                        {
                                            var artist = item.Key;
                                            foreach (var album in item.Value)
                                            {
                                                await Library.SyncSongsAsync(artist, album);
                                            }
                                        }
                                    }
                                }).FireAndForgetSafe(ErrorHandler);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }

                        // Sync after startup
                        // The connection test performed inside this method as well
                        //if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Settings.UpdateLibraryOnStartup)
                        Library.SyncDatabaseAsync(true);
                    }
                }
                catch (Exception ex)
                {
                    LogAndShowException(ex);
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void Player_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            try
            {
                App.RefreshCommands();

                switch (e.NewState)
                {
                    case PlaybackStateType.Playing:
                        RaisePlaybackStarted();
                        break;

                    case PlaybackStateType.Stopped:
                        RaisePlaybackStopped();
                        break;

                    case PlaybackStateType.EndOfSong:
                        RaisePlaybackStopped();
                        //Update playback counters only after the song finished completely
                        Library.UpdateSongPlaybackStatistics(NowPlaying.CurrentSong);
                        PlayNextCommand_Action(null);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
        }

        private void RaisePlaybackStopped()
        {
            if (PlaybackStopped != null)
                PlaybackStopped(this);
        }

        private void RaisePlaybackStarted()
        {
            if (PlaybackStarted != null)
                PlaybackStarted(this);
        }

        private void Player_PlayerException(object sender, Exception e)
        {
            LogAndShowException(e);
        }

        private async Task TryToConnect()
        {
            Credentials credentials = null; // Try to re-use the session first
            while (!Connected)
            {
                try
                {
                    using (var cur = new CursorChange(Cursors.Wait))
                        Connected = await Task.Run(async () => await Library.ConnectAsync(credentials?.Password));
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }

                // Show login dialog
                if (!Connected)
                {
                    if (credentials is null)
                        credentials = new Credentials()
                        {
                            Url = Settings.Url,
                            Username = Settings.Username
                        };
                    if (new LoginDialog(credentials).ShowDialog() != true)
                        Environment.Exit(0);
                    Settings.Url = credentials.Url;
                    Settings.Username = credentials.Username;
                    App.SaveSettings();
                }
            }
            App.RefreshCommands();
        }

        private List<SongModel> CollectSongModels()
        {
            var result = new List<SongModel>();
            App.Current.Dispatcher.Invoke(() =>
            {
                lock (_syncRoot)
                {
                    var tabs = Tabs.ToArray();
                    foreach (var tab in tabs)
                    {
                        if (tab.Content is TabViewModel tvm && tvm.Content.Content is IEnumerable<SongViewModel> songs)
                            result.AddRange(songs.Select(x => x.Song));
                    }

                    var fromNowPlaying = NowPlaying.Songs.Select(x => x.Song).Where(x => !result.Contains(x)).ToArray();
                    result.AddRange(fromNowPlaying);
                }
            });
            return result;
        }

        private void Library_SongsUpdated(object sender, SongModel[] songs)
        {
            var oldSongs = CollectSongModels();
            var st = TableInfo.Get<SongModel>();
            var updatedSongDict = songs.ToDictionary(k => k.Id, v => v);
            foreach (var oldSong in oldSongs)
            {
                if (updatedSongDict.TryGetValue(oldSong.Id, out var newSong))
                    SongModel.Copy(newSong, oldSong);
            }
        }

        public void OnClose()
        {
            _log.Debug(nameof(OnClose));
            using (var cur = new CursorChange(Cursors.Wait))
            {
                // Unsubscribe from events
                NowPlaying.CurrentSongChanged -= NowPlaying_CurrentSongChanged;
                if (LibraryLoaded)
                {
                    Library.ExceptionThrown -= Library_ExceptionThrown;
                    Library.Updating -= Library_Updating;
                    Library.Updated -= Library_Updated;
                    Library.ArtistsUpdated -= Library_ArtistsUpdated;
                    Library.AlbumsUpdated -= Library_AlbumsUpdated;
                    Library.SyncCompleted -= Library_SyncCompleted;
                    Library.AlbumCoverUpdated -= Library_AlbumUpdated;
                    Library.SongsUpdated -= Library_SongsUpdated;
                }

                // Save NowPlaying state
                NowPlaying.SaveState(Player?.Position);
                if (Player != null)
                {
                    Player.PlaybackStateChanged -= Player_PlaybackStateChanged;
                    Player.Dispose();
                }
                Library.Dispose();
            }
        }

        private void Library_ExceptionThrown(object sender, Exception exception)
        {
            if (exception is System.AggregateException aex && !(aex.InnerException is null))
                exception = aex.InnerException;

            ShowException(exception);

            if (exception is LibraryResponseException lre)
            {
                if (lre.ErrorCode == (int)SynologyDotNet.Core.Model.LoginErrorCode.AccountParameterMissing)
                {
                    Connected = false;
                    TryToConnect().FireAndForgetSafe(ErrorHandler);
                }
            }
        }

        private void NowPlaying_CurrentSongChanged(NowPlayingViewModel sender)
        {
            string cover = null;
            try
            {
                if (!(sender.CurrentSongVM is null))
                    cover = Library.GetAlbumCover(sender.CurrentSong.AlbumId);
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }
            sender.Cover = cover;
        }

        private static float PercentageToRatio(float value)
        {
            if (value == 0)
                return 0;
            return (float)(value / 100.0);
        }

        private void Library_ArtistsUpdated(object sender, EventArgs e)
        {
            _log.Debug(nameof(Library_ArtistsUpdated));
            // Collect artist models
            // Calculate delta and update accordingly

            //var artistTableInfo = TableInfo.Get<ArtistModel>();
            //var oldModels = Artists.ToDictionary(a => a.Name, a => a);
            //var newModels = Library.GetArtists().ToDictionary(a => a.Name, a => a);

            //var toRemove = oldModels.Where(x => !newModels.ContainsKey(x.Key)).Select(x => x.Value).ToArray();
            //var toAdd = newModels.Where(x => !oldModels.ContainsKey(x.Key)).Select(x => x.Value).ToArray();
            //var toUpdate = newModels.Where(x => oldModels.ContainsKey(x.Key)).Select(x => (newModel: x.Value, oldModel: oldModels[x.Key])).ToArray();

            //foreach (var pair in toUpdate)
            //	artistTableInfo.CopyDifferentProperties(pair.newModel, pair.oldModel); //This is slow and has to be optimized
            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //	Artists.AddRange(toAdd);
            //	foreach (var x in toRemove)
            //		Artists.Remove(x);
            //});
        }

        private void Library_AlbumsUpdated(object sender, EventArgs e)
        {
            //// Albums
            //if (!(SelectedArtist is null))
            //{
            //	// Update album list for selected artist
            //	Application.Current.Dispatcher.Invoke(() =>
            //	{
            //		LoadArtistAlbums(SelectedArtist, true);
            //	});
            //}
        }

        private void UpdateSongModels(ICollection<SongModel> collection)
        {
            if (collection.Count > 0)
            {
                var oldSongs = collection.ToDictionary(a => a.Id, a => a);
                var newSongs = Library.GetSongs(oldSongs.Keys.ToArray());
                var songTableInfo = TableInfo.Get<SongModel>();

                // Update song data
                foreach (var newSong in newSongs)
                {
                    if (oldSongs.TryGetValue(newSong.Id, out var oldSong))
                    {
                        songTableInfo.CopyDifferentProperties(newSong, oldSong);
                        oldSong.LoadCustomizationFromCommentTag();
                    }
                }

                // Remove deleted songs
                foreach (var song in oldSongs.Where(x => !newSongs.Any(y => y.Id == x.Key)).Select(x => x.Value))
                    collection.Remove(song);
            }
        }

        private void Library_SyncCompleted(object sender, EventArgs e)
        {
            //// Songs
            //if (!(SelectedAlbum is null))
            //{
            //	// Update song list for selected album
            //	Application.Current.Dispatcher.Invoke(() =>
            //	{
            //		LoadAlbumSongs(SelectedAlbum, true);
            //	});
            //}
            //else
            //{
            //	// Album not selected, so update the displayed songs only
            //	lock (Songs)
            //		UpdateSongModels(Songs);
            //}

            //// Also refresh the NowPlaying song references
            //UpdateSongModels(NowPlaying.Songs);
        }

        private void Library_SongUpdated(object sender, int songId)
        {
            //Log.Debug($"{nameof(Library_SongUpdated)}, {songId}");
            //if (!CollectSongModels().TryGetValue(songId, out var oldSong))
            //	return;
            //Task.Run(() =>
            //{
            //	var songTableInfo = TableInfo.Get<SongModel>();
            //	var newSong = Library.GetSongs(new[] { oldSong.Id }).FirstOrDefault();
            //	if (!(newSong is null))
            //		SongModel.Copy(newSong, oldSong);
            //});
        }

        private void Library_Updating(object sender, EventArgs e)
        {
            _log.Debug(nameof(Library_Updating));
            IsLibraryUpdating = true;
        }

        private void Library_ArtistUpdated(object sender, ArtistModel artist)
        {
            _log.Debug(nameof(Library_ArtistUpdated));
            //if (Library.Connected)
            //{
            //	Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //	{
            //		Log.Debug("Application.Current.Dispatcher.BeginInvoke");
            //		var myArtist = Artists.FirstOrDefault(a => a.Name == artist.Name);
            //		if (!(myArtist is null))
            //		{
            //			var tableInfo = TableInfo.Get<ArtistModel>();
            //			tableInfo.CopyDifferentProperties(artist, myArtist);
            //		}
            //	}));
            //}
        }

        private void Library_AlbumUpdated(object sender, AlbumModel album)
        {
            //Log.Debug(nameof(Library_AlbumUpdated));
            //if (Library.Connected)
            //{
            //	Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //	{
            //		Log.Debug("Application.Current.Dispatcher.BeginInvoke");
            //		var myAlbum = Albums.FirstOrDefault(a => a.Name == album.Artist && a.Name == album.Name);
            //		if (!(myAlbum is null))
            //		{
            //			var tableInfo = TableInfo.Get<AlbumModel>();
            //			tableInfo.CopyDifferentProperties(album, myAlbum);
            //		}
            //	}));
            //}
        }

        private void Library_Updated(object sender, EventArgs e)
        {
            _log.Debug(nameof(Library_Updated));
            IsLibraryUpdating = false;
        }

        public async Task SetRating(int rating)
        {
            _log.Debug(nameof(SetRating));

            if (rating < 0 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating));

            if (NowPlaying.CurrentSongVM != null)
            {
                NowPlaying.CurrentSong.Rating = rating;
                await Task.Run(async () => await Library.SetRating(NowPlaying.CurrentSong, rating));
            }
        }

        #endregion

        #region [Private Methods]

        private void Player_PauseCommand_Action(object obj)
        {
            _log.Debug(nameof(Player_PauseCommand_Action));
            if (Player.PlaybackState == PlaybackStateType.Playing)
            {
                Player.Pause();
            }
        }

        private void PlayCommand_Action(object obj)
        {
            _log.Debug(nameof(PlayCommand_Action));
            switch (Player.PlaybackState)
            {
                case PlaybackStateType.Playing:
                    return;

                case PlaybackStateType.Paused:
                    Player.Play();
                    break;

                default:
                    if (NowPlaying.Songs.Any())
                    {
                        if (NowPlaying.CurrentSongVM is null)
                        {
                            var next = NowPlaying.Next();
                            if (!(next is null))
                                StartPlaypack(next);
                        }
                        else
                        {
                            StartPlaypack(NowPlaying.CurrentSong);
                        }
                    }
                    break;
            }
        }

        private void PlayNextCommand_Action(object obj)
        {
            _log.Debug(nameof(PlayNextCommand_Action));
            var song = NowPlaying.Next();
            if (!(song is null) && !song.PlaybackError)
                StartPlaypack(song);
        }

        private void PlayPrevCommand_Action(object obj)
        {
            _log.Debug(nameof(PlayPrevCommand_Action));
            var song = NowPlaying.Previous();
            if (!(song is null))
                StartPlaypack(song);
        }

        private void Player_ShuffleCommand_Action(object obj)
        {
            NowPlaying.Shuffle = !NowPlaying.Shuffle;
        }

        private void Player_RepeatCommand_Action(object obj)
        {
            NowPlaying.Repeat = !NowPlaying.Repeat;
        }

        private void Player_MuteCommand_Action(object obj)
        {
            if (PlayerVolume > 0)
            {
                _volumeBeforeMuted = PlayerVolume;
                PlayerVolume = 0;
            }
            else
            {
                PlayerVolume = _volumeBeforeMuted;
            }
            if (!(Player is null))
                Player.Volume = PercentageToRatio(PlayerVolume);
        }

        #endregion
    }
}
