using System.Threading;

namespace Utils
{
    public class WorkerMethodParameter
    {
        public CancellationToken Token { get; set; }
        public object Data { get; set; }
    }
}
