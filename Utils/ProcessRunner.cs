using System;
using System.Diagnostics;
using System.IO;

namespace Utils
{
    public class ProcessRunner : IDisposable
    {
        public delegate void ProcessRunnerEvent(ProcessRunner o);
        public event ProcessRunnerEvent Exited;


        private ProcessStartInfo _startInfo;
        private Process _proc;

        public StreamReader StandardOutput => _proc?.StandardOutput;
        public StreamReader StandardError => _proc?.StandardError;
        public int? ExitCode => _proc?.ExitCode;
        public bool IsRunning => _proc?.HasExited != true;
        public object Tag { get; set; }

        public ProcessRunner(string command) : this(command, null, null) { }
        public ProcessRunner(string command, string arguments) : this(command, arguments, null) { }
        public ProcessRunner(string command, string arguments, string workingDirectory)
        {
            if (string.IsNullOrEmpty(arguments))
                _startInfo = new ProcessStartInfo(command);
            else
                _startInfo = new ProcessStartInfo(command, arguments);

            if (!string.IsNullOrEmpty(workingDirectory))
                _startInfo.WorkingDirectory = workingDirectory;

            _startInfo.RedirectStandardOutput = true;
            _startInfo.RedirectStandardError = true;
            _startInfo.UseShellExecute = false;
            _startInfo.CreateNoWindow = true;
        }

        public void Start()
        {
            if (!(_proc is null))
                throw new InvalidOperationException("Already started.");
            _proc = new Process();
            _proc.StartInfo = _startInfo;
            _proc.Exited += _proc_Exited;
            _proc.Start();
        }

        private void _proc_Exited(object sender, EventArgs e) => Exited?.Invoke(this);

        public void Kill()
        {
            if (_proc is null)
                throw new ObjectDisposedException(nameof(ProcessRunner));
            if (!_proc.HasExited)
                _proc.Kill();
        }

        public void Wait()
        {
            if (_proc is null)
                throw new ObjectDisposedException(nameof(ProcessRunner));
            if (!_proc.HasExited)
                _proc.WaitForExit();
        }

        public void Dispose()
        {
            if (!(_proc is null))
            {
                Kill();
                _proc.Exited -= _proc_Exited;
                _proc.Dispose();
                _proc = null;
            }
        }
    }
}
