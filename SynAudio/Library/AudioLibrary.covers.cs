using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SynAudio.DAL;
using Utils;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        private async Task SyncCoversAsync(CancellationToken token, bool force)
        {
            List<AlbumModel> albums;
            if (force)
            {
                albums = new List<AlbumModel>();
                foreach (var album in Db.Table<AlbumModel>())
                {
                    if (string.IsNullOrEmpty(album.CoverFileName))
                    {
                        // Albums without cover
                        albums.Add(album);
                    }
                    else
                    {
                        // If the cover file does not exist
                        if (!File.Exists(album.Cover))
                            albums.Add(album);
                    }
                }
            }
            else
            {
                // Get albums without cover (haven't tried to download yet)
                albums = Db.Table<AlbumModel>().Where(x => x.CoverFileName == null).ToList();
            }

            // Sort albums
            albums = albums.OrderByDescending(x => x.InsertDate).ThenBy(x => x.Name).ToList();

            var processor = new QueueProcessorTasks<AlbumModel>(async (t, album) =>
            {
                await DownloadAndSaveAlbumCover(album);
                AlbumCoverUpdated.FireAsync(this, album);
            }, 4);
            foreach (var album in albums)
            {
                processor.Enqueue(album);
            }
            await processor.WaitAsync();

            // Set Artist covers from Album covers
            // The artist cover will be it's newest album's cover
            Db.RunInTransaction(() =>
            {
                var artists = Db.Table<ArtistModel>().ToArray();
                foreach (var artist in artists)
                {
                    var latestAlbumByYear = Db.Table<AlbumModel>()
                        .Where(x => x.Artist == artist.Name && !string.IsNullOrEmpty(x.CoverFileName))
                        .OrderByDescending(x => x.Year)
                        .FirstOrDefault();
                    artist.CoverFileName = latestAlbumByYear?.CoverFileName;
                    Db.Update(artist);
                }
            });

            if (force)
                DeleteOrphanedCoverFiles();

            ArtistsUpdated.FireAsync(this, EventArgs.Empty);
        }

        private void DeleteOrphanedCoverFiles()
        {
            var dbCoversHashSet = new HashSet<string>();
            foreach (var cover in Db.Table<AlbumModel>()
                .Where(x => !string.IsNullOrEmpty(x.CoverFileName))
                .Select(x => x.CoverFileName))
                dbCoversHashSet.Add(cover);
            var coverFiles = Directory.GetFiles(AlbumModel.CoversDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (var fullPath in coverFiles)
            {
                var fileName = Path.GetFileName(fullPath);
                if (!dbCoversHashSet.Contains(fileName))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch { }
                }
            }
        }

        private async Task DownloadAndSaveAlbumCover(AlbumModel album)
        {
            try
            {
                string coverFileName = AlbumModel.GetCoverFileNameFromArtistAndAlbum(album.Artist, album.Name);
                string coverFilePath = AlbumModel.GetCoverFileFullPath(coverFileName);
                if (File.Exists(coverFilePath))
                {
                    // File already exists, just re-link
                    album.CoverFileName = coverFileName;
                }
                else
                {
                    // Download file
                    var data = await _audioStation.GetAlbumCoverAsync(album.Artist, album.Name);
                    if (data?.Data?.Length > 0)
                    {
                        AlbumModel.SaveCoverFile(coverFileName, data.Data);
                        album.CoverFileName = coverFileName;
                    }
                    else
                    {
                        album.CoverFileName = string.Empty;
                    }
                }
                Db.Update(album);
            }
            catch (SynologyDotNet.Core.Exceptions.SynoHttpException synoHttpException)
            {
                if (synoHttpException.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    album.CoverFileName = string.Empty;
                    Db.Update(album);
                }
                else
                {
                    _log.Error(synoHttpException, $"Unexpected error while downloading cover for: \"{album.Artist}\", \"{album.Name}\"");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unexpected error while downloading cover for: \"{album.Artist}\", \"{album.Name}\"");
            }
        }

        public string GetAlbumCover(int id)
        {
            var album = Db.Table<AlbumModel>().First(x => x.Id == id);
            return album.Cover;
        }
    }
}
