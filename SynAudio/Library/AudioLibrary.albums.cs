using System.Collections.Generic;
using SynAudio.DAL;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public AlbumModel GetAlbum(int id)
        {
            _log.Debug($"{nameof(GetAlbum)}, {id}");
            var obj = Db.FindWithQuery<AlbumModel>($"WHERE {nameof(AlbumModel.Id)} = @0", id);
            return obj;
        }

        public AlbumModel[] GetAlbums(string artist)
        {
            _log.Debug($"{nameof(GetAlbums)}, \"{artist}\"");
            var result = new List<AlbumModel>();
            result.Add(new AlbumModel()
            {
                Artist = artist,
                Name = "All songs",
                Id = -1
            });

            //// Songs without album check
            //var st = TableInfo.Get<SongModel>();
            //var unknownCheckWhere = new List<string>();
            //if (string.IsNullOrEmpty(artist))
            //{
            //    unknownCheckWhere.Add($"COALESCE({st[nameof(SongModel.Artist)]}, '') = ''");
            //    unknownCheckWhere.Add($"COALESCE({st[nameof(SongModel.AlbumArtist)]}, '') = ''");
            //}
            //else
            //{
            //    unknownCheckWhere.Add($"{st[nameof(SongModel.Artist)]} = @0");
            //}
            //unknownCheckWhere.Add($"{st[nameof(SongModel.AlbumId)]} = 0");
            //if (sql.ExecuteScalar($"SELECT TOP 1 1 FROM {st} WHERE {string.Join(" AND ", unknownCheckWhere)}", artist) as int? == 1)
            //    result.Add(new AlbumModel() { Name = string.Empty, Artist = string.Empty }); // Add placeholder if there is any song without an album

            //// Query Albums
            //result.AddRange(sql.Select<AlbumModel>($"WHERE {nameof(AlbumModel.Artist)} = @0 ORDER BY {nameof(AlbumModel.Year)}, {nameof(AlbumModel.Name)}", artist));

            if (string.IsNullOrEmpty(artist))
                result.AddRange(Db.Table<AlbumModel>().ToArray());
            else
                result.AddRange(Db.Table<AlbumModel>().Where(x => x.Artist == artist).ToArray());

            return result.ToArray();
        }
    }
}
