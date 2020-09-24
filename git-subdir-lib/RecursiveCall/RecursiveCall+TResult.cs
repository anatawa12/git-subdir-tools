using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GitSubdirTools.Libs.RecursiveCall
{
    [AsyncMethodBuilder(typeof(RecursiveCallResultBuilder<>))]
    public class RecursiveCallResult<TResult>
    {
        internal RecursiveCallContext Context;
        internal bool                 IsCompleted;
        internal Exception?           Exception;
        internal TResult              Result = default!;
        internal Action?              OnCompleted;

        internal RecursiveCallResult()
        {
            Context = RecursiveCallContext.Context ??
                      throw new InvalidOperationException(
                          "RecursiveCall cannot use out of RecursiveCallCaller.Call method.");
        }

        internal bool IsOwnerOfCtx { get; set; }

        public RecursiveCallResultAwaiter<TResult> GetAwaiter() => new RecursiveCallResultAwaiter<TResult>(this);
    }

    public readonly struct RecursiveCallResultAwaiter<TResult> : INotifyCompletion
    {
        private readonly RecursiveCallResult<TResult> _result;

        internal RecursiveCallResultAwaiter(RecursiveCallResult<TResult> result)
        {
            _result = result;
        }

        public bool IsCompleted => _result.IsCompleted;

        public TResult GetResult()
        {
            if (_result.Exception != null) Task.FromException(_result.Exception).GetAwaiter().GetResult();
            return _result.Result;
        }

        public void OnCompleted(Action continuation)
        {
            if (_result.OnCompleted == null)
                _result.OnCompleted = continuation;
            else
                _result.OnCompleted += continuation;
        }
    }

    // see https://github.com/dotnet/roslyn/blob/d148f06/docs/features/task-types.md
    public readonly struct RecursiveCallResultBuilder<TResult>
    {
        // ReSharper disable once UnusedParameter.Local
        private RecursiveCallResultBuilder(int _)
        {
            Task = new RecursiveCallResult<TResult>();
        }

        public static RecursiveCallResultBuilder<TResult> Create() => new RecursiveCallResultBuilder<TResult>(0);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // nop
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            Task.Context.AddAction(stateMachine.MoveNext);
        }

        public void SetException(Exception exception)
        {
            Task.Exception = exception;
            Task.IsCompleted = true;
            Task.OnCompleted?.Invoke();
            if (Task.IsOwnerOfCtx) Task.Context.OnEndOwnerOfContext();
        }

        public void SetResult(TResult result)
        {
            Task.Result = result;
            Task.IsCompleted = true;
            Task.OnCompleted?.Invoke();
            if (Task.IsOwnerOfCtx) Task.Context.OnEndOwnerOfContext();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var stateMachineCached = stateMachine;
            var task               = Task;
            awaiter.OnCompleted(() => task.Context.AddAction(stateMachineCached.MoveNext));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => AwaitOnCompleted(ref awaiter, ref stateMachine);

        public RecursiveCallResult<TResult> Task { get; }
    }
}
