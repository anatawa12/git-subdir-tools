using GitSubdirTools.Libs.RecursiveCall;
using LibGit2Sharp;

namespace GitSubdirTools.Libs.Copy
{
    public class CopyCommitMain
    {
        private readonly CopyOptions   _options;
        private readonly ObjectIdCache _objectMapping;
        private readonly Logger?       _logger;

        public CopyCommitMain(CopyOptions options)
        {
            _options = options;
            _objectMapping = options.ObjectMapping;
            _logger = options.Logger;
        }

        public void DoMain(Repository  rootdirRepo, Repository subdirRepo,
            ICopyCommitProgressHandler progressHandler)
        {
            var copyContext = new CopyCommitContext(progressHandler, _objectMapping,
                new CopyCommitCallback(_options));

            // ReSharper disable AccessToDisposedClosure
            RecursiveCaller.Call(async () =>
            {
                progressHandler.OnFoundNewCommit(_options.Branches.Count);
                foreach (var convertBranch in _options.Branches)
                {
                    await CopyBranch(rootdirRepo, subdirRepo, convertBranch, copyContext);
                }
            });
            // ReSharper restore AccessToDisposedClosure
        }

        private async RecursiveCallResult CopyBranch(
            Repository        rootdirRepo,
            Repository        subdirRepo,
            Branch            convertBranch,
            CopyCommitContext copyContext)
        {
            var tip       = convertBranch.Tip;
            var newCommit = await copyContext.ReferenceOrCopyCommit(rootdirRepo, tip!, subdirRepo, _options.MaxDepth);
            if (newCommit == null)
            {
                _logger?.Warn($"branch {convertBranch.CanonicalName} cannot convert: no directory found.");
                return;
            }

            subdirRepo.Refs.Add(convertBranch.CanonicalName, newCommit.Id, true);
        }
    }
}
