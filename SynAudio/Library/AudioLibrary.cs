using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqlCeLibrary;
using SynAudio.DAL;
using SynAudio.Library.Exceptions;
using SynAudio.Models.Config;
using SynAudio.Utils;
using SynologyDotNet;
using SynologyDotNet.AudioStation;
using SynologyDotNet.Core.Model;
using SynologyDotNet.Core.Responses;
using Utils;

namespace SynAudio.Library
{
    public partial class AudioLibrary : IDisposable
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        public delegate void LibraryEvent(AudioLibrary sender);
        public delegate void LibraryExceptionEvent(AudioLibrary sender, Exception exception, string message);
        public delegate void SongUpdatedEvent(AudioLibrary sender, int songId);
        public delegate void SongsUpdatedEvent(AudioLibrary sender, SongModel[] songs);
        public delegate void ArtistUpdatedEvent(AudioLibrary sender, ArtistModel artist);
        public delegate void AlbumUpdatedEvent(AudioLibrary sender, AlbumModel album);

        #region [Fields]
        private const long DatabaseVersion = 3; //Increase by one after the schema changed
        private const int ApiPageSize = 5000;
        private const int DatabaseBatchSize = 1000;
        private bool _disposed;
        private AudioStationClient _audioStation;
        private SynoClient _synoClient;
        private BackgroundThreadWorker _updateCacheJob;
        private BackgroundThreadWorker _restoreBackupJob;
        private string _sqlFile;
        private readonly ViewModels.StatusViewModel _status;
        private readonly StringMultiComparer _stringComparer = new StringMultiComparer(StringComparison.OrdinalIgnoreCase, StringComparison.InvariantCultureIgnoreCase);
        #endregion

        #region [Events]
        public event LibraryExceptionEvent ExceptionThrown;
        public event LibraryEvent Updating, Updated, ArtistsUpdated, AlbumsUpdated, SyncCompleted;
        public event AlbumUpdatedEvent AlbumCoverUpdated;
        //public event SongUpdatedEvent SongAnalyzed;
        public event SongsUpdatedEvent SongsUpdated;
        #endregion

        #region [Properties]
        public static string LibraryDatabaseFile { get; } = Path.Combine(App.UserDataFolder, "library.sdf");
        public SettingsModel Settings { get; }
        public bool IsUpdatingInBackground => _updateCacheJob?.IsRunning == true;
        public bool Connected { get; private set; }
        #endregion

        #region [Public Methods]
        public AudioLibrary(SettingsModel settings, ViewModels.StatusViewModel status)
        {
            Settings = settings;
            _sqlFile = Path.Combine(LibraryDatabaseFile);
            _status = status;

            // Database initialization
            long? dbVersion = null;
            using (var sql = Sql())
            {
                if (sql.NewFileCreated)
                    sql.WriteInt64(Int64Values.DatabaseVersion, DatabaseVersion);
                else
                    dbVersion = sql.ReadInt64(Int64Values.DatabaseVersion) ?? -1;
            }
            // Re-generate the database file, if the version has been updated
            if (dbVersion.HasValue && dbVersion.Value < DatabaseVersion)
            {
                File.Delete(_sqlFile);
                using (var sql = Sql())
                    sql.WriteInt64(Int64Values.DatabaseVersion, DatabaseVersion);
            }
        }

        public async Task<bool> ConnectAsync()
        {
            _log.Debug(nameof(ConnectAsync));
            try
            {
                if (!(_audioStation is null))
                {
                    _audioStation.Dispose();
                    _audioStation = null;
                }

                if (string.IsNullOrWhiteSpace(Settings.Connection.Url))
                    throw new NullReferenceException("API url is null");

                _audioStation = new AudioStationClient();
                _synoClient = new SynoClient(new Uri(Settings.Connection.Url), true, _audioStation);

                using (var sql = Sql())
                {
                    // Login
                    SynoSession session;
                    if (sql.TryReadString(StringValues.AudioStationConnectorSession, out var json))
                    {
                        session = JsonConvert.DeserializeObject<SynoSession>(json);
                        await _synoClient.LoginWithPreviousSessionAsync(session, false);
                    }
                    else
                    {
                        session = await _synoClient.LoginAsync(Settings.Connection.Username, App.Encrypter.Decrypt(Settings.Connection.Password)).ConfigureAwait(false);
                    }

                    // Test connection
                    var response = await _audioStation.ListSongsAsync(1, 0, SynologyDotNet.AudioStation.Model.SongQueryAdditional.None).ConfigureAwait(false);
                    if (!response.Success)
                        session = null;
                    sql.WriteString(StringValues.AudioStationConnectorSession, !(session is null) ? JsonConvert.SerializeObject(session) : null);
                    Connected = response.Success;
                }
            }
            catch (Exception ex)
            {
                Connected = false;
                _log.Error(ex);
            }
            return Connected;
        }

        public void Logout()
        {
            _log.Debug(nameof(Logout));
            Connected = false;
            using (var sql = Sql())
                sql.WriteString(StringValues.AudioStationConnectorSession, null);
        }

        public void Dispose()
        {
            _log.Debug(nameof(Dispose));
            if (_disposed)
                return;
            _disposed = true;
            CleanUpStreaming();
            _restoreBackupJob?.Cancel();
            _updateCacheJob?.Cancel();
            Connected = false;
            _audioStation.Dispose();
            _synoClient.Dispose();
        }

        #endregion

        #region [Private Methods]
        private static SqlCe Sql() => App.GetSql();
        private MultiComparerStringHashSet CreateMultiComparerStringHashSet() => new MultiComparerStringHashSet(StringComparer.OrdinalIgnoreCase, StringComparer.InvariantCultureIgnoreCase);
        private MultiComparerStringDictionary<T> CreateMultiComparerStringDictionary<T>() => new MultiComparerStringDictionary<T>(StringComparer.OrdinalIgnoreCase, StringComparer.InvariantCultureIgnoreCase);
        private string EscapeSqlLikeString(string s) => s.Replace("%", "[%]");

        private void OnException(Exception ex, string message = null)
        {
            if (!string.IsNullOrEmpty(message))
                _log.Error(ex, message);
            else
                _log.Error(ex);
            ExceptionThrown?.BeginInvoke(this, ex, message, null, null);
        }
        private static byte[] CompressImage(byte[] data, int quality)
        {
            var img = BitmapImageHelper.LoadImageFromBytes(data);
            return BitmapImageHelper.SaveJpgToByteArray(img, quality);
        }

        private static void GuardResponse(IApiResponse response)
        {
            if (response is null)
                throw new LibraryResponseException(-1);
            else if (!response.Success)
                throw new LibraryResponseException(response.Error.Code);
        }
        #endregion
    }
}
