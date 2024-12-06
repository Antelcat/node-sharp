using System.Diagnostics;

namespace Antelcat.NodeSharp.Tests;

public class DelegateTests
{
    public event Action<object[]>    AsEvent;
    public readonly  IList<Delegate> AsEnumerable = [];
    public           Delegate        AsDelegate;
    private readonly Stopwatch       stopwatch = new();
    
    [SetUp]
    public void Setup()
    {
        for (var i = 0; i < 3; i++)
        {
            var action = New();
            AsEvent += args => action.DynamicInvoke(args);
            AsEnumerable.Add(action);
            AsDelegate = Delegate.Combine(action, AsDelegate);
        }
    }

    [Test]
    public void TestWatch()
    {
        stopwatch.Start();
        TestCombine();
        Console.WriteLine(stopwatch.ElapsedTicks);
    }
    
    private void TestAdd()
    {
        AsEvent.Invoke([1, 2]);
    }

    private void TestCombine()
    {
        var res = AsDelegate.DynamicInvoke(1, 2);
        if (res is Task task)
        {
            
        } 
    }

    private void TestList()
    {
        foreach (var @delegate in AsEnumerable)
        {
            @delegate.DynamicInvoke(1, 2);
        }
    }

    [Test]
    public void TestRemove()
    {
        var fin = Delegate.Combine(AsDelegate, Action);
        var del = Delegate.Remove(fin, Action);
    }

    [Test]
    public void TestException()
    {
        Action e = () => throw new Exception("1");
        e += () => throw new Exception("2");
        try
        {
            e();
        }
        catch (Exception ex)
        {
            Debugger.Break();
        }
    }
    
    private int count;
    public Delegate New()
    {
        var c = count++;
        return async (int a, int b) =>
        {
            Console.WriteLine($"this is delegate {c}");
        };
    }

    private async Task Action(int a, int b)
    {
        
    }
    
    [Test]
    public async Task TestCancel()
    {
        var source = new CancellationTokenSource();
        await source.CancelAsync();
        await Task.FromCanceled(source.Token).ContinueWith(t => { }, TaskContinuationOptions.OnlyOnCanceled);
    } 
}