using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Antelcat.NodeSharp.Events;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Antelcat.NodeSharp.Tests;

public class Tests
{
    private readonly EventEmitter emitter = new(new EventEmitterOptions()
    {
        CaptureFailed = true
    });
    [SetUp]
    public void Setup()
    {
        emitter.Error += Failed;
    }

    private void Failed(string name, Exception ex) => Assert.Fail(name, ex);

    [Test]
    public async Task TestEmitOnce()
    {
        const string name  = "event";
        var          times = 0;
        emitter.On(name, () => "not important");
        emitter.Once(name, (int _) =>
        {
            Console.WriteLine($"Call {++times} times");
            if (times > 1) throw new Exception("this should be call once");
        });
        emitter.RemovedListener += (eventName, listener) =>
        {
            Console.WriteLine($"event {eventName} removed listener as expected");
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
        var times   = 0;
        emitter.On("event",  (int arg) =>
        {
            var time = times++;
            Console.WriteLine($"Times:{time} Args:{string.Join(",", arg)}");
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
        emitter.On(name, () => { });
        emitter.RemovedListener += (eventName, listener) =>
        {
            Console.WriteLine($"event {eventName} removed listener");
        };
        var handler = (object value) =>
        {
            throw new Exception("this should not be call");
        };
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
        var caught = false;
        emitter.On(name, () =>
        {
            if (caught == false) throw new Exception("caught should be true");
            Console.WriteLine("caught is true");
        });
        emitter.PrependListener(name, () =>
        {
            caught = true;
            Console.WriteLine("set caught true");
        });
        emitter.Emit(name, 1);
    }

    [Test]
    public void TestVariantListen()
    {
        const string name = "variant";
        emitter.On(name, () => { Console.WriteLine("I dont need arg"); });
        emitter.On(name, async () =>
        {
            await Task.CompletedTask;
            Console.WriteLine("I dont need arg and I am async"); 
        });
        emitter.On(name, (params object[] args) =>
        {
            Console.WriteLine($"I need everything, they are {string.Join(", ", args)}");
        });
        emitter.On(name, (int first) =>
        {
            Console.WriteLine($"I need strong type first arg as int: {first}");
        });
        emitter.On(name, (int first, int second, object third) =>
        {
            Console.WriteLine($"I need first: {first}, second: {second} , third: {third}");
        });

        emitter.Emit(name, 1);
        Console.WriteLine("--- next round ---");
        emitter.Emit(name, 2, 3);
    }

    [Test]
    public async Task TestException()
    {
        emitter.On("Error", (Action)(() => throw new Exception("Sync Error")));
        try
        {
            emitter.Emit("Error");
            Assert.Fail();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.InnerException?.Message} threw");
        }

        var source = Wait<Exception>();
        emitter.Error -= Failed;
        emitter.Error += (_, ex) =>
        {
            source.SetResult(ex);
        };
        emitter.On("AsyncError", (Func<Task>)(async () => throw new Exception("Async Error")));
        emitter.Emit("AsyncError");
        var ae = await source.Task;
        Console.WriteLine($"{ae.InnerException?.Message} threw");
        Assert.Pass();
    }

    private TaskCompletionSource<T> Wait<T>() => new();
    private TaskCompletionSource    Wait()    => new();
}