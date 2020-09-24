using System.Collections.Generic;

namespace GitSubdirTools.Libs
{
    public readonly struct BottomUpWalker<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T>     _starts;
        private readonly NextGetter<T>      _nextGetter;
        private readonly UniqueKeyGetter<T> _keyGetter;

        public BottomUpWalker(IEnumerable<T> starts, NextGetter<T> nextGetter, UniqueKeyGetter<T> keyGetter)
        {
            _starts = starts;
            _nextGetter = nextGetter;
            _keyGetter = keyGetter;
        }

        public BottomUpWalkEnumerator<T> GetEnumerator()
        {
            return new BottomUpWalkEnumerator<T>(_starts, _nextGetter, _keyGetter);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public readonly struct BottomUpWalkEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerable<T>           _starts;
        private readonly IList<BottomUpWalkState> _states;
        private readonly ISet<object>             _returnedUniqueKeys;
        private readonly UniqueKeyGetter<T>?      _uniqueKeyGetter;
        private readonly NextGetter<T>            _nextGetter;

        internal BottomUpWalkEnumerator(IEnumerable<T> starts, NextGetter<T> nextGetter,
            UniqueKeyGetter<T>?                        uniqueKeyGetter)
        {
            _starts = starts;
            _nextGetter = nextGetter;
            _uniqueKeyGetter = uniqueKeyGetter;
            _returnedUniqueKeys = new HashSet<object>();
            _states = new List<BottomUpWalkState>();
            Reset();
        }

        public bool MoveNext()
        {
            while (true)
            {
                // Take next file from the top of the stack or return if there's nothing left
                if (_states.Count == 0) return false;

                var topState = _states[^1];
                if (topState.RootVisited)
                {
                    _states.RemoveAt(_states.Count - 1);
                    topState.Dispose();
                    continue;
                }
                else if (!topState.TryStep(out var file))
                {
                    // There is nothing more on the top of the stack, go back
                    topState.RootVisited = true;
                    _states[^1] = topState;
                    var uniqueKey = _uniqueKeyGetter?.Invoke(topState.Root);
                    if (uniqueKey != null) _returnedUniqueKeys.Add(uniqueKey);
                    return true;
                }
                else
                {
                    var uniqueKey = _uniqueKeyGetter?.Invoke(file);
                    if (uniqueKey != null && _returnedUniqueKeys.Contains(uniqueKey)) continue;
                    // Proceed to a sub-directory
                    _states.Add(new BottomUpWalkState(file, _nextGetter));
                    continue;
                }

#pragma warning disable 162
                // ReSharper disable once HeuristicUnreachableCode
                throw new System.Exception("never pass here!");
#pragma warning restore 162
            }
        }

        public void Reset()
        {
            Dispose();
            foreach (var start in _starts)
            {
                var key = _uniqueKeyGetter?.Invoke(start);
                if (key != null)
                    _returnedUniqueKeys.Add(key);
                _states.Add(new BottomUpWalkState(start, _nextGetter));
            }
        }

        public T Current => _states[^1].Root;

#nullable disable // explicitly leaving Current as "oblivious" to avoid spurious warnings in foreach over non-generic enumerables
        object System.Collections.IEnumerator.Current => Current;
#nullable restore

        public void Dispose()
        {
            foreach (var bottomUpWalkState in _states) bottomUpWalkState.Dispose();
            _states.Clear();
        }

        private struct BottomUpWalkState
        {
            public readonly  T              Root;
            public           bool           RootVisited;
            private readonly IEnumerator<T> _children;

            public BottomUpWalkState(T root, NextGetter<T> getter)
            {
                Root = root;
                RootVisited = false;
                _children = getter(root).GetEnumerator();
            }

            public bool TryStep(out T file)
            {
                if (_children.MoveNext())
                {
                    file = _children.Current;
                    return true;
                }
                else
                {
                    file = default!;
                    return false;
                }
            }

            public void Dispose()
            {
                _children.Dispose();
            }
        }
    }

    public delegate IEnumerable<T> NextGetter<T>(T parent);

    /**
     * null cannot be use as key
     */
    public delegate object? UniqueKeyGetter<in T>(T parent);
}
