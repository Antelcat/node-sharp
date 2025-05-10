using System;
using System.Collections.Generic;
using Handler = System.Delegate;

namespace Antelcat.NodeSharp.Events;

public interface IEventEmitter
{
    /// <summary>
    /// The <see cref="EventEmitter"/> instance will emit its own <see cref="EventEmitter.NewListener"/> event before
    /// a listener is added to its internal array of listeners.
    /// Listeners registered for the <see cref="EventEmitter.NewListener"/> event are passed the event name and a reference
    /// to the listener being added.
    /// The fact that the event is triggered before adding the listener has a subtle but important side effect:
    /// any additional listeners registered to the same name within the <see cref="EventEmitter.NewListener"/> callback
    /// are inserted before the listener that is in the process of being added.
    /// </summary>
    event Action<string, Handler>? NewListener;

    /// <summary>
    /// The <see cref="EventEmitter.RemovedListener"/> event is emitted after the listener is removed.
    /// </summary>
    event Action<string, Handler>? RemovedListener;

    /// <summary>
    /// Occurred when listener throws an error
    /// </summary>
    event Action<string, Exception>? Error;

    /// <summary>
    /// Returns an array listing the events for which the emitter has registered listeners. The values in the array are strings.
    /// </summary>
#if NET40 || NETSTANDARD1_0
    IEnumerable<string>
#else
    IReadOnlyCollection<string>
#endif
        EventNames { get; }

    /// <summary>
    /// The current max listener value for the <see cref="EventEmitter"/> which is defaults to <see cref="EventEmitter.DefaultMaxListeners"/>
    /// </summary>
    int MaxListeners { get; set; }

    /// <summary>
    /// <inheritdoc cref="EventEmitter.On"/>
    /// </summary>
    /// <param name="eventName"><inheritdoc cref="EventEmitter.On"/></param>
    /// <param name="listener"><inheritdoc cref="EventEmitter.On"/></param>
    /// <returns></returns>
    IEventEmitter AddListener(string eventName, Handler listener);

    /// <summary>
    /// Synchronously calls each of the listeners registered for the event named eventName, in the order they were registered,
    /// passing the supplied arguments to each.
    /// Returns true if the event had listeners, false otherwise.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="args"></param>
    /// <returns>true if the event had listeners, false otherwise.</returns>
    bool Emit(string eventName, params object?[] args);

    /// <summary>
    /// Returns the number of listeners listening for the event named eventName. If listener is provided,
    /// it will return how many times the listener is found in the list of the listeners of the event.
    /// </summary>
    /// <param name="eventName">name of the event</param>
    /// <param name="listener"></param>
    /// <returns></returns>
    int ListenerCount(string eventName, Handler? listener = null);

    /// <summary>
    /// Returns a copy of the array of listeners for the event named eventName.
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    IEnumerable<Handler> Listeners(string eventName);

    /// <summary>
    /// <see cref="EventEmitter.RemoveListener"/>
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns></returns>
    IEventEmitter Off(string eventName, Handler listener);

    /// <summary>
    /// Adds the listener function to the end of the listeners array for the event named eventName.
    /// No checks are made to see if the listener has already been added.
    /// Multiple calls passing the same combination of eventName and listener will result in the listener being added,
    /// and called, multiple times.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    IEventEmitter On(string eventName, Handler listener);

    /// <summary>
    /// Adds a one-time listener function for the event named eventName. The next time eventName is triggered,
    /// this listener is removed and then invoked.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    IEventEmitter Once(string eventName, Handler listener);

    /// <summary>
    /// Adds the listener function to the beginning of the listeners array for the event named eventName.
    /// No checks are made to see if the listener has already been added.
    /// Multiple calls passing the same combination of eventName and listener will result in the listener being added,
    /// and called, multiple times.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    IEventEmitter PrependListener(string eventName, Handler listener);

    /// <summary>
    /// Adds a one-time listener function for the event named eventName to the beginning of the listeners array.
    /// The next time eventName is triggered, this listener is removed, and then invoked.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    IEventEmitter PrependOnceListener(string eventName, Handler listener);

    /// <summary>
    /// Removes all listeners, or those of the specified eventName.
    /// It is bad practice to remove listeners added elsewhere in the code,
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    IEventEmitter RemoveAllListeners(string eventName);

    /// <summary>
    /// Removes the specified listener from the listener array for the event named eventName.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="listener"></param>
    /// <returns>Returns a reference to the <see cref="EventEmitter"/>, so that calls can be chained.</returns>
    IEventEmitter RemoveListener(string eventName, Handler listener);

    /// <summary>
    /// Returns a copy of the array of listeners for the event named eventName, including any wrappers (such as those created by .once()).
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    IEnumerable<Handler> RawListeners(string eventName);
}