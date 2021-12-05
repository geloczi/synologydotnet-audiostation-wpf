using System;
using System.Threading.Tasks;

namespace Utils
{
    public static class EventHandlerExtensions
    {
        /// <summary>
        /// Invokes all subscribed event handler methods.
        /// The "Invoke" or "BeginInvoke" methods are not working with more than one subscribers, this helper is a solution for that.
        /// </summary>
        public static void Fire(this EventHandler eventHandler, object sender, EventArgs eventArgs)
        {
            if (eventHandler is null)
                return;
            foreach (EventHandler handler in eventHandler.GetInvocationList())
                handler(sender, eventArgs);
        }

        /// <summary>
        /// Invokes all subscribed event handler methods.
        /// The "Invoke" or "BeginInvoke" methods are not working with more than one subscribers, this helper is a solution for that.
        /// </summary>
        public static void Fire<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs eventArgs)
        {
            if (eventHandler is null)
                return;
            foreach (EventHandler<TEventArgs> handler in eventHandler.GetInvocationList())
                handler(sender, eventArgs);
        }

        /// <summary>
        /// Invokes all subscribed event handler methods.
        /// The "Invoke" or "BeginInvoke" methods are not working with more than one subscribers, this helper is a solution for that.
        /// </summary>
        public static void FireAsync(this EventHandler eventHandler, object sender, EventArgs eventArgs)
        {
            if (eventHandler is null)
                return;
            foreach (EventHandler handler in eventHandler.GetInvocationList())
                Task.Run(() => handler(sender, eventArgs));
        }

        /// <summary>
        /// Invokes all subscribed event handler methods.
        /// The "Invoke" or "BeginInvoke" methods are not working with more than one subscribers, this helper is a solution for that.
        /// </summary>
        public static void FireAsync<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs eventArgs)
        {
            if (eventHandler is null)
                return;
            foreach (EventHandler<TEventArgs> handler in eventHandler.GetInvocationList())
                Task.Run(() => handler(sender, eventArgs));
        }
    }
}
