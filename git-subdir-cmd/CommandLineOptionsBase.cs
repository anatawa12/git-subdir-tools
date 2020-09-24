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

        [Option("subdir-repo-desc",
            HelpText = "url or identifier of subdir repository.",
            MetaValue = "SUBDIR_REPO_DESC")]
        public string? SubdirRepoDesc { get; set; } = null;

        [Option("rootdir-repo-desc",
            HelpText = "url or identifier of rootdir repository.",
            MetaValue = "ROOTDIR_REPO_DESC")]
        public string? RootdirDesc { get; set; } = null;

        [Option('d', "dir-in-src", HelpText = "the directory which should be copied. " +
                                              "you can choose two or more directories " +
                                              "but only first directory which is found will only be used",
            MetaValue = "DIR_IN_SRC", Required = true)]
        public IList<string> DirInSrcs { get; set; } = null!;

#if SUPPORT_REMOTE_REPOSITORY
        [Option("push", HelpText = "pushes repository to origin")]
        public bool PushAfterCommit { get; set; } = false;
#else
        public bool PushAfterCommit => false;
#endif
    }
}
