using System;
using System.Collections.Generic;
using System.Linq;
using GitSubdirTools.Libs.RecursiveCall;
using LibGit2Sharp;

namespace GitSubdirTools.Libs
{
    public interface ICopyCommitProgressHandler : IDisposable
    {
        void OnStartCopy(Commit commit)
        {
        }

        void OnCopiedOneCommit(Commit commit, Commit? newCommit)
        {
        }

        void OnFoundNewCommit(int newCommitCount)
        {
        }

        void IDisposable.Dispose()
        {
        }
    }

    public interface ICopyCommitCallback
    {
        CommitCopyKind GetCommitCopyKind(Commit     commit);
        Tree           CreateCommitTree(Commit      commit, Commit[] parentsInTargetRepo, Repository targetRepo);
        string         GenerateCommitMessage(Commit commit);
        bool           TryGetCopiedCommit(Commit    copiedFrom, Repository targetRepo, out Commit? commit);
    }

    public enum CommitCopyKind
    {
        /// <summary>
        /// source tree not found: this commit will be parent of top commit.
        /// </summary>
        NoCommit,

        /// <summary>
        /// no difference: this commit is same as parent's commit
        /// </summary>
        UseParent,

        /// <summary>
        /// otherwise: copies commit
        /// </summary>
        CopyCommit,
    }

    public class CopyCommitContext
    {
        private readonly ICopyCommitProgressHandler?                         _progressHandler;
        private readonly ObjectIdCache                                       _objectMapping;
        private readonly ICopyCommitCallback                                 _callback;
        private readonly IDictionary<ObjectId, RecursiveCallResult<Commit?>> _referenceOrCopyCommitPromises;

        public CopyCommitContext(ICopyCommitProgressHandler? progressHandler, ObjectIdCache objectMapping,
            ICopyCommitCallback                              callback)
        {
            _progressHandler = progressHandler;
            _objectMapping = objectMapping;
            _callback = callback;
            _referenceOrCopyCommitPromises = new Dictionary<ObjectId, RecursiveCallResult<Commit?>>();
        }

        public Commit? CopyCommits(Repository sourceRepo, Commit root, Repository targetRepo, int maxDepth)
        {
            using var ctx = RecursiveCaller.CreateContext();
            return ctx.Call(ReferenceOrCopyCommit(sourceRepo, root, targetRepo, maxDepth));
        }

        public RecursiveCallResult<Commit?> ReferenceOrCopyCommit(
            Repository sourceRepo,
            Commit     commit,
            Repository targetRepo,
            int        maxDepth)
        {
            _progressHandler?.OnStartCopy(commit);
            if (!_referenceOrCopyCommitPromises.TryGetValue(commit.Id, out var promise))
            {
                promise = ReferenceOrCopyCommitImpl(sourceRepo, commit, targetRepo, maxDepth);
                _referenceOrCopyCommitPromises[commit.Id] = promise;
            }

            var awaiter = promise.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                _progressHandler?.OnCopiedOneCommit(commit, awaiter.GetResult());
            }
            else
            {
                var progressHandler = _progressHandler;
                awaiter.OnCompleted(() => { progressHandler?.OnCopiedOneCommit(commit, awaiter.GetResult()); });
            }

            return promise;
        }

        private async RecursiveCallResult<Commit?> ReferenceOrCopyCommitImpl(
            Repository sourceRepo,
            Commit     commit,
            Repository targetRepo,
            int        maxDepth)
        {
            if (maxDepth == 0) return null;
            // fastest way: use copied commit
            if (TryGetCopiedCommit(commit, targetRepo, out var newCommit))
            {
                return newCommit;
            }

            switch (_callback.GetCommitCopyKind(commit))
            {
                case CommitCopyKind.NoCommit:
                {
                    // if source tree not found: this commit will be parent of top commit.
                    newCommit = null;
                }
                    break;
                case CommitCopyKind.UseParent:
                {
                    // if maxDepth == 1 means deepest commit: parent never exists so force copy commit
                    if (maxDepth == 1) goto case CommitCopyKind.CopyCommit;

                    // if no difference: this commit is same as parent's commit
                    _progressHandler?.OnFoundNewCommit(1);
                    newCommit = await ReferenceOrCopyCommit(sourceRepo, commit.Parents.First(), targetRepo,
                        NextDepth(maxDepth));
                }
                    break;
                case CommitCopyKind.CopyCommit:
                {
                    // otherwise: copies commit

                    // Count() will be fast because Parents implements Collection
                    var parentCount = commit.Parents.Count();
                    _progressHandler?.OnFoundNewCommit(parentCount);

                    Commit[] parentArray  = new Commit[parentCount];
                    var      parentsIndex = 0;

                    RecursiveCallResult<Commit?>[] promises      = new RecursiveCallResult<Commit?>[parentCount];
                    var                            promisesIndex = 0;

                    foreach (var commitParent in commit.Parents)
                    {
                        promises[promisesIndex++] =
                            ReferenceOrCopyCommit(sourceRepo, commitParent, targetRepo, NextDepth(maxDepth));
                    }

                    foreach (var promise in promises)
                    {
                        var newParent = await promise;
                        if (newParent != null)
                            parentArray[parentsIndex++] = newParent;
                    }

                    Commit[] parents = parentArray.AsSpan().Slice(0, parentsIndex).ToArray();

                    targetRepo.Index.Clear();
                    var targetRepoIndexTree = _callback.CreateCommitTree(commit, parents, targetRepo);

                    var message   = _callback.GenerateCommitMessage(commit);
                    var author    = commit.Author;
                    var committer = commit.Committer;

                    newCommit = targetRepo.ObjectDatabase.CreateCommit(author, committer, message,
                        targetRepoIndexTree, parents, false);


                    _objectMapping[newCommit.Id] = commit.Id;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("", "result of GetCommitCopyKind");
            }

            _objectMapping[commit.Id] = newCommit?.Id ?? PreDefinedHash.EmptyTreeId;

            return newCommit;
        }

        private int NextDepth(int maxDepth)
        {
            if (maxDepth < 0) return -1;
            return maxDepth - 1;
        }

        private bool TryGetCopiedCommit(Commit copiedFrom, Repository targetRepo, out Commit? commit)
        {
            if (_objectMapping.TryGet(copiedFrom.Id, out var newId))
            {
                // PreDefinedHash.EmptyTreeId is used as null commit.
                if (newId == PreDefinedHash.EmptyTreeId)
                {
                    commit = null;
                    return true;
                }
                else
                {
                    commit = targetRepo.Lookup<Commit>(newId);
                    if (commit != null) return true;
                }
            }

            return _callback.TryGetCopiedCommit(copiedFrom, targetRepo, out commit);
        }
    }
}
