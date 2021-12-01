using System;
using System.Collections.Generic;
using System.Linq;
using SqlCeLibrary;
using SynAudio.DAL;
using SynAudio.Models;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public string[] GetFolders(string parentFolder)
        {
            if (parentFolder.EndsWith("/"))
                throw new ArgumentException("The folder path must not end with '/'", nameof(parentFolder));

            var folders = new HashSet<string>();
            using (var sql = Sql())
            {
                var t = TableInfo.Get<SongModel>();
                sql.ExecuteReader($"SELECT {t[nameof(SongModel.Path)]} FROM {t} WHERE {t[nameof(SongModel.Path)]} LIKE '{EscapeSqlLikeString(parentFolder)}%'", r =>
                {
                    if (IsSubfolder(parentFolder, r.GetString(0), out var subfolder))
                        folders.Add(subfolder);
                });
            }
            return folders.OrderBy(x => x).ToArray();
        }

        public FolderContentsModel GetFolderContents(string parentFolder)
        {
            if (parentFolder is null)
                parentFolder = string.Empty;
            else if (parentFolder.EndsWith("/"))
                throw new ArgumentException("The folder path must not end with '/'", nameof(parentFolder));
            var songs = new List<SongModel>();
            var folders = new HashSet<string>();
            using (var sql = Sql())
            {
                var t = TableInfo.Get<SongModel>();
                foreach (var song in sql.Select<SongModel>($"WHERE {t[nameof(SongModel.Path)]} LIKE '{EscapeSqlLikeString(parentFolder) + '/'}%'"))
                {
                    if (IsSubfolder(parentFolder, song.Path, out var subfolder))
                    {
                        folders.Add(subfolder);
                    }
                    else
                    {
                        songs.Add(song);
                        song.LoadCustomizationFromCommentTag();
                    }
                }
            }
            return new FolderContentsModel()
            {
                Songs = songs.OrderBy(x => x.Path).ToArray(),
                Folders = folders.OrderBy(x => x).ToArray()
            };
        }

        public SongModel[] GetSongsInFolder(string parentFolder)
        {
            if (parentFolder.EndsWith("/"))
                throw new ArgumentException("The folder path must not end with '/'", nameof(parentFolder));

            SongModel[] songs;
            using (var sql = Sql())
            {
                var t = TableInfo.Get<SongModel>();
                songs = sql.Select<SongModel>($"WHERE {t[nameof(SongModel.Path)]} LIKE '{EscapeSqlLikeString(parentFolder) + '/'}%'")
                    .Where(song => song.Path.Remove(0, parentFolder.Length + 1).IndexOf('/') == -1)
                    .ToArray();
            }
            foreach (var song in songs)
                song.LoadCustomizationFromCommentTag();
            return songs;
        }

        public SongModel[] GetSongsInFolderRecursively(string parentFolder)
        {
            if (parentFolder.EndsWith("/"))
                throw new ArgumentException("The folder path must not end with '/'", nameof(parentFolder));

            SongModel[] songs;
            using (var sql = Sql())
            {
                var t = TableInfo.Get<SongModel>();
                songs = sql.Select<SongModel>($"WHERE {t[nameof(SongModel.Path)]} LIKE '{EscapeSqlLikeString(parentFolder) + '/'}%'");
            }
            foreach (var song in songs)
                song.LoadCustomizationFromCommentTag();
            return songs;
        }

        private static bool IsSubfolder(string parentFolder, string path, out string subFolder)
        {
            var sub = path.Remove(0, parentFolder.Length + 1);
            var lastSlashIndex = sub.IndexOf('/');
            if (lastSlashIndex >= 0)
            {
                sub = sub.Substring(0, sub.IndexOf('/'));
                subFolder = parentFolder + '/' + sub;
                return true;
            }
            subFolder = null;
            return false;
        }
    }
}
