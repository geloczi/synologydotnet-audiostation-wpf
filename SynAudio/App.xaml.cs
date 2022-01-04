using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using SQLite;
using SynAudio.Models.Config;
using Utils;
using Utils.ObjectStorage;

namespace SynAudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region [Fields]

        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly object _configLock = new object();
        private Mutex _mutex = null;

#if DEBUG
        // Use a different data folder in DEBUG mode
        private const string DevSuffix = "_dev";
        private static readonly string MutexName = nameof(SynAudio) + "_" + AssemblyProps.EntryAssembly.ProductGuid + DevSuffix;
        internal static readonly string UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SynAudio) + DevSuffix);
#else
        private static readonly string MutexName = nameof(SynAudio) + "_" + AssemblyProps.EntryAssembly.ProductGuid;
        internal static readonly string UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SynAudio));
#endif

        private static readonly string ProgramFolder = Path.GetDirectoryName(AssemblyProps.EntryAssembly.Location);

        private static readonly IObjectStorage UserFolderStorage = new JsonStorage(UserDataFolder);

        private static readonly IObjectStorage ProgramFolderStorage = new JsonStorage(ProgramFolder);

        #endregion

        #region [Properties]

        internal static Encryption.Encrypter Encrypter { get; } = new Encryption.Encrypter("spGnKhrqZ6-VFuQ3TiRdEXvCo7AbBaeWfHy48_mxMLNgctz29j", Encoding.UTF8.GetBytes(Environment.UserName));
        internal static IObjectStorage ConfigStorage { get; private set; } = UserFolderStorage;
        internal static SQLiteConnection Db { get; private set; }
        internal static SqlLiteSettingsRepository DbSettings { get; private set; }
        internal static Random Rnd { get; } = new Random();

        private static SettingsModel _config;
        internal static SettingsModel Config
        {
            get
            {
                if (_config is null)
                {
                    lock (_configLock)
                    {
                        if (_config is null)
                        {
                            SettingsModel cfg;
                            if (ProgramFolderStorage.TryLoad<SettingsModel>("config", out cfg))
                            {
                                // Use config file from program folder
                                ConfigStorage = ProgramFolderStorage;
                            }
                            else
                            {
                                // Use config from user's app data folder
                                if (!UserFolderStorage.TryLoad<SettingsModel>("config", out cfg))
                                    cfg = new SettingsModel();
                            }
                            _config = cfg;
                        }
                    }
                }
                return _config;
            }
        }

        internal static bool MusicFolderAvailableOnLan { get; set; }

        #endregion

        #region [Imports]
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("USER32.DLL")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion

        internal static void BringToFront(string title)
        {
            _log.Debug($"{nameof(BringToFront)}, {title}");
            IntPtr handle = FindWindow(null, title);
            if (handle == IntPtr.Zero)
                return;
            SetForegroundWindow(handle);
        }

        internal static void RefreshCommands()
        {
            Current.Dispatcher.BeginInvoke(new Action(() => System.Windows.Input.CommandManager.InvalidateRequerySuggested()));
        }

        //internal static string GetNasFileFullUncPath(string internalPath) => NetworkHelper.GetUncPath(MusicFolderPath, internalPath);

        internal static bool ExistsOnHost(string path, out string uncPath)
        {
            uncPath = null;
            // Todo
            //if (MusicFolderAvailableOnLan)
            //{
            //    uncPath = GetNasFileFullUncPath(path);
            //    return File.Exists(uncPath);
            //}
            return false;
        }

        internal static void SaveSettings()
        {
            ConfigStorage.Save("config", Config);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out var createdNewMutex);
            if (!createdNewMutex)
            {
                if (MessageBox.Show($"Do you want to force close and re-open it?",
                    $"{AssemblyProps.EntryAssembly.Product} is already running", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (Process currentProc = Process.GetCurrentProcess())
                    {
                        var otherProcess = Process.GetProcessesByName(nameof(SynAudio)).Where(p => p.Id != currentProc.Id).FirstOrDefault();
                        if (!(otherProcess is null))
                        {
                            otherProcess.Kill();
                            otherProcess.WaitForExit();
                            _mutex.ReleaseMutex();
                            _mutex.Dispose();
                            _mutex = new Mutex(true, MutexName, out createdNewMutex); // The old instance has been terminted, and the current process continues
                        }
                    }
                }
                else
                {
                    BringToFront(ViewModels.MainWindowViewModel.OriginalWindowTitle);
                }

                // Exit, if could not create a new mutex
                if (!createdNewMutex)
                {
                    _mutex = null;
                    Environment.Exit(0);
                    return;
                }
            }

            // Application can start, ensure directories
            if (!Directory.Exists(UserDataFolder))
                Directory.CreateDirectory(UserDataFolder);
            if (!Directory.Exists(DAL.AlbumModel.CoversDirectory))
                Directory.CreateDirectory(DAL.AlbumModel.CoversDirectory);

            SetupLogging();
            SetupExceptionHandling();

            try
            {
                // Set process to high priority
                using (var p = Process.GetCurrentProcess())
                {
                    p.PriorityClass = ProcessPriorityClass.AboveNormal;
                }

                // Start event
                _log.Info($"{nameof(OnStartup)}, {AssemblyProps.EntryAssembly.Product} v{AssemblyProps.EntryAssembly.Version}");
                base.OnStartup(e);

                // Catch binding errors
                PresentationTraceSources.Refresh();
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorTraceListener());
                DispatcherUnhandledException += App_DispatcherUnhandledException;

                // Prepare database
                EnsureDatabase();

                // Start GUI
                new MainWindow().Show();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
#if DEBUG
                Debugger.Break();
#endif
            }
        }

        private static void SetupLogging()
        {
            var logConfig = new NLog.Config.LoggingConfiguration();

            // To file
            var fileTarget = new NLog.Targets.FileTarget()
            {
                FileName = Path.Combine(UserDataFolder, $"logs\\{nameof(SynAudio)}_{DateTime.Now.ToString("yyyyMMdd")}.log"),
                CreateDirs = true,
                Encoding = Encoding.UTF8,
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss.fff}|${threadid}|${level}|${logger}|${message}|${exception:format=toString}"
            };
#if DEBUG
            logConfig.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, fileTarget);
#else
            logConfig.AddRule(NLog.LogLevel.Warn, NLog.LogLevel.Fatal, fileTarget);
#endif

#if DEBUG
            // To debug console
            logConfig.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new NLog.Targets.DebuggerTarget(nameof(NLog.Targets.DebuggerTarget)));
#endif

            // To syslog server
            try
            {
                if (!string.IsNullOrWhiteSpace(Config.LogToSyslog?.Server))
                {
                    var syslogTarget = new NLog.Targets.Syslog.SyslogTarget()
                    {
                        Name = Config.LogToSyslog.Name ?? "synaudio",
                        MessageSend = new NLog.Targets.Syslog.Settings.MessageTransmitterConfig()
                        {
                            Protocol = NLog.Targets.Syslog.Settings.ProtocolType.Udp,
                            Udp = new NLog.Targets.Syslog.Settings.UdpConfig()
                            {
                                Server = Config.LogToSyslog.Server,
                                Port = Config.LogToSyslog.Port
                            }
                        }
                    };
                    logConfig.AddRule(NLog.LogLevel.FromString(Config.LogToSyslog.Level ?? "Info"), NLog.LogLevel.Fatal, syslogTarget);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Syslog configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            NLog.LogManager.Configuration = logConfig;
        }

        private static void EnsureDatabase()
        {
            var dbFile = Path.Combine(UserDataFolder, "db.sqlite3");
            bool isEmptyDb = !File.Exists(dbFile);
            Db = new SQLiteConnection(dbFile, true);
            if (isEmptyDb)
            {
                Db.CreateTable<DAL.ArtistModel>();
                Db.CreateTable<DAL.AlbumModel>();
                Db.CreateTable<DAL.ByteArrayValue>();
                Db.CreateTable<DAL.Int64Value>();
                Db.CreateTable<DAL.NowPlayingItem>();
                Db.CreateTable<DAL.SongBackup>();
                Db.CreateTable<DAL.SongModel>();
                Db.CreateTable<DAL.StringValue>();
            }
            DbSettings = new SqlLiteSettingsRepository(Db);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _log.Error(e.Exception);
#if DEBUG
            Console.Error.WriteLine(e.Exception.ToString());
            //Debugger.Break();
#endif
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _log.Info(nameof(OnExit));
            SaveSettings();
            Db.Close();
            Db.Dispose();
            NLog.LogManager.Shutdown();
            if (_mutex != null)
                _mutex.ReleaseMutex();
            base.OnExit(e);
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            };
            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            var message = $"Unhandled exception ({source})";
            try
            {
                var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Exception in {nameof(LogUnhandledException)}");
            }
            finally
            {
                _log.Error(exception, message);
            }
#if DEBUG
            Console.Error.WriteLine(exception.ToString());
            //Debugger.Break();
#endif
        }
    }
}