using System;
using System.Collections.Generic;
using System.Linq;

namespace SynAudio.ViewModels
{
    public interface IStatusViewModel
    {
        void Remove(StatusItemViewModel msg);
    }

    public class StatusViewModel : ViewModelBase, IStatusViewModel
    {
        /// <summary>
        /// Gets the last state instance (if any).
        /// </summary>
        public StatusItemViewModel Current { get; private set; }

        /// <summary>
        /// Contains an ordered list of state instances. The last instance will be shown on the UI.
        /// </summary>
        private readonly List<StatusItemViewModel> _items = new List<StatusItemViewModel>();

        /// <summary>
        /// Creates and registers a new state instance. Call the Dispose() method on the instance to unregister it.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public StatusItemViewModel Create(string text)
        {
            var item = new StatusItemViewModel(this, text);
            lock (_items)
            {
                _items.Add(item);
                OnChanged();
            }
            return item;
        }

        void IStatusViewModel.Remove(StatusItemViewModel msg)
        {
            lock (_items)
            {
                _items.Remove(msg);
                OnChanged();
            }
        }

        private void OnChanged()
        {
            Current = _items.Count > 0 ? _items.Last() : null;
        }
    }

    public class StatusItemViewModel : ViewModelBase, IDisposable
    {
        private readonly IStatusViewModel _owner;

        /// <summary>
        /// The text to be displayed while this state is displayed.
        /// </summary>
        public string Text { get; set; }

        public StatusItemViewModel(IStatusViewModel owner)
        {
            _owner = owner;
        }
        public StatusItemViewModel(IStatusViewModel owner, string text) : this(owner)
        {
            Text = text;
        }
        /// <summary>
        /// Unregister this state instane.
        /// </summary>
        public void Dispose()
        {
            _owner.Remove(this);
        }
    }
}
