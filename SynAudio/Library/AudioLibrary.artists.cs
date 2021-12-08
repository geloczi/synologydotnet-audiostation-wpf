using System.Collections.Generic;
using SynAudio.DAL;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public ArtistModel[] GetArtists()
        {
            _log.Debug(nameof(GetArtists));
         
            var result = new List<ArtistModel>();
            result.Add(new ArtistModel() { Name = string.Empty });

            //// Songs without artists
            //var st = TableInfo.Get<SongModel>();
            //if (sql.ExecuteScalar($"SELECT TOP 1 1 FROM {st} WHERE {st[nameof(SongModel.Artist)]} = '' AND {st[nameof(SongModel.AlbumArtist)]} = ''") as int? == 1)
            //    result.Add(new ArtistModel() { Name = string.Empty });
            //result.AddRange(sql.Select<ArtistModel>());

            result.AddRange(DB.Table<ArtistModel>().ToArray());
            return result.ToArray();
        }
    }
}
