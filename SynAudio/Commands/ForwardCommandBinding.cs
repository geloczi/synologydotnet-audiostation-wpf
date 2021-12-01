using System.Windows.Input;

namespace SynAudio.Commands
{
    /// <summary>
    /// This class is used to forward a static command to the ViewModel command instance.
    /// </summary>
    public class ForwardCommandBinding : CommandBinding
    {
        /// <summary>
        /// Forwards a command to an another one.
        /// </summary>
        /// <param name="source">The command to forward.</param>
        /// <param name="target">Source command will be forwarded to this command instance.</param>
        public ForwardCommandBinding(ICommand source, ICommand target) : base(source, (sender, e) => target.Execute(e.Parameter), (sender, e) => e.CanExecute = target.CanExecute(e.Parameter))
        {
        }
    }
}
