using System.Linq;
using LibGit2Sharp;

namespace GitSubdirTools.Libs.Back
{
    class BackCopyCommitCallback : ICopyCommitCallback
    {
        private readonly BackCopyOptions _copyOptions;

        public BackCopyCommitCallback(BackCopyOptions copyOptions)
        {
            _copyOptions = copyOptions;
        }

        public CommitCopyKind GetCommitCopyKind(Commit commit)
        {
            return CommitCopyKind.CopyCommit;
        }

        public Tree CreateCommitTree(Commit commit, Commit[] parentsInTargetRepo, Repository targetRepo)
        {
            var modify = new TreeModify();
            modify.AddOrSet(_copyOptions.DirInSrc, commit.Tree);
            return TreeUtil.CopyTreeToAnotherRepositoryWithModifying(parentsInTargetRepo[0].Tree, targetRepo, modify);
        }

        public string GenerateCommitMessage(Commit commit)
        {
            return commit.Message + "\n" + CommitMessageUtil.SubdirPrefix + _copyOptions.DirInSrc
                   + ": " + _copyOptions.SubdirDesc + "@" + commit.Sha + "\n";
        }

        public bool TryGetCopiedCommit(Commit copiedFrom, Repository targetRepo, out Commit? commit)
        {
            var hash = CommitMessageUtil.ReadBasedirCommitNameFromMessage(copiedFrom.Message);
            if (hash != null)
            {
                // TODO: make option: remote name
                commit = targetRepo.Lookup<Commit>(hash);
                if (commit != null) return true;
                CommandGit.ShallowFetch(targetRepo, "origin", hash.Sha);
                commit = targetRepo.Lookup<Commit>(hash);
                if (commit != null) return true;
                return false;
            }
            else
            {
                commit = RepositoryUtil.GetAllCommits(targetRepo)
                    .FirstOrDefault(copied =>
                        CommitMessageUtil.ReadSubdirCommitNameFromMessage(copied.Message, _copyOptions.DirInSrc) ==
                        copiedFrom.Id);
                if (commit != null) return true;

                return false;
            }
        }
    }
}
