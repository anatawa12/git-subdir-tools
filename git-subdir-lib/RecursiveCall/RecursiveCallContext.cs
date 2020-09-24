using System;
using System.Collections.Concurrent;

namespace GitSubdirTools.Libs.RecursiveCall
{
    internal class RecursiveCallContext : IDisposable
    {
        [ThreadStatic] internal static RecursiveCallContext? Context;

        internal readonly BlockingCollection<Action> WillDoAction = new BlockingCollection<Action>();

        public void AddAction(Action moveNext)
        {
            WillDoAction.Add(moveNext);
        }

        public void OnEndOwnerOfContext()
        {
            WillDoAction.CompleteAdding();
        }

        public void Dispose()
        {
            WillDoAction.Dispose();
        }
    }
}
