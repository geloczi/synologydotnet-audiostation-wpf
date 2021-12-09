using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SynAudio.DAL;
using SynologyDotNet.AudioStation.Model;
using Utils;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public async Task SyncSongsAsync(string artist, string album = null)
        {
            //TODO: partial sync
            //if (string.IsNullOrEmpty(artist))
            //    throw new ArgumentException(nameof(artist));

            //var s = TableInfo.Get<SongModel>();
            //var filter = new List<string>();
            //var parameters = new List<object>();
            //var queryFilter = new List<(SongQueryParameters, object)>();

            //filter.Add($"{s[nameof(SongModel.Artist)]} = @{parameters.Count}");
            //parameters.Add(artist);
            //queryFilter.Add((SongQueryParameters.artist, artist));

            //// Kept for the future, so filtering by album is possible too
            //if (!string.IsNullOrEmpty(album))
            //{
            //    filter.Add($"{s[nameof(SongModel.Album)]} = @{parameters.Count}");
            //    parameters.Add(album);
            //    queryFilter.Add((SongQueryParameters.album, album));
            //}

            //using (var sql = Sql())
            //{
            //    var dbSongs = sql.Select<SongModel>($"WHERE {string.Join(" AND ", filter)}", parameters.ToArray());
            //    if (dbSongs.Length > 0)
            //    {
            //        using (var state = _status.Create($"Sync {dbSongs.Length} songs..."))
            //        {
            //            var response = await _audioStation.ListSongsAsync(10000, 0, SongQueryAdditional.All, queryFilter.ToArray()).ConfigureAwait(false);
            //            if (response.Success && response?.Data.Songs?.Length > 0)
            //            {
            //                var dtos = response.Data.Songs.Select(x => (Id: x.ID, Dto: x)).ToDictionary(k => k.Id, v => v.Dto);
            //                dbSongs = dbSongs.Where(x => dtos.ContainsKey(x.Id)).ToArray();
            //                if (dbSongs.Length > 0)
            //                {
            //                    foreach (var song in dbSongs)
            //                        song.LoadFromDto(dtos[song.Id]);
            //                    sql.Update(dbSongs);
            //                    SongsUpdated.FireAsync(this, dbSongs);
            //                }
            //            }
            //        }
            //    }
            //}
        }

        public void SyncDatabaseAsync(bool forceUpdate)
        {
            _log.Debug(nameof(SyncAsync));
            if (_updateCacheJob?.IsRunning != true && _restoreBackupJob?.IsRunning != true)
            {
                Updating.FireAsync(this, EventArgs.Empty);
                _updateCacheJob = new BackgroundThreadWorker(SyncWorkMethod, nameof(SyncWorkMethod));
                _updateCacheJob.Start(forceUpdate);
            }
        }

        private void SyncWorkMethod(WorkerMethodParameter p)
        {
            _log.Debug(nameof(SyncWorkMethod));
            try
            {
                using (var state = _status.Create("Checking for updates..."))
                {
                    //// Cover download
                    //state.Text = "Downloading covers...";
                    //SyncCoversAsync(p.Token, true).Wait();

                    // Library synchronization
                    //var manualSync = (bool)p.Data;
                    bool manualSync = true; //TODO
                    if (manualSync || IsMusicSyncNecessary().Result)
                    {
                        state.Text = "Syncing...";
                        // Reset sync related status
                        DbSettings.WriteInt64(Int64Values.LastCoverDownloadCompleted, 0);
                        DbSettings.WriteInt64(Int64Values.LastSyncCompleted, 0);
                        DbSettings.WriteInt64(Int64Values.LastSongAnalysisCompleted, 0);

                        // Music sync
                        SyncAsync(p.Token).Wait();

                        // Finished music sync
                        if (!p.Token.IsCancellationRequested)
                        {
                            DbSettings.WriteInt64(Int64Values.LastSyncCompleted, 1);
                            DbSettings.WriteInt64(Int64Values.LastSyncDateTimeUtc, DateTime.UtcNow.Ticks);
                        }
                        Updated.FireAsync(this, EventArgs.Empty);
                    }

                    // Cover download
                    if (!p.Token.IsCancellationRequested && DbSettings.ReadInt64(Int64Values.LastCoverDownloadCompleted) != 1)
                    {
                        state.Text = "Downloading covers...";
                        SyncCoversAsync(p.Token, manualSync).Wait();
                        if (!p.Token.IsCancellationRequested)
                            DbSettings.WriteInt64(Int64Values.LastCoverDownloadCompleted, 1);
                    }

                    // Song analysis
                    if (!p.Token.IsCancellationRequested && DbSettings.ReadInt64(Int64Values.LastSongAnalysisCompleted) != 1)
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

        private async Task<bool> IsMusicSyncNecessary()
        {
            //// Was the last sync successful?
            //if (DB.ReadInt64(Int64Values.LastSyncCompleted) != 1)
            //    return true;

            //// Compare song count with API
            //var songTable = TableInfo.Get<SongModel>();
            //var countInDb = (int)sql.ExecuteScalar($"SELECT COUNT(*) FROM {songTable.NameWithBrackets}");
            //var response = await _audioStation.ListSongsAsync(1, 0, SongQueryAdditional.None).ConfigureAwait(false); // It is enough to query just 1 song, because the response includes the total count
            //GuardResponse(response);
            //if (response.Data.Total != countInDb)
            //    return true;

            // Commented out, because my goal is to avoid regular full-syncs and use small partial syncs while the user is navigating in the library
            //// If the last sync was 24 hours ago
            //var lastSyncTicks = Int64Value.Read(sql, Int64Values.LastSyncDateTimeUtc);
            //if (!lastSyncTicks.HasValue || (DateTime.UtcNow - DateTime.FromBinary(lastSyncTicks.Value) > TimeSpan.FromHours(24)))
            //	return true;

            return true;
        }

        private async Task SyncAsync(CancellationToken token)
        {
            _log.Debug(nameof(SyncAsync));

            int downloaded = 0;
            int offset = 0;
            int total = -1;
            var insertDate = DateTime.Now;

            // Get all hashcodes for DB songs
            var dbSongHashCodes = new Dictionary<string, string>();
            foreach (var song in Db.Table<SongModel>().Select(x => new { x.Id, x.Md5Hash }))
                dbSongHashCodes[song.Id] = song.Md5Hash;

            // Artists
            var dbArtists = new HashSet<string>();
            foreach (var artist in Db.Table<ArtistModel>())
                dbArtists.Add(artist.Name);

            var songIDsFromApi = new HashSet<string>();
            var albumModelByArtistAlbumYear = new Dictionary<string, Dictionary<string, Dictionary<int, AlbumModel>>>();
            var songsToCheckBackups = new List<(string Id, string Artist, string Album, string Title)>();

            do
            {
                // Download songs
                var response = await _audioStation.ListSongsAsync(ApiPageSize, offset, SongQueryAdditional.All).ConfigureAwait(false);
                if (!response.Success)
                    break;
                if (total == -1)
                    total = response.Data.Total;
                downloaded += response.Data.Songs.Length;

                // Construct SongModels
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

                        // Album
                        if (!string.IsNullOrEmpty(song.Album))
                        {
                            if (!albumModelByArtistAlbumYear.TryGetValue(song.AlbumArtist, out var byArtist))
                            {
                                byArtist = new Dictionary<string, Dictionary<int, AlbumModel>>();
                                albumModelByArtistAlbumYear[song.AlbumArtist] = byArtist;
                            }
                            if (!byArtist.TryGetValue(song.Album, out var byAlbum))
                            {
                                byAlbum = new Dictionary<int, AlbumModel>();
                                byArtist[song.Album] = byAlbum;
                            }
                            if (!byAlbum.TryGetValue(song.Year, out var albumModel))
                            {
                                albumModel = new AlbumModel()
                                {
                                    InsertDate = insertDate,
                                    Artist = song.AlbumArtist,
                                    Name = song.Album,
                                    Year = song.Year
                                };
                                byAlbum[song.Year] = albumModel;
                                Db.Insert(albumModel);
                            }
                            song.AlbumId = albumModel.Id;
                        }

                        // Artist
                        if (!string.IsNullOrEmpty(song.Artist) && dbArtists.Add(song.Artist))
                        {
                            Db.Insert(new ArtistModel()
                            {
                                Name = song.Artist
                            });
                        }
                    }
                }


                // Update Song
                var toUpdate = songModels
                    .Where(x => dbSongHashCodes.TryGetValue(x.Key, out var dbHashCode) && x.Value.Md5Hash != dbHashCode)
                    .Select(x => x.Value)
                    .ToArray();
                _log.Debug($"Songs to update: {toUpdate.Length}");
                if (toUpdate.Length > 0)
                {
                    Db.UpdateAll(toUpdate);
                }

                // Insert Song
                if (token.IsCancellationRequested)
                    return;
                var toInsert = songModels.Where(a => !dbSongHashCodes.ContainsKey(a.Key)).Select(a => a.Value).ToArray();
                if (toInsert.Length > 0)
                {
                    _log.Debug($"Songs to insert: {toInsert.Length}");
                    try
                    {
                        var cueFiles = toInsert.Where(x => x.Path.EndsWith(".cue", StringComparison.OrdinalIgnoreCase)).Select(x => x.Path).ToArray();
                        if (cueFiles.Length > 0)
                            _log.Warn("CUE sheet detected. These files are causing metadata loss, please delete them!" + Environment.NewLine + string.Join(Environment.NewLine, cueFiles));

                        //// Try to find a matching backups
                        //foreach (var song in toInsert)
                        //{
                        //    var backup = DB.Table<SongBackup>().FirstOrDefault(x => x.Artist == song.Artist && x.Album == song.Album && song.Title == song.Title);
                        //    if (!(backup is null))
                        //    {
                        //        if (_audioStation.RateSongAsync(song.Id, song.Rating).Result.Success)
                        //        {
                        //            song.Rating = backup.Rating;
                        //            DB.Delete(backup);
                        //        }
                        //    }
                        //}

                        Db.InsertAll(toInsert);

                        //songsToCheckBackups.AddRange(toInsert
                        //    .Where(a => !string.IsNullOrEmpty(a.Artist) && !string.IsNullOrEmpty(a.Album) && !string.IsNullOrEmpty(a.Title))
                        //    .Select(a => (a.Id, a.Artist, a.Album, a.Title)));
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
                Db.RunInTransaction(() =>
                {
                    foreach (var id in songIDsToDelete)
                    {
                        // Backup
                        var song = Db.Table<SongModel>().First(x => x.Id == id);
                        var b = new SongBackup(song);
                        Db.InsertOrReplace(b);

                        // Then delete
                        Db.Delete(song);
                    }
                });
            }

            //// Try restore data from backups
            //foreach (var item in songsToCheckBackups)
            //{
            //    // Try to find a matching backup
            //    var backup = DB.Table<SongBackup>().FirstOrDefault(x => x.Artist == item.Artist && x.Album == item.Album && x.Title == item.Title);
            //    if (backup is null)
            //        continue;
            //    var song = DB.Table<SongModel>().FirstOrDefault(x => x.Id == item.Id);

            //    if (song.Rating == 0)
            //    {

            //        if (_audioStation.RateSongAsync(song.Id, song.Rating).Result.Success)
            //        {
            //            song.Rating = backup.Rating;
            //            DB.Update(song);
            //        }
            //    }
            //    else
            //    {
            //        DB.Delete(backup);
            //    }
            //}

            DeleteOrphanedAlbums();
            DeleteOrphanedArtists();

            SyncCompleted.FireAsync(this, EventArgs.Empty);
        }

        private void DeleteOrphanedAlbums()
        {
            var cmd = Db.CreateCommand($"DELETE FROM Album WHERE {nameof(AlbumModel.Id)} NOT IN (SELECT DISTINCT {nameof(SongModel.AlbumId)} FROM Song)");
            cmd.ExecuteNonQuery();
        }

        private void DeleteOrphanedArtists()
        {
            var cmd = Db.CreateCommand($"DELETE FROM Artist WHERE {nameof(ArtistModel.Name)} NOT IN (SELECT DISTINCT {nameof(SongModel.Artist)} FROM Song)");
            cmd.ExecuteNonQuery();
        }
    }
}
