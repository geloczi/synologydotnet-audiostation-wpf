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
                    if (album.CoverFile != ResourceState.Exists)
                    {
                        // Albums without cover (with retry download)
                        albums.Add(album);
                    }
                    else
                    {
                        // If the cover file does not exist
                        var path = album.GetCoverFileFullPath();
                        if (!File.Exists(path))
                            albums.Add(album);
                    }
                }
            }
            else
            {
                // Get albums without cover (haven't tried to download yet)
                albums = Db.Table<AlbumModel>().Where(x => x.CoverFile == ResourceState.NotSet).ToList();
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
                if (album.TryToFindCoverFile())
                {
                    // Cover file re-link
                    Db.Update(album);
                    AlbumCoverUpdated.FireAsync(this, album);
                }
                else
                {
                    // Has to be downloaded
                    processor.Enqueue(album);
                }
            }
            await processor.WaitAsync();

            // Generate Artist covers from Album covers
            // The artist cover will be the newest album's cover inside that artist
            var artists = Db.Table<ArtistModel>().ToArray();
            foreach (var artist in artists)
            {
                var latestAlbumByYear = Db.Table<AlbumModel>().Where(x => x.Artist == artist.Name).OrderByDescending(x => x.Year).FirstOrDefault();
                if (latestAlbumByYear is null)
                {
                    artist.CoverAlbumId = null;
                }
                else
                {
                    artist.CoverAlbumId = latestAlbumByYear.Id;
                }
                Db.Update(artist);
            }

            ArtistsUpdated.FireAsync(this, EventArgs.Empty);
        }

        private async Task DownloadAndSaveAlbumCover(AlbumModel album)
        {
            try
            {
                var data = await _audioStation.GetAlbumCoverAsync(album.Artist, album.Name);
                if (data?.Data?.Length > 0)
                    album.SaveCover(data.Data);
                else
                    album.DeleteCover();
                Db.Update(album);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to save Album cover: \"{album.Artist}\", \"{album.Name}\"");
            }
        }

        public string GetAlbumCover(int id)
        {
            var album = Db.Table<AlbumModel>().First(x => x.Id == id);
            return album.Cover;
        }
    }
}
