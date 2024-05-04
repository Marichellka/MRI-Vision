using Python.Runtime;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MRI_Vision.Python;

public static class PythonHelper
{
    private static readonly BlockingCollection<Action> _actions = new();

    static PythonHelper()
    {
        new Thread(() =>
        {
            Runtime.PythonDLL = @"C:\Python311\python311.dll";
            PythonEngine.Initialize();

            while (true)
            {
                var action = _actions.Take();
                action.Invoke();
            }
        })
        { IsBackground = true }.Start();
    }

    public static MoveToAwaitable MoveTo()
    {
        return new MoveToAwaitable();
    }

    public struct MoveToAwaitable
    {
        public MoveToAwaiter GetAwaiter() => new();
    }

    public class MoveToAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; private set; }

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            _actions.Add(() =>
            {
                continuation.Invoke();
                IsCompleted = true;
            });
        }
    }
}
