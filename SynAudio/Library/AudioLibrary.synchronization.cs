using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqlCeLibrary;
using SynAudio.DAL;
using SynologyDotNet.AudioStation.Model;
using Utils;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public async Task SyncSongsAsync(string artist, string album = null)
        {
            if (string.IsNullOrEmpty(artist))
                throw new ArgumentException(nameof(artist));

            var s = TableInfo.Get<SongModel>();
            var filter = new List<string>();
            var parameters = new List<object>();
            var queryFilter = new List<(SongQueryParameters, object)>();

            filter.Add($"{s[nameof(SongModel.Artist)]} = @{parameters.Count}");
            parameters.Add(artist);
            queryFilter.Add((SongQueryParameters.artist, artist));

            // Kept for the future, so filtering by album is possible too
            if (!string.IsNullOrEmpty(album))
            {
                filter.Add($"{s[nameof(SongModel.Album)]} = @{parameters.Count}");
                parameters.Add(album);
                queryFilter.Add((SongQueryParameters.album, album));
            }

            using (var sql = Sql())
            {
                var dbSongs = sql.Select<SongModel>($"WHERE {string.Join(" AND ", filter)}", parameters.ToArray());
                if (dbSongs.Length > 0)
                {
                    using (var state = _status.Create($"Sync {dbSongs.Length} songs..."))
                    {
                        var response = await _audioStation.ListSongsAsync(10000, 0, SongQueryAdditional.All, queryFilter.ToArray()).ConfigureAwait(false);
                        if (response.Success && response?.Data.Songs?.Length > 0)
                        {
                            var dtos = response.Data.Songs.Select(x => (Id: x.ID, Dto: x)).ToDictionary(k => k.Id, v => v.Dto);
                            dbSongs = dbSongs.Where(x => dtos.ContainsKey(x.Id)).ToArray();
                            if (dbSongs.Length > 0)
                            {
                                foreach (var song in dbSongs)
                                    song.LoadFromDto(dtos[song.Id]);
                                sql.Update(dbSongs);
                                SongsUpdated?.BeginInvoke(this, dbSongs, null, null);
                            }
                        }
                    }
                }
            }
        }

        public void SyncDatabaseAsync(bool forceUpdate)
        {
            _log.Debug(nameof(SyncAsync));
            if (_updateCacheJob?.IsRunning != true && _restoreBackupJob?.IsRunning != true)
            {
                Updating?.Invoke(this);
                _updateCacheJob = new BackgroundThreadWorker(SyncWorkMethod, nameof(SyncWorkMethod));
                _updateCacheJob.Start(forceUpdate);
            }
        }

        private void SyncWorkMethod(WorkerMethodParameter p)
        {
            _log.Debug(nameof(SyncWorkMethod));
            try
            {
                using (var sql = Sql())
                using (var state = _status.Create("Checking for updates..."))
                {
                    //// Cover download
                    //state.Text = "Downloading covers...";
                    //SyncCoversAsync(p.Token, true).Wait();

                    // Library synchronization
                    var manualSync = (bool)p.Data;
                    if (manualSync || IsMusicSyncNecessary(sql).Result)
                    {
                        state.Text = "Syncing...";
                        // Reset sync related status
                        sql.WriteInt64(Int64Values.LastCoverDownloadCompleted, 0);
                        sql.WriteInt64(Int64Values.LastSyncCompleted, 0);
                        sql.WriteInt64(Int64Values.LastSongAnalysisCompleted, 0);

                        // Music sync
                        SyncAsync(p.Token).Wait();

                        // Finished music sync
                        if (!p.Token.IsCancellationRequested)
                        {
                            sql.WriteInt64(Int64Values.LastSyncCompleted, 1);
                            sql.WriteInt64(Int64Values.LastSyncDateTimeUtc, DateTime.UtcNow.Ticks);
                        }
                        Updated?.BeginInvoke(this, null, null);
                    }

                    // Cover download
                    if (!p.Token.IsCancellationRequested && sql.ReadInt64(Int64Values.LastCoverDownloadCompleted) != 1)
                    {
                        state.Text = "Downloading covers...";
                        SyncCoversAsync(p.Token, manualSync).Wait();
                        if (!p.Token.IsCancellationRequested)
                            sql.WriteInt64(Int64Values.LastCoverDownloadCompleted, 1);
                    }

                    // Song analysis
                    if (!p.Token.IsCancellationRequested && sql.ReadInt64(Int64Values.LastSongAnalysisCompleted) != 1)
                    {
                        //Todo
                        //if (Api.CanUseTagging)
                        //{
                        //	AnalyzeSongs(p.Token);
                        //	if (!p.Token.IsCancellationRequested)
                        //		Int64Value.Write(sql, Int64Values.LastSongAnalysisCompleted, 1);
                        //}
                        //else
                        //{
                        //	Log.Warn("Cannot analyze songs, because the application portal for AudioStation is disabled. Please enable application portal for AudioStation using the default alias.");
                        //	// Todo: put a warning onto the UI to notify the user about this usage limitation.
                        //}
                    }
                }
            }
            catch (ThreadAbortException)
            {
                _log.Debug($"{nameof(SyncWorkMethod)} aborted");
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        private async Task<bool> IsMusicSyncNecessary(SqlCe sql)
        {
            // Was the last sync successful?
            if (sql.ReadInt64(Int64Values.LastSyncCompleted) != 1)
                return true;

            // Compare song count with API
            var songTable = TableInfo.Get<SongModel>();
            var countInDb = (int)sql.ExecuteScalar($"SELECT COUNT(*) FROM {songTable.NameWithBrackets}");
            var response = await _audioStation.ListSongsAsync(1, 0, SongQueryAdditional.None).ConfigureAwait(false); // It is enough to query just 1 song, because the response includes the total count
            GuardResponse(response);
            if (response.Data.Total != countInDb)
                return true;

            // Commented out, because my goal is to avoid regular full-syncs and use small partial syncs while the user is navigating in the library
            //// If the last sync was 24 hours ago
            //var lastSyncTicks = Int64Value.Read(sql, Int64Values.LastSyncDateTimeUtc);
            //if (!lastSyncTicks.HasValue || (DateTime.UtcNow - DateTime.FromBinary(lastSyncTicks.Value) > TimeSpan.FromHours(24)))
            //	return true;

            return false;
        }

        private async Task SyncAsync(CancellationToken token)
        {
            _log.Debug(nameof(SyncAsync));
            var songTable = TableInfo.Get<SongModel>();
            var songIDsFromApi = new HashSet<string>();
            var songsToCheckBackups = new List<(string ID, string Artist, string Album, string Title)>();
            var albums = CreateMultiComparerStringDictionary<AlbumModel>();

            using (var sql = Sql())
            {
                foreach (var album in sql.Select<AlbumModel>())
                    albums.Add(AlbumModel.ConstructSimpleKey(album), album);

                int downloaded = 0;
                int offset = 0;
                int total = -1;
                var insertDate = DateTime.Now;

                // Get all hashcodes for DB songs
                var dbSongHashCodes = new Dictionary<string, int>();
                sql.ExecuteReader($"SELECT {songTable[nameof(SongModel.Id)]}, {songTable[nameof(SongModel.HashCode)]} FROM {songTable}",
                    (r) => dbSongHashCodes.Add(r.GetString(0), r.GetInt32(1))
                );

                do
                {
                    var response = await _audioStation.ListSongsAsync(ApiPageSize, offset, SongQueryAdditional.All).ConfigureAwait(false);
                    if (!response.Success)
                        break;

                    if (total == -1)
                        total = response.Data.Total;
                    downloaded += response.Data.Songs.Length;

                    var songModels = new Dictionary<string, SongModel>();
                    foreach (var dto in response.Data.Songs)
                    {
                        var song = new SongModel()
                        {
                            InsertDate = insertDate
                        };
                        song.LoadFromDto(dto);
                        if (songIDsFromApi.Add(song.Id)) //Just for extra safety, to avoid duplicates. This might happen if you copy song files to the NAS while synchronization is in progress
                        {
                            songModels.Add(song.Id, song);
                            songIDsFromApi.Add(song.Id);

                            // Map to an album
                            if (!string.IsNullOrEmpty(song.Album))
                            {
                                var albumKey = AlbumModel.ConstructSimpleKey(song.AlbumArtist, song.Album, song.Year);
                                if (!albums.TryGetValue(albumKey, out var album))
                                {
                                    // Insert new album
                                    album = new AlbumModel()
                                    {
                                        InsertDate = insertDate,
                                        Artist = song.AlbumArtist,
                                        Name = song.Album,
                                        Year = song.Year.HasValue ? song.Year.Value : 0
                                    };
                                    sql.Insert(album);
                                    albums.Add(albumKey, album);
                                }
                                song.AlbumId = album.Id;
                            }
                        }
                    }

                    // Update Song
                    var toUpdate = songModels
                        .Where(x => dbSongHashCodes.TryGetValue(x.Key, out var dbHashCode) && x.Value.HashCode != dbHashCode)
                        .Select(x => x.Value)
                        .ToArray();
                    _log.Debug($"Songs to update: {toUpdate.Length}");
                    if (toUpdate.Length > 0)
                    {
                        using (var tran = sql.BeginTransaction())
                        {
                            foreach (var batch in SqlCe.PageCollection(toUpdate, DatabaseBatchSize))
                            {
                                if (token.IsCancellationRequested)
                                    return;
                                sql.Update(batch);
                            }
                            tran.Commit();
                        }
                    }

                    // Insert Song
                    if (token.IsCancellationRequested)
                        return;
                    var toInsert = songModels.Where(a => !dbSongHashCodes.ContainsKey(a.Key)).Select(a => a.Value).ToArray();
                    if (toInsert.Any())
                    {
                        _log.Debug($"Songs to insert: {toInsert.Length}");
                        try
                        {
                            var cueFiles = toInsert.Where(x => x.Path.EndsWith(".cue", StringComparison.OrdinalIgnoreCase)).Select(x => x.Path).ToArray();
                            if (cueFiles.Length > 0)
                                _log.Warn("CUE sheet detected. These files are causing metadata loss, please delete them!" + Environment.NewLine + string.Join(Environment.NewLine, cueFiles));

                            sql.Insert(toInsert);
                            songsToCheckBackups.AddRange(toInsert
                                .Where(a => !string.IsNullOrEmpty(a.Artist) && !string.IsNullOrEmpty(a.Album) && !string.IsNullOrEmpty(a.Title))
                                .Select(a => (a.Id, a.Artist, a.Album, a.Title)));
                        }
                        catch (Exception ex)
                        {
                            OnException(ex, $"Failed to insert {toInsert.Length} songs");
                        }
                    }

                    offset += ApiPageSize;
                }
                while (!token.IsCancellationRequested && downloaded < total);

                // Delete songs
                var songIDsToDelete = dbSongHashCodes.Keys.Except(songIDsFromApi).ToArray();
                if (songIDsToDelete.Length > 0)
                {
                    // Backup rating before deletion
                    var songDataToBackup = sql.Select<SongModel>($"WHERE {songTable[nameof(SongModel.Rating)]} <> 0"
                        + $" AND LEN({songTable[nameof(SongModel.Artist)]}) > 0"
                        + $" AND LEN({songTable[nameof(SongModel.Album)]}) > 0"
                        + $" AND LEN({songTable[nameof(SongModel.Title)]}) > 0"
                        + $" AND {songTable[nameof(SongModel.Id)]} IN ({string.Join(",", songIDsToDelete.Select(x => "'" + x + "'"))})");
                    foreach (var song in songDataToBackup)
                    {
                        var b = new SongBackup()
                        {
                            Artist = song.Artist.Trim(),
                            Album = song.Album.Trim(),
                            Title = song.Title.Trim(),
                            Path = song.Path,
                            Rating = song.Rating
                        };
                        sql.DeleteSingleByPrimaryKey<SongBackup>(b.Artist, b.Album, b.Title);
                        sql.Insert(b);
                    }

                    _log.Debug($"Songs to delete: {songIDsToDelete.Length}");
                    sql.DeleteMultipleByPrimaryKey<SongModel>(songIDsToDelete.Select(x => new object[] { x }).ToArray());
                }

                // Try restore data from backups
                foreach (var item in songsToCheckBackups)
                {
                    var backup = sql.Select<SongBackup>($"WHERE {songTable[nameof(SongBackup.Artist)]} = @0 AND {songTable[nameof(SongBackup.Album)]} = @1 AND {songTable[nameof(SongBackup.Title)]} = @2",
                        item.Artist.Trim(), item.Album.Trim(), item.Title.Trim()).FirstOrDefault();
                    if (backup is null)
                        continue;
                    var song = sql.Select<SongModel>($"WHERE {songTable[nameof(SongModel.Id)]} = @0", item.ID).First();
                    if (song.Rating == 0)
                    {
                        if (_audioStation.RateSongAsync(song.Id, song.Rating).Result.Success)
                        {
                            song.Rating = backup.Rating;
                            sql.Update(song);
                            sql.DeleteSingle(backup);
                            _log.Debug($"Restored backup for \"{song.Id}\"");
                        }
                        else
                        {
                            _log.Error($"Could not restore backup for \"{song.Id}\"");
                        }
                    }
                    else
                    {
                        sql.DeleteSingle(backup);
                    }
                }

                // Delete orphaned albums
                var albumTable = TableInfo.Get<AlbumModel>();
                sql.ExecuteNonQuery($"DELETE FROM {albumTable} WHERE {albumTable[nameof(AlbumModel.Id)]} NOT IN (SELECT DISTINCT {songTable[nameof(SongModel.AlbumId)]} FROM {songTable})");
                AlbumsUpdated?.BeginInvoke(this, null, null);
            }
            SyncCompleted?.BeginInvoke(this, null, null);
            BuildArtists(token);
        }

        /// <summary>
        /// Generates artist records from songs in the DB.
        /// </summary>
        /// <param name="token"></param>
        private void BuildArtists(CancellationToken token)
        {
            var a = TableInfo.Get<ArtistModel>();
            var s = TableInfo.Get<SongModel>();
            using (var sql = Sql())
            {
                // Deleted
                //if (syncCompleted)
                sql.ExecuteNonQuery($"DELETE FROM {a.NameWithBrackets} WHERE {a[nameof(ArtistModel.Name)]} NOT IN (SELECT DISTINCT {s[nameof(SongModel.Artist)]} FROM {s.NameWithBrackets})");
                // New
                if (!token.IsCancellationRequested)
                    sql.ExecuteNonQuery(
                        $@"INSERT INTO {a.NameWithBrackets}({a[nameof(ArtistModel.Name)]})
						SELECT DISTINCT s.{s[nameof(SongModel.AlbumArtist)]} FROM {s.NameWithBrackets} s
						LEFT JOIN {a.NameWithBrackets} a ON a.{a[nameof(ArtistModel.Name)]} = s.{s[nameof(SongModel.AlbumArtist)]}
						WHERE LEN(s.{s[nameof(SongModel.AlbumArtist)]}) > 0 AND a.{a[nameof(ArtistModel.Name)]} IS NULL");
            }
            ArtistsUpdated?.BeginInvoke(this, null, null);
        }
    }
}
