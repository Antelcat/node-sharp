using System.Diagnostics;
using System.Runtime.InteropServices;
using Antelcat.NodeSharp.Events;

namespace Antelcat.NodeSharp.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestMethodHash()
    {
        var temp  = Handler;
        var hash1 = temp.GetHashCode();
        var temp2 = Handler;
        var hash2 = temp2.GetHashCode();
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void TestRemoveByHash()
    {
        Action? a = null;
        int     hash = 0;
        var     handler = () =>
        {
            a -= (Action)a.GetInvocationList().FirstOrDefault(x => x.GetHashCode() == hash);
            Debugger.Break();
        };
        hash    = handler.GetHashCode();
        a += handler;
        var found = a.GetInvocationList().FirstOrDefault(x => x.GetHashCode() == hash);
        Assert.That(found, Is.Not.Null);
        ((Action)found)();
    }

    [Test]
    public void TestEventCall()
    {
        Action? action = null;
        for (int i = 0; i < 1000_000; i++)
        {
            action += new Action(() =>
            {
                var a = i + 1;
            });
        }

        var watch = new Stopwatch();
        watch.Start();
        action?.Invoke();
        Console.WriteLine(watch.ElapsedTicks);
    }

    [Test]
    public void TestEnumerateCall()
    {
        var dic = new Dictionary<int, Action>();

        for (var i = 0; i < 1000_000; i++)
        {
            var cur = i;
            var handler = () =>
            {
                Trace.WriteLine(cur);
            };
            var code = handler.Method.GetHashCode();
            try
            {
                dic.Add(code, handler);
            }
            catch
            {
                //
            }
        }

        var watch = new Stopwatch();
        watch.Start();
        foreach (var (key, action) in dic)
        {
            action();
        }

        Console.WriteLine(watch.ElapsedTicks);
    }

    [Test]
    public void TestHandler()
    {
        var a1 = () => { };
        var a2 = () => { };
        var h1 = a1.GetHashCode();
        var h2 = a2.GetHashCode();
        var m1 = a1.Method.GetHashCode();
        var m2 = a2.Method.GetHashCode();
        var t1 = a1.Method.MetadataToken;
        var t2 = a2.Method.MetadataToken;
        var eq = a1 == a2;
        Debugger.Break();
    }

    [Test]
    public async Task TestEmitOnce()
    {
        var name    = "event";
        var emitter = new EventEmitter();
        var times   = 0;
        emitter.Once(name, async _ =>
        {
            times++;
            Assert.That(times, Is.LessThan(2));
        });
        emitter.RemovedListener += (eventName, listener) =>
        {
            Console.WriteLine($"event {eventName} removed {listener}");
        };
        var targetTime  = DateTime.Now + TimeSpan.FromSeconds(1);
        var tasks = new List<Task>();
        for (var i = 0; i < 4; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (DateTime.Now < targetTime)
                {
                }

                emitter.Emit(name, 1);
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Test]
    public async Task TestEmit()
    {
        var emitter = new EventEmitter();
        var times   = 0;
        emitter.On("event", async args =>
        {
            var time = times++;
            Console.WriteLine($"Times:{time} Args:{string.Join(",", args)}");
        });
        var targetTime = DateTime.Now + TimeSpan.FromSeconds(1);
        var tasks      = new List<Task>();
        for (var i = 0; i < 4; i++)
        {
            var arg = i;
            tasks.Add(Task.Run(() =>
            {
                while (DateTime.Now < targetTime)
                {
                }

                emitter.Emit("event", arg);
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Test]
    public void TestOff()
    {
        var name    = "event";
        var emitter = new EventEmitter();
        emitter.RemovedListener += (eventName, listener) =>
        {
            Console.WriteLine($"event {eventName} removed {listener}");
        };
        var handler = async (object?[] _) => { Assert.Fail(); };
        emitter.On(name, handler);
        emitter.Off(name, handler);
        emitter.Emit(name, 1);
        
        emitter.Once(name, handler);
        emitter.Off(name, handler);
        emitter.Emit(name, 1);
    }

    [Test]
    public void TestPrepend()
    {
        var name    = "prepend";
        var emitter = new EventEmitter();
        emitter.On(name,  async _ => { Console.WriteLine(2); });
        emitter.PrependListener(name, async _ => { Console.WriteLine(1); });
        emitter.Emit(name, 1);
    }

    [Test]
    public void TestSeq()
    {
        Action action = null!;
        action += () => { Console.WriteLine(1); };
        action =  (() => { Console.WriteLine(2); }) + action;
        action();
    }

    [Test]
    public async Task TestAsyncTasks()
    {
        Func<Task> func = null!;
        func += async () =>
        {
            Console.WriteLine("1 start");
            await Task.Delay(1000);
            Console.WriteLine("1 end");
        };
        func += async () =>
        {
            Console.WriteLine("2 start");
            await Task.Delay(1000);
            Console.WriteLine("2 end");
        };
        var watch = new Stopwatch();
        watch.Start();
        await func();
        Console.WriteLine(watch.ElapsedMilliseconds);
    }

    [Test]
    public async Task TestError()
    {
        var emitter = new EventEmitter();
        emitter.EmitError += (eventName, ex) =>
        {
            Console.WriteLine($"{eventName} threw error {ex.GetType()}");
        };
        emitter.On("error", async _ => throw new ArgumentNullException());
        emitter.Emit("error", 1);
        await Task.Delay(500);
    }
    
    private void Handler(string arg0, string arg1)
    {
        
    }
}