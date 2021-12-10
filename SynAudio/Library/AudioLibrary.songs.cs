using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SynAudio.DAL;
using SynologyDotNet.AudioStation;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public SongModel[] GetSongs(string artist) => GetSongs(artist, -1);

        public SongModel[] GetSongs(string artist, int albumId)
        {
            _log.Debug($"{nameof(GetSongs)}, {nameof(albumId)}={albumId}");
            var songs = Db.Table<SongModel>();

            if (!(artist is null) && albumId > 0)
                return songs.Where(x => (x.Artist == artist || x.AlbumArtist == artist) && x.AlbumId == albumId).ToArray();
            if (!(artist is null) && albumId <= 0)
                return songs.Where(x => x.Artist == artist || x.AlbumArtist == artist).ToArray();
            else if (artist is null)
                return songs.Where(x => x.AlbumId == albumId).ToArray();

            return new SongModel[0];
        }

        public SongModel[] GetSongs(string[] ids)
        {
            var songs = Db.Query<SongModel>($"WHERE {nameof(SongModel.Id)} IN ({string.Join(",", ids.Select(x => "'" + x + "'"))})");
            return songs.ToArray();
        }

        //public UserDataBackupModel BackupUserData()
        //{
        //    var ti = TableInfo.Get<SongModel>();
        //    // Create backup from cache
        //    var result = sql.Select<SongModel>($"WHERE {ti[nameof(SongModel.Rating)]} > 0").Select(s => new SongBackup(s)).ToList();
        //    // Then add orphaned songs (skip on conflict)
        //    foreach (var orphanedBackup in sql.Select<SongBackup>())
        //    {
        //        if (!result.Any(r => r.CompareMetadata(orphanedBackup)))
        //            result.Add(orphanedBackup);
        //    }
        //    return new UserDataBackupModel()
        //    {
        //        SongBackups = result.ToArray()
        //    };
        //}

        //public void RestoreUserData(UserDataBackupModel package)
        //{
        //    if (_updateCacheJob?.IsRunning == true || _restoreBackupJob?.IsRunning == true)
        //        throw new InvalidOperationException("Cannot restore backup while synchronization is running.");
        //    if (!(package?.SongBackups?.Length > 0))
        //        throw new ArgumentException(nameof(package));

        //    _restoreBackupJob = new BackgroundThreadWorker((p) =>
        //    {
        //        int restoredCount = 0;
        //        int notFoundCount = 0;
        //        int failCount = 0;
        //        using (var state = _status.Create("Restoring user data from backup..."))
        //        using (var sql = Sql())
        //        {
        //            // Restore song-by-song
        //            var ti = TableInfo.Get<SongModel>();
        //            for (int i = 0; i < package.SongBackups.Length; i++)
        //            {
        //                if (p.Token.IsCancellationRequested)
        //                    break;
        //                var sb = package.SongBackups[i];
        //                try
        //                {
        //                    var song = sql.SelectFirst<SongModel>(
        //                        $"WHERE ({ti[nameof(SongModel.Path)]}=@0 OR ({ti[nameof(SongModel.Artist)]}=@1) AND {ti[nameof(SongModel.Album)]}=@2 AND {ti[nameof(SongModel.Title)]}=@3) ",
        //                        new object[] { sb.Path, sb.Artist, sb.Album, sb.Title, sb.Rating });
        //                    if (song is null)
        //                    {
        //                        ++notFoundCount;
        //                    }
        //                    else if (song.Rating != sb.Rating)
        //                    {
        //                        var response = _audioStation.RateSongAsync(song.Id, sb.Rating).Result; //Call API to set rating on Synology side
        //                        if (response.Success)
        //                        {
        //                            song.Rating = sb.Rating;
        //                            sql.Update(song, new[] { nameof(SongModel.Rating) }); //Store the rating in our cache
        //                            ++restoredCount;
        //                        }
        //                        else
        //                            throw new Exception($"{nameof(_audioStation.RateSongAsync)} failed. Code: {response.Error.Code}");
        //                    }
        //                    else
        //                    {
        //                        ++restoredCount; //Already restored
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    ++failCount;
        //                    _log.Error(ex, $"Failed to restore song backup for \"{sb}\"");
        //                }
        //                state.Text = $"Restoring user data from backup ({i + 1}/{package.SongBackups.Length})";
        //            }
        //        }
        //    }, nameof(RestoreUserData));
        //    _restoreBackupJob.Start(package);
        //}

        public async Task SetRating(SongModel song, int rating)
        {
            _log.Debug($"{nameof(SetRating)}, {song.Id}");
            using (var state = _status.Create("Rate song..."))
            {
                await _audioStation.RateSongAsync(song.Id, rating).ConfigureAwait(false);
                Db.Update(song);
                //using (var sql = Sql())
                //    sql.Update(song, new[] { nameof(SongModel.Rating) });
            }
        }

        public Task StreamSongAsync(CancellationToken token, TranscodeMode transcode, string songId, double positionSeconds, Action<SongStream> readAction)
        {
            return _audioStation.StreamSongAsync(token, transcode, songId, positionSeconds, readAction);
        }

        public void DeleteSongsFromCache(IEnumerable<SongModel> songs)
        {
            Db.RunInTransaction(() =>
            {
                foreach (var song in songs)
                {
                    Db.Delete(song);
                }
            });
            //sql.DeleteMultipleByPrimaryKey<SongModel>(songs.Select(x => new object[] { x.Id }).ToArray());
        }

        public void UpdateSongPlaybackStatistics(SongModel song)
        {
            //TODO
            //using (var sql = Sql())
            //{
            //    //Always get the song from the cache and update those values
            //    var dbSong = sql.SelectSingleEntity(song);
            //    song.PlayCount = ++dbSong.PlayCount;
            //    song.LastPlayDate = dbSong.LastPlayDate = DateTime.Now;
            //    sql.Update(dbSong, nameof(SongModel.PlayCount), nameof(SongModel.LastPlayDate));
            //}
        }
    }
}
