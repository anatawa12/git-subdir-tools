using System.Collections.Generic;
using LibGit2Sharp;

namespace GitSubdirTools.Libs.Back
{
    public class BackCopyOptions
    {
        public BackCopyOptions(
            string dirInSrc,
            string subdirDesc)
        {
            DirInSrc = dirInSrc;
            SubdirDesc = subdirDesc;
        }


        public string        SubdirDesc    { get; }
        public ObjectIdCache ObjectMapping { get; init; } = new ObjectIdCache(null);
        public Logger?       Logger        { get; init; } = null;
        public string        DirInSrc      { get; }
        public IList<Branch> Branches      { get; init; } = new Branch[0];
        public int           MaxDepth      { get; init; } = -1;
    }
}
