using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MRI_Vision.UI.Utils;

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
        { IsBackground = true}.Start();
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

        public void GetResult() {  }

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
