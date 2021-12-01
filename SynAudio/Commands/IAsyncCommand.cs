using System.Threading.Tasks;
using System.Windows.Input;

namespace SynAudio.Commands
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }
}
