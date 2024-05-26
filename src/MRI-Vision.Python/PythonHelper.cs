using Python.Runtime;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MRI_Vision.Python;

/// <summary>
/// Helper for calling Python code in one thread
/// </summary>
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

    /// <summary>
    /// Add action to the queue
    /// </summary>
    public static MoveToAwaitable MoveTo()
    {
        return new MoveToAwaitable();
    }

    /// <summary>
    /// Awaitable returned from <see cref="PythonHelper.MoveTo"/> used to schedule an operation on a Python thread
    /// </summary>
    public struct MoveToAwaitable
    {
        /// <summary>
        /// Get <see cref="MoveToAwaiter"/> 
        /// </summary>
        public MoveToAwaiter GetAwaiter() => new();
    }

    /// <summary>
    /// <inheritdoc cref="INotifyCompletion"/>
    /// Awaiter used to support async/await with <see cref="PythonHelper.MoveTo"/>
    /// </summary>
    public class MoveToAwaiter : INotifyCompletion
    {
        /// <summary>
        /// Is <see cref="Action"/> completed
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Get <see cref="Action"/> result
        /// </summary>
        public void GetResult() { }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            _actions.Add(() =>
            {
                continuation.Invoke();
                IsCompleted = true;
            });
        }
    }
}
