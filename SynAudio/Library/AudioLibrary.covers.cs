using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqlCeLibrary;
using SynAudio.DAL;
using Utils;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        private async Task SyncCoversAsync(CancellationToken token, bool force)
        {
            //       // Download album covers
            //       var alb = TableInfo.Get<AlbumModel>();
            //       var processor = new QueueProcessorTasks<AlbumModel>(async (t, album) =>
            //       {
            //           await DownloadAndSaveAlbumCover(sql, album);
            //           AlbumCoverUpdated.FireAsync(this, album);
            //       }, 4);

            //       List<AlbumModel> albums;
            //       if (force)
            //       {
            //           // Get albums without cover (with retry)
            //           albums = sql.Select<AlbumModel>($"WHERE {alb[nameof(AlbumModel.CoverFileState)]} <> 1 ORDER BY {alb[nameof(AlbumModel.InsertDate)]} DESC, {alb[nameof(AlbumModel.Name)]} ASC").ToList();
            //           // Get albums without the cover file (missing file)
            //           foreach (var album in sql.Select<AlbumModel>($"WHERE {alb[nameof(AlbumModel.CoverFileState)]} = 1 ORDER BY {alb[nameof(AlbumModel.InsertDate)]} DESC, {alb[nameof(AlbumModel.Name)]}"))
            //           {
            //               var path = album.GetCoverFileFullPath();
            //               if (!File.Exists(path))
            //               {
            //                   albums.Add(album);
            //                   _log.Warn($"Missing cover file: {Path.GetFileName(path)}");
            //               }
            //           }
            //       }
            //       else
            //       {
            //           // Get albums without cover (did not try download yet)
            //           albums = sql.Select<AlbumModel>($"WHERE {alb[nameof(AlbumModel.CoverFileState)]} = 0 ORDER BY {alb[nameof(AlbumModel.InsertDate)]} DESC, {alb[nameof(AlbumModel.Name)]}").ToList();
            //       }

            //       foreach (var album in albums)
            //       {
            //           var existingCover = album.TryToFindCoverFile();
            //           if (existingCover)
            //           {
            //               sql.Update(album, nameof(AlbumModel.CoverFileState));
            //               AlbumCoverUpdated.FireAsync(this, album);
            //           }
            //           else
            //           {
            //               processor.Enqueue(album);
            //           }
            //       }
            //       await processor.WaitAsync();

            //       // Generate Artist covers from Album covers
            //       // The artist cover will be the newest album's cover inside that artist
            //       var art = TableInfo.Get<ArtistModel>();
            //       var artistCovers = sql.SelectCustom<ArtistModel>(
            //           $@"SELECT 
            //	a.{art[nameof(ArtistModel.Name)]}, 
            //	coveralb.{alb[nameof(AlbumModel.Id)]} AS {art[nameof(ArtistModel.CoverAlbumId)]}
            //FROM {art.NameWithBrackets} a
            //OUTER APPLY (
            //	SELECT TOP 1 alb.{alb[nameof(AlbumModel.Id)]} 
            //	FROM {alb.NameWithBrackets} alb
            //	WHERE alb.{alb[nameof(AlbumModel.Artist)]} = a.Name AND alb.{alb[nameof(AlbumModel.CoverFileState)]} = 1
            //	ORDER BY alb.Year DESC
            //) coveralb
            //WHERE coveralb.{alb[nameof(AlbumModel.Id)]} <> COALESCE(a.{art[nameof(ArtistModel.CoverAlbumId)]}, -1)
            //ORDER BY a.{art[nameof(ArtistModel.Name)]}");
            //       if (artistCovers.Any())
            //       {
            //           sql.Update(artistCovers.ToArray(), new[] { nameof(ArtistModel.CoverAlbumId) });
            //           ArtistsUpdated.FireAsync(this, EventArgs.Empty);
            //       }
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
                DB.Update(album);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to save Album cover: \"{album.Artist}\", \"{album.Name}\"");
            }
        }

        public string GetAlbumCover(int id)
        {
            //TODO
            //using (var sql = Sql())
            //{
            //    var a = TableInfo.Get<AlbumModel>();
            //    var stateObj = sql.ExecuteScalar($"SELECT TOP 1 {a[nameof(AlbumModel.CoverFileState)]} FROM {a} a WHERE a.{a[nameof(AlbumModel.Id)]} = @0", id);
            //    if (stateObj is int state && state == 1)
            //        return AlbumModel.GetCoverFileFullPath(id);
            //}
            return null;
        }

        //private static bool ProcessCoverDownloadResult(ByteArrayData data, out byte[] thumbImage, out byte[] largeImage)
        //{
        //	thumbImage = largeImage = null;
        //	if (data?.Data?.Length > 0)
        //	{
        //		using (var img = ImageHelper.ImageFromByteArray(data.Data))
        //		{
        //			// Large (no upper limit for image dimensions, just compress it to JPG)
        //			using (var ms = new MemoryStream())
        //			{
        //				ImageHelper.SaveJpg(img, 90, ms);
        //				largeImage = ms.ToArray();
        //			}

        //			// Thumb
        //			using (var resized = ImageHelper.FitImageIn(img, new System.Drawing.Size(300, 300), System.Drawing.Drawing2D.PixelOffsetMode.HighQuality))
        //			using (var ms = new MemoryStream())
        //			{
        //				ImageHelper.SaveJpg(resized, 80, ms);
        //				thumbImage = ms.ToArray();
        //			}
        //		}
        //		return true;
        //	}
        //	return false;
        //}
    }
}
