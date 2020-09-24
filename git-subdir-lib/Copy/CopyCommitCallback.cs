using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace GitSubdirTools.Libs.Copy
{
    class CopyCommitCallback : ICopyCommitCallback
    {
        private readonly CopyOptions _options;

        public CopyCommitCallback(CopyOptions options)
        {
            _options = options;
        }

        public CommitCopyKind GetCommitCopyKind(Commit commit)
        {
            var tree = GetSubdirTree(commit.Tree);

            if (tree == null)
            {
                // if source tree not found: this commit will be parent of top commit.
                return CommitCopyKind.NoCommit;
            }
            else if (!_options.CommitEvenIfEmpty && !CommitIsChanged(commit.Parents, tree))
            {
                // if no difference: this commit is same as parent's commit
                return CommitCopyKind.UseParent;
            }
            else
            {
                // otherwise: copies commit
                return CommitCopyKind.CopyCommit;
            }
        }

        private bool CommitIsChanged(IEnumerable<Commit> parents, Tree tree)
        {
            var isEmpty = true;
            foreach (var parent in parents)
            {
                isEmpty = false;
                var subDir = GetSubdirTree(parent.Tree);
                if (subDir?.Id != tree.Id) return true;
            }

            if (isEmpty) return true;
            else return false;
        }

        private Tree? GetSubdirTree(Tree tree)
        {
            var treeEntries = from dirInSrc in _options.DirInSrcs
                select tree[dirInSrc]
                into directoryEntry
                where directoryEntry != null
                where directoryEntry.TargetType == TreeEntryTargetType.Tree
                select directoryEntry;

            return (Tree?) treeEntries.FirstOrDefault()?.Target;
        }

        public Tree CreateCommitTree(Commit commit, Commit[] parentsInTargetRepo, Repository targetRepo)
        {
            return TreeUtil.CopyTreeToAnotherRepository(GetSubdirTree(commit.Tree)!, targetRepo);
        }

        public string GenerateCommitMessage(Commit commit)
        {
            return commit.Message + "\n" + CommitMessageUtil.BasedirPrefix + _options.RootdirDesc
                   + "@" + commit.Sha + "\n";
        }

        public bool TryGetCopiedCommit(Commit copiedFrom, Repository targetRepo, out Commit? commit)
        {
            commit = RepositoryUtil.GetAllCommits(targetRepo)
                .FirstOrDefault(copied =>
                    CommitMessageUtil.ReadBasedirCommitNameFromMessage(copied.Message) == copiedFrom.Id);
            return commit != null;
        }
    }
}
