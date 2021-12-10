using System;
using System.Collections.Generic;
using System.Linq;
using SynAudio.DAL;
using SynAudio.Models;

namespace SynAudio.Library
{
    public partial class AudioLibrary
    {
        public FolderContentsModel GetFolderContents(string parentFolder)
        {
            if (parentFolder is null)
                parentFolder = string.Empty;
            else if (parentFolder.EndsWith("/"))
                throw new ArgumentException("The folder path must not end with '/'", nameof(parentFolder));
            var songs = new List<SongModel>();
            var folders = new HashSet<string>();
            var pathFilter = parentFolder + '/';

            foreach (var song in Db.Table<SongModel>().Where(x => x.Path.StartsWith(pathFilter)).ToArray())
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

            return new FolderContentsModel()
            {
                Songs = songs.OrderBy(x => x.Path).ToArray(),
                Folders = folders.OrderBy(x => x).ToArray()
            };
        }

        public SongModel[] GetSongsInFolderRecursively(string parentFolder)
        {
            if (parentFolder.EndsWith("/"))
                throw new ArgumentException("The folder path must not end with '/'", nameof(parentFolder));

            var pathFilter = parentFolder + '/';
            return Db.Table<SongModel>().Where(x => x.Path.StartsWith(pathFilter)).ToArray();
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
