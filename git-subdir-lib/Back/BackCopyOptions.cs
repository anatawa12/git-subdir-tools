using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitSubdirTools.Libs.Back
{
    public class BackCopyOptions
    {
        public BackCopyOptions(string subdirDesc)
        {
            SubdirDesc = subdirDesc;
        }


        public string        SubdirDesc    { get; }
        public ObjectIdCache ObjectMapping { get; init; } = new ObjectIdCache(null);
        public Logger?       Logger        { get; init; } = null;
        public IList<string> DirInSrcs     { get; init; } = Array.Empty<string>();
        public IList<Branch> Branches      { get; init; } = new Branch[0];
        public int           MaxDepth      { get; init; } = -1;
    }
}
