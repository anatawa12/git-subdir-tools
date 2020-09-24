using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace GitSubdirTools.Libs
{
    public static class RepositoryUtil
    {
        public static BottomUpWalker<Commit> GetAllCommits(Repository repository)
        {
            IEnumerable<Commit> GetParentsOf(Commit commit)
                => commit.Parents.Where(commit1 => repository.Lookup(commit1.Id) != null);

            var allRefTargetsCommits = new HashSet<Commit>();
            foreach (var repositoryRef in repository.Refs)
            {
                var sha = repositoryRef.TargetIdentifier;
                if (ObjectId.TryParse(sha, out var objectId))
                {
                    var commit = repository.Lookup<Commit>(objectId);
                    if (commit == null) continue;
                    allRefTargetsCommits.Add(commit);
                }
            }

            return new BottomUpWalker<Commit>(allRefTargetsCommits, GetParentsOf, parent => parent.Id);
        }

        public static IEnumerable<Commit> GetAllCommits1(Repository repository)
        {
            using var enumerator = repository.Commits.GetEnumerator();
            while (true)
            {
                try
                {
                    var moveNext = enumerator.MoveNext();
                    if (!moveNext) break;
                }
                catch (LibGit2SharpException e)
                {
                    // means NotFound
                    if (e.Data["libgit2.code"] is int code && code == -3)
                        continue;
                    throw;
                }

                yield return enumerator.Current!;
            }
        }
    }
}
