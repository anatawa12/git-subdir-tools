using System;
using System.Runtime.CompilerServices;

namespace GitSubdirTools.Libs.RecursiveCall
{
    internal struct VoidStruct
    {
    }

    [AsyncMethodBuilder(typeof(RecursiveCallResultBuilder))]
    public class RecursiveCallResult
    {
        internal RecursiveCallResult<VoidStruct> Base;

        internal RecursiveCallResult(RecursiveCallResult<VoidStruct> @base)
        {
            Base = @base;
        }

        public RecursiveCallResultAwaiter GetAwaiter() => new RecursiveCallResultAwaiter(Base.GetAwaiter());
    }

    public readonly struct RecursiveCallResultAwaiter : INotifyCompletion
    {
        private readonly RecursiveCallResultAwaiter<VoidStruct> _awaiter;

        internal RecursiveCallResultAwaiter(RecursiveCallResultAwaiter<VoidStruct> awaiter) => _awaiter = awaiter;

        public bool IsCompleted                      => _awaiter.IsCompleted;
        public void GetResult()                      => _awaiter.GetResult();
        public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);
    }

    // see https://github.com/dotnet/roslyn/blob/d148f06/docs/features/task-types.md
    public struct RecursiveCallResultBuilder
    {
        private RecursiveCallResultBuilder<VoidStruct> _base;

        // ReSharper disable once UnusedParameter.Local
        private RecursiveCallResultBuilder(int _)
        {
            _base = RecursiveCallResultBuilder<VoidStruct>.Create();
            Task = new RecursiveCallResult(_base.Task!);
        }

        public static RecursiveCallResultBuilder Create() => new RecursiveCallResultBuilder(0);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // nop
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => _base.Start(ref stateMachine);

        public void SetException(Exception exception) => _base.SetException(exception);
        public void SetResult()                       => _base.SetResult(default);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _base.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => _base.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public RecursiveCallResult Task { get; }
    }
}
