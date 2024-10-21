using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Antelcat.NodeSharp.Events;

public class EventEmitter
{
    public static int DefaultMaxListeners { get; set; } = 10;

    private readonly ReaderWriterLockSlim      locker   = new();
    private readonly Dictionary<string, Event> handlers = [];


    public void AddListener(string eventName, Action<object?[]> listener) => On(eventName, listener);

    public void Emit(string eventName, params object?[] args)
    {
        locker.EnterReadLock();
        Get(eventName).Emit(args);
        locker.ExitReadLock();
    }

    public IReadOnlyCollection<string> EventNames => handlers.Keys;

    public int MaxListeners { get; set; } = DefaultMaxListeners;

    public int ListenerCount(string eventName, Action<object?[]>? listener = null) =>
        !handlers.TryGetValue(eventName, out var @event) ? 0 : @event.Count(listener);

    public IEnumerable<Action<object?[]>> Listeners(string eventName) =>
        !handlers.TryGetValue(eventName, out var @event) ? [] : @event.Listeners;

    public void Off(string eventName, Action<object?[]> listener)
    {
        locker.EnterWriteLock();
        if (handlers.TryGetValue(eventName, out var @event))
        {
            @event.Off(listener);
            if (!@event.Listeners.Any()) handlers.Remove(eventName);
        }

        locker.ExitWriteLock();
    }

    public void On(string eventName, Action<object?[]> listener)
    {
        locker.EnterWriteLock();
        CheckEvent(Get(eventName))?.On(listener);
        locker.ExitWriteLock();
    }

    public void Once(string eventName, Action<object?[]> listener)
    {
        locker.EnterWriteLock();
        CheckEvent(Get(eventName))?.Once(listener);
        locker.ExitWriteLock();
    }

    public void PrependListener(string eventName, Action<object?[]> listener)
    {
        locker.EnterWriteLock();
        CheckEvent(Get(eventName))?.Prepend(listener);
        locker.ExitWriteLock();
    }

    public void PrependOnceListener(string eventName, Action<object?[]> listener)
    {
        locker.EnterWriteLock();
        CheckEvent(Get(eventName))?.PrependOnce(listener);
        locker.ExitWriteLock();
    }

    public void RemoveAllListeners(string eventName)
    {
        locker.EnterWriteLock();
        handlers.Remove(eventName);
        locker.ExitWriteLock();
    }

    public void RemoveListener(string eventName, Action<object?[]> listener) => Off(eventName, listener);

    public IEnumerable<Action<object?[]>> RawListeners(string eventName) =>
        !handlers.TryGetValue(eventName, out var @event) ? [] : @event.RawListeners;

    private Event? CheckEvent(Event @event) => @event.Count() < MaxListeners ? @event : null;

    private Event Get(string eventName)
    {
        if (handlers.TryGetValue(eventName, out var @event)) return @event;
        @event              = new Event();
        handlers[eventName] = @event;
        return @event;
    }

    private class Event
    {
        private readonly object          locker    = new();
        private readonly List<Wrapper>   listeners = [];
        private event Action<object?[]>? Events;

        public void Prepend(Action<object?[]> listener) => Prepend(listener, listener);
        public void On(Action<object?[]> listener)      => On(listener, listener);

        public void PrependOnce(Action<object?[]> listener)
        {
            Action<object?[]> call = null!;
            call = args =>
            {
                if (!Remove(listener)) return;
                Events -= call;
                listener(args);
            };
            Prepend(listener, call);
        }

        public void Once(Action<object?[]> listener)
        {
            Action<object?[]> call = null!;
            call = args =>
            {
                if (!Remove(listener)) return;
                Events -= call;
                listener(args);
            };
            On(listener, call);
        }

        public void Emit(params object?[] args)
        {
            lock (locker) Events?.Invoke(args);
        }

        public void Off(Action<object?[]> listener) => Remove(listener);

        public int Count(Action<object?[]>? listener = null) =>
            listener is null ? listeners.Count : listeners.Count(x => x.Origin == listener);

        public IEnumerable<Action<object?[]>> Listeners => listeners.Select(static x => x.Origin);

        public IEnumerable<Action<object?[]>> RawListeners => listeners.Select(static x => x.Raw);

        private void On(Action<object?[]> source, Action<object?[]> call)
        {
            var handler = new Wrapper(source, call);
            lock (locker)
            {
                listeners.Add(handler);
                Events += handler.Raw;
            }
        }

        private void Prepend(Action<object?[]> source, Action<object?[]> call)
        {
            var handler = new Wrapper(source, call);
            lock (locker)
            {
                listeners.Add(handler);
                Events = handler.Raw + Events;
            }
        }

        private bool Remove(Action<object?[]> listener)
        {
            var remove = listeners.FirstOrDefault(x => x.Origin == listener);
            if (remove is null) return false;
            lock (locker)
            {
                return listeners.Remove(remove);
            }
        }
    }

    private class Wrapper(Action<object?[]> origin, Action<object?[]> raw)
    {
        public Action<object?[]> Origin => origin;
        public Action<object?[]> Raw    => raw;
    }
}