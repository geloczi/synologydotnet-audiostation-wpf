using System;
using System.Threading.Tasks;

namespace SynAudio.Utils
{
    public interface IErrorHandler
    {
        void HandleError(Exception ex);
    }

    public static class TaskUtilities
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public static async void FireAndForgetSafe(this Task task, IErrorHandler handler = null)
        {
            try
            {
                await task; //Await the task here to catch the exceptions.
            }
            catch (Exception ex)
            {
                if (!(handler is null))
                    handler.HandleError(ex);
                else
                    _log.Error(ex);
            }
        }
    }
}
