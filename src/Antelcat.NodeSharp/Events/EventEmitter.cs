using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Handler = System.Func<object?[], System.Threading.Tasks.Task>;

namespace Antelcat.NodeSharp.Events;

public class EventEmitter : IEventEmitter
{
    public static int DefaultMaxListeners { get; set; } = 10;
    
    /// <summary>
    /// The <see cref="EventEmitter"/> instance will emit its own <see cref="EventEmitter.NewListener"/> event before
    /// a listener is added to its internal array of listeners.
    /// Listeners registered for the <see cref="EventEmitter.NewListener"/> event are passed the event name and a reference
    /// to the listener being added.
    /// The fact that the event is triggered before adding the listener has a subtle but important side effect:
    /// any additional listeners registered to the same name within the <see cref="EventEmitter.NewListener"/> callback
    /// are inserted before the listener that is in the process of being added.
    /// </summary>
    public event Action<string, Handler>? NewListener;
    
    /// <summary>
    /// The <see cref="EventEmitter.RemovedListener"/> event is emitted after the listener is removed.
    /// </summary>
    public event Action<string, Handler>? RemovedListener;

    /// <summary>
    /// Occurred when listener throws an error
    /// </summary>
    public event Action<string, Exception>? EmitError;

    /// <summary>
    /// <inheritdoc cref="On"/>
    /// </summary>
    /// <param name="eventName"><inheritdoc cref="On"/></param>
    /// <param name="listener"><inheritdoc cref="On"/></param>
    /// <returns></returns>
    public IEventEmitter AddListener(string eventName, Handler listener) => On(eventName, listener);

    /// <summary>
    /// Synchronously calls each of the listeners registered for the event named eventName, in the order they were registered,
    /// passing the supplied arguments to each.
    /// Returns true if the event had listeners, false otherwise.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="args"></param>
    /// <returns>true if the event had listeners, false otherwise.</returns>
    public bool Emit(string eventName, params object?[] args)
    {
        locker.EnterReadLock();
        var @event = Get(eventName);
        if (@event is null || @event.Count() == 0)
        {
            locker.ExitReadLock();
            return false;
        }
        @event.Emit(args)?.ContinueWith(task => EmitError?.Invoke(eventName, task.Exception), TaskContinuationOptions.OnlyOnFaulted);
        locker.ExitReadLock();
        return true;
    }

    /// <summary>
    /// Returns an array listing the events for which the emitter has registered listeners. The values in the array are strings.
    /// </summary>
    public IReadOnlyCollection<string> EventNames => handlers.Keys;

    /// <summary>
    /// The current max listener value for the <see cref="EventEmitter"/> which is defaults to <see cref="DefaultMaxListeners"/>
    /// </summary>
    public int MaxListeners { get; set; } = DefaultMaxListeners;

    /// <summary>
    /// Returns the number of listeners listening for the event named eventName. If listener is provided,
    /// it will return how many times the listener is found in the list of the listeners of the event.
    /// </summary>
    /// <param name="eventName">name of the event</param>
    /// <param name="listener"></param>
    /// <returns></returns>
    public int ListenerCount(string eventName, Handler? listener = null) => Get(eventName)?.Count(listener) ?? 0;

    /// <summary>
    /// Returns a copy of the array of listeners for the event named eventName.
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public IEnumerable<Handler> Listeners(string eventName) => Get(eventName)?.Listeners ?? [];

    /// <summary>
    /// <see cref="RemoveListener"/>
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns></returns>
    public IEventEmitter Off(string eventName, Handler listener)
    {
        locker.EnterWriteLock();
        if (handlers.TryGetValue(eventName, out var @event))
        {
            @event.Off(listener);
            RemovedListener?.Invoke(eventName, listener);
            if (!@event.Listeners.Any()) handlers.Remove(eventName);
        }

        locker.ExitWriteLock();
        return this;
    }

    /// <summary>
    /// Adds the listener function to the end of the listeners array for the event named eventName.
    /// No checks are made to see if the listener has already been added.
    /// Multiple calls passing the same combination of eventName and listener will result in the listener being added,
    /// and called, multiple times.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    public IEventEmitter On(string eventName, Handler listener)
    {
        locker.EnterWriteLock();
        WillAdd(eventName, listener)?.On(listener);
        locker.ExitWriteLock();
        return this;
    }

    /// <summary>
    /// Adds a one-time listener function for the event named eventName. The next time eventName is triggered,
    /// this listener is removed and then invoked.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    public IEventEmitter Once(string eventName, Handler listener)
    {
        locker.EnterWriteLock();
        WillAdd(eventName, listener)?.Once(listener,
            () => RemovedListener?.Invoke(eventName, listener),
            ex => EmitError?.Invoke(eventName, ex));
        locker.ExitWriteLock();
        return this;
    }

    /// <summary>
    /// Adds the listener function to the beginning of the listeners array for the event named eventName.
    /// No checks are made to see if the listener has already been added.
    /// Multiple calls passing the same combination of eventName and listener will result in the listener being added,
    /// and called, multiple times.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    public IEventEmitter PrependListener(string eventName, Handler listener)
    {
        locker.EnterWriteLock();
        WillAdd(eventName, listener)?.Prepend(listener);
        locker.ExitWriteLock();
        return this;
    }

    /// <summary>
    /// Adds a one-time listener function for the event named eventName to the beginning of the listeners array.
    /// The next time eventName is triggered, this listener is removed, and then invoked.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    public IEventEmitter PrependOnceListener(string eventName, Handler listener)
    {
        locker.EnterWriteLock();
        WillAdd(eventName, listener)?.PrependOnce(listener,
            () => RemovedListener?.Invoke(eventName, listener),
            ex => EmitError?.Invoke(eventName, ex));
        locker.ExitWriteLock();
        return this;
    }

    /// <summary>
    /// Removes all listeners, or those of the specified eventName.
    /// It is bad practice to remove listeners added elsewhere in the code,
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    public IEventEmitter RemoveAllListeners(string eventName)
    {
        locker.EnterWriteLock();
        handlers.Remove(eventName);
        locker.ExitWriteLock();
        return this;
    }

    /// <summary>
    /// Removes the specified listener from the listener array for the event named eventName.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    public IEventEmitter RemoveListener(string eventName, Handler listener) => Off(eventName, listener);

    /// <summary>
    /// Returns a copy of the array of listeners for the event named eventName, including any wrappers (such as those created by .once()).
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public IEnumerable<Handler> RawListeners(string eventName) => Get(eventName)?.RawListeners ?? [];

    private readonly ReaderWriterLockSlim      locker   = new();
    private readonly Dictionary<string, Event> handlers = [];
    
    private Event? CheckEvent(Event @event) => @event.Count() < MaxListeners ? @event : null;
    
    private Event? WillAdd(string eventName, Handler listener)
    {
        var @event = CheckEvent(GetOrCreate(eventName));
        if (@event is null) return null;
        NewListener?.Invoke(eventName, listener);
        return @event;
    }

    private Event GetOrCreate(string eventName)
    {
        if (handlers.TryGetValue(eventName, out var @event)) return @event;
        @event              = new Event();
        handlers[eventName] = @event;
        return @event;
    }

    private Event? Get(string eventName) =>
#if NETSTANDARD
        handlers.GetValueOrDefault(eventName);
#else
        handlers.TryGetValue(eventName, out var @event) ? @event : null;
#endif

    private class Event
    {
        private readonly object        locker    = new();
        private readonly List<Wrapper> listeners = [];
        private event Handler?         Events;

        public void Prepend(Handler listener)                   => Prepend(listener, listener);
        public void On(Handler listener)                        => On(listener, listener);

        public void PrependOnce(Handler listener, Action notifyRemove, Action<Exception> notifyError) =>
            Prepend(listener, MakeOnce(listener, notifyRemove, notifyError));

        public void Once(Handler listener, Action notifyRemove, Action<Exception> notifyError) =>
            On(listener, MakeOnce(listener, notifyRemove, notifyError));

        private Handler MakeOnce(Handler listener, Action notifyRemove, Action<Exception> notifyError)
        {
            Handler once = null!;
            once = async args =>
            {
                if (!Remove(listener)) return;
                Events -= once;
                notifyRemove();
                try
                {
                    await listener(args);
                }
                catch (Exception ex)
                {
                    notifyError(ex);
                }
            };
            return once;
        }

        public Task? Emit(params object?[] args)
        {
            lock (locker)
            {
                try
                {
                    return Events?.Invoke(args);
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
        }

        public void Off(Handler listener) => Remove(listener);

        public int Count(Handler? listener = null) =>
            listener is null ? listeners.Count : listeners.Count(x => x.Origin == listener);

        public IEnumerable<Handler> Listeners => listeners.Select(static x => x.Origin);

        public IEnumerable<Handler> RawListeners => listeners.Select(static x => x.Raw);


        private void On(Handler source, Handler call)
        {
            var handler = new Wrapper(source, call);
            lock (locker)
            {
                listeners.Add(handler);
                Events += handler.Raw;
            }
        }

        private void Prepend(Handler source, Handler call)
        {
            var handler = new Wrapper(source, call);
            lock (locker)
            {
                listeners.Add(handler);
                Events = handler.Raw + Events;
            }
        }

        private bool Remove(Handler listener)
        {
            var remove = listeners.FirstOrDefault(x => x.Origin == listener);
            if (remove is null) return false;
            lock (locker)
            {
                return listeners.Remove(remove);
            }
        }
    }

    private class Wrapper(Handler origin, Handler raw)
    {
        public Handler Origin => origin;
        public Handler Raw    => raw;
    }
}