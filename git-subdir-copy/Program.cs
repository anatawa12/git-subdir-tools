using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using GitSubdirTools.Cmd;
using GitSubdirTools.Libs;
using GitSubdirTools.Libs.Copy;

namespace GitSubdirTools.Init
{
    internal class CommandLineOptions : CommandLineOptionsBase
    {
        [Option("rootdir-repo-desc",
            HelpText = "url or identifier of rootdir repository.",
            MetaValue = "ROOTDIR_REPO_DESC")]
        public string? RootdirDesc { get; set; } = null;

        [Option('d', "dir-in-src", HelpText = "the directory which should be copied. " +
                                              "you can choose two or more directories " +
                                              "but only first directory which is found will only be used",
            MetaValue = "DIR_IN_SRC", Required = true)]
        public IList<string> DirInSrcs { get; set; } = null!;

        [Option("commit-even-if-empty", HelpText = "Creates commit even if empty commit. In default, " +
                                                   "if there is no change, the commit will never be copied.")]
        public bool CommitEvenIfEmpty { get; set; } = false;
    }

    internal class Program
    {
        private Program()
        {
        }

        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(Main, HandleParseError);
        }

        private static ValueTask<int> HandleParseError(IEnumerable<Error> errs)
        {
            var result = -2;
            if (errs.Any(x => x is HelpRequestedError || x is VersionRequestedError))
                result = -1;
            return new ValueTask<int>(result);
        }

        private static async ValueTask<int> Main(CommandLineOptions options)
        {
            try
            {
                await DoMain(options);
                return 0;
            }
            catch (InvalidUsageException e)
            {
                Console.WriteLine();
                await Console.Error.WriteLineAsync(e.Message);
                return 1;
            }
        }

        private static async ValueTask DoMain(CommandLineOptions options)
        {
            var logger = new Logger
            {
                TraceEnabled = options.TraceLog
            };

            CmdUtil.NoCacheWarn(options.CacheFilePath);

            await using var mapping = new ObjectIdCache(options.CacheFilePath);

            using var rootdirRepo = CmdUtil.PrepareRepository(options.RootdirPath, "rootdir repository",
                maxDepth: options.MaxDepth,
                allowEmpty: false, allowRemote: true);
            using var subdirRepo = CmdUtil.PrepareRepository(options.SubdirRepoPath, "subdir repository",
                maxDepth: options.MaxDepth,
                allowEmpty: true, allowRemote: options.PushAfterCommit,
                mustLocalTailingMessage: " if --push is not specified");

            var rootdirDesc = CmdUtil.GetDescription(
                descriptionRepository: rootdirRepo,
                descriptionByOption: options.RootdirDesc,
                otherRepository: subdirRepo,
                repositoryType: "rootdir");

            var copyOptions = new CopyOptions(
                rootdirDesc: rootdirDesc)
            {
                ObjectMapping = mapping,
                CommitEvenIfEmpty = options.CommitEvenIfEmpty,
                DirInSrcs = options.DirInSrcs,
                Branches = CmdUtil.Branches(options.BranchNames, rootdirRepo),
                MaxDepth = options.MaxDepth,
                Logger = logger,
            };

            var main = new CopyCommitMain(copyOptions);
            main.DoMain(rootdirRepo, subdirRepo, options.TraceLog
                ? new NopProgressBarCopyCommitProgressHandler()
                : new ConsoleProgressBarCopyCommitProgressHandler());

#if SUPPORT_REMOTE_REPOSITORY
            if (options.PushAfterCommit)
            {
                var branches = CmdUtil.Branches(options.BranchNames, subdirRepo, notFoundError: false);

                subdirRepo.Network.Push(branches, new PushOptions());
            }
#endif
        }
    }

    class NopProgressBarCopyCommitProgressHandler : ICopyCommitProgressHandler
    {
    }
}
