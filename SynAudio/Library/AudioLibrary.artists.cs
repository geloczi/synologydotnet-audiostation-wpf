using System.Collections.Generic;
using SqlCeLibrary;
using SynAudio.DAL;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public ArtistModel[] GetArtists()
        {
            _log.Debug(nameof(GetArtists));
            using (var sql = Sql())
            {
                var result = new List<ArtistModel>();
                // Songs without artists
                var st = TableInfo.Get<SongModel>();
                if (sql.ExecuteScalar($"SELECT TOP 1 1 FROM {st} WHERE {st[nameof(SongModel.Artist)]} = '' AND {st[nameof(SongModel.AlbumArtist)]} = ''") as int? == 1)
                    result.Add(new ArtistModel() { Name = string.Empty });
                result.AddRange(sql.Select<ArtistModel>());
                return result.ToArray();
            }
        }
    }
}
