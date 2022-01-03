using System;
using System.Diagnostics;

namespace Utils
{
    public class BindingErrorTraceListener : TraceListener
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        public override void Write(string s) { }
        public override void WriteLine(string message)
        {
#if DEBUG
            _log.Debug(message);
            Console.Error.WriteLine($"BINDING_ERROR: {message}");
            //Debugger.Break(); //Binding error, check the value of the "message" variable.
#endif
        }
    }
}