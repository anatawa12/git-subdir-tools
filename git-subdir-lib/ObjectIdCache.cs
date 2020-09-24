using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace GitSubdirTools.Libs
{
    public class ObjectIdCache : IAsyncDisposable
    {
        private readonly Dictionary<ObjectId, ObjectId> _dictionary = new ();
        private readonly string?                        _file;

        public ObjectIdCache(string? file)
        {
            this._file = file;
            if (file != null && File.Exists(file))
            {
                foreach (var line in File.ReadLines(file))
                {
                    var values = line.Split(':');
                    if (values.Length != 2) continue;
                    if (!values[0].IsValidObjectId()) continue;
                    if (!values[1].IsValidObjectId()) continue;
                    _dictionary[new ObjectId(values[0])] = new ObjectId(values[1]);
                }
            }
        }

        public ObjectId? Get(ObjectId key) => _dictionary.TryGetValue(key, out var value) ? value : null;

        public bool TryGet(ObjectId key, [MaybeNullWhen(false)] out ObjectId value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public async ValueTask Write()
        {
            if (_file != null)
            {
                await using var streamWriter = File.CreateText(_file);
                foreach (var (k, v) in _dictionary)
                {
                    await streamWriter.WriteLineAsync($"{k.Sha}:{v.Sha}");
                }
            }
        }

        public ObjectId this[ObjectId commitId]
        {
            get => Get(commitId) ?? throw new KeyNotFoundException($"{commitId}");
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _dictionary[commitId] = value;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Write();
        }
    }
}
