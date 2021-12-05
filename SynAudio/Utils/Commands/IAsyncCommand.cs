using System.Threading.Tasks;
using System.Windows.Input;

namespace Utils.Commands
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }
}
