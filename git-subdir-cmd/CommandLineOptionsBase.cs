using System;
using System.Collections.Generic;
using CommandLine;

namespace GitSubdirTools.Cmd
{
    public abstract class CommandLineOptionsBase
    {
        [Option('s', "subdir-repo",
            HelpText = "subdir repository path or url.",
            MetaValue = "SUBDIR_REPO", Required = true)]
        public string SubdirRepoPath { get; set; } = null!;

        [Option('r', "rootdir-repo",
            HelpText = "rootdir repository path.",
            MetaValue = "ROOTDIR_REPO", Required = true)]
        public string RootdirPath { get; set; } = null!;

        [Option('c', "cache-file", HelpText = "path to cache file", MetaValue = "CACHE_FILE_PATH")]
        public string? CacheFilePath { get; set; } = null;

        [Option("max-depth", HelpText = "max depth of commits")]
        public int MaxDepth { get; set; } = -1;

        [Option("trace", HelpText = "enable trace log")]
        public bool TraceLog { get; set; } = false;

        [Option('b', "branch", HelpText = "branch name. if not specified, all branches will be copied",
            MetaValue = "BRANCH")]
        public IList<string> BranchNames { get; set; } = Array.Empty<string>();

#if SUPPORT_REMOTE_REPOSITORY
        [Option("push", HelpText = "pushes repository to origin")]
        public bool PushAfterCommit { get; set; } = false;
#else
        public bool PushAfterCommit => false;
#endif
    }
}
