using System.Collections.Generic;
using LibGit2Sharp;

namespace GitSubdirTools.Libs.Copy
{
    public class CopyOptions
    {
        public CopyOptions(
            string rootdirDesc)
        {
            RootdirDesc = rootdirDesc;
        }

        public string              RootdirDesc       { get; }
        public ObjectIdCache       ObjectMapping     { get; init; } = new ObjectIdCache(null);
        public Logger?             Logger            { get; init; } = null;
        public bool                CommitEvenIfEmpty { get; init; } = false;
        public IEnumerable<string> DirInSrcs         { get; init; } = System.Array.Empty<string>();
        public IList<Branch>       Branches          { get; init; } = System.Array.Empty<Branch>();
        public int                 MaxDepth          { get; init; } = -1;
    }
}
