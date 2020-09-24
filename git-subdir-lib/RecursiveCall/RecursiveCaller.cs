using System;

namespace GitSubdirTools.Libs.RecursiveCall
{
    public static class RecursiveCaller
    {
        public static TResult Call<TResult>(Func<RecursiveCallResult<TResult>> block)
        {
            using var ctx = CreateContext();
            return ctx.Call(block());
        }

        public static void Call(Func<RecursiveCallResult> block)
        {
            Call(() => block().Base);
        }

        public static RecursiveCallerContext CreateContext()
        {
            var oldContext = RecursiveCallContext.Context;
            var ctx        = RecursiveCallContext.Context = new RecursiveCallContext();
            return new RecursiveCallerContext(ctx, oldContext);
        }
    }

    public struct RecursiveCallerContext : IDisposable
    {
        private readonly RecursiveCallContext  _context;
        private readonly RecursiveCallContext? _oldContext;

        internal RecursiveCallerContext(RecursiveCallContext context, RecursiveCallContext? oldContext)
        {
            _context = context;
            _oldContext = oldContext;
        }

        public TResult Call<TResult>(RecursiveCallResult<TResult> result)
        {
            if (_context != RecursiveCallContext.Context) throw new InvalidOperationException("context mismatch");
            result.IsOwnerOfCtx = true;
            if (result.IsCompleted) return result.GetAwaiter().GetResult();
            foreach (var action in _context.WillDoAction.GetConsumingEnumerable()) action();
            return result.GetAwaiter().GetResult();
        }

        public void Call(RecursiveCallResult result)
        {
            Call(result.Base);
        }

        public void Dispose()
        {
            _context.Dispose();
            RecursiveCallContext.Context = _oldContext;
        }
    }
}
