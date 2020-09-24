using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using GitSubdirTools.Cmd;
using GitSubdirTools.Libs;
using GitSubdirTools.Libs.Back;

namespace GitSubdirTools.Back
{
    internal class CommandLineOptions : CommandLineOptionsBase
    {
        [Option("subdir-repo-desc",
            HelpText = "url or identifier of subdir repository.",
            MetaValue = "SUBDIR_REPO_DESC")]
        public string? SubdirRepoDesc { get; set; } = null;

        [Option('d', "dir-in-src", HelpText = "the directory which should be copied. " +
                                              "you can choose two or more directories " +
                                              "but only first directory which is found will only be used",
            MetaValue = "DIR_IN_SRC", Required = true)]
        public string DirInSrc { get; set; } = null!;
    }

    internal class Program
    {
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
                await new Program().DoMain(options);
                return 0;
            }
            catch (InvalidUsageException e)
            {
                Console.WriteLine();
                await Console.Error.WriteLineAsync(e.Message);
                return 1;
            }
        }

        private async ValueTask DoMain(CommandLineOptions options)
        {
            var logger = new Logger
            {
                TraceEnabled = options.TraceLog
            };

            CmdUtil.NoCacheWarn(options.CacheFilePath);

            await using var mapping = new ObjectIdCache(options.CacheFilePath);

            using var rootdirRepo = CmdUtil.PrepareRepository(options.RootdirPath, "rootdir repository",
                maxDepth: 1,
                allowEmpty: false, allowRemote: options.PushAfterCommit,
                mustLocalTailingMessage: " if --push is not specified");
            using var subdirRepo = CmdUtil.PrepareRepository(options.SubdirRepoPath, "subdir repository",
                maxDepth: options.MaxDepth,
                allowEmpty: false, allowRemote: true);

            var subdirDesc = CmdUtil.GetDescription(
                descriptionRepository: subdirRepo,
                descriptionByOption: options.SubdirRepoDesc,
                otherRepository: rootdirRepo,
                repositoryType: "subdir");

            var backCopyOptions = new BackCopyOptions(
                dirInSrc: options.DirInSrc,
                subdirDesc: subdirDesc)
            {
                ObjectMapping = mapping,
                Branches = CmdUtil.Branches(options.BranchNames, subdirRepo),
                MaxDepth = options.MaxDepth,
                Logger = logger,
            };

            var main = new BackCopyCommitMain(backCopyOptions);
            main.DoMain(rootdirRepo, subdirRepo, options.TraceLog
                ? new NopProgressBarCopyCommitProgressHandler()
                : new ConsoleProgressBarCopyCommitProgressHandler());

#if SUPPORT_REMOTE_REPOSITORY
            if (options.PushAfterCommit)
            {
                var branches = CmdUtil.Branches(options.BranchNames, rootdirRepo, 
                    notFoundError: false, remoteTrackingError: false);

                foreach (var branch in branches)
                {
                    if (branch.RemoteName == null)
                        rootdirRepo.Config.Set($"branch.{branch.FriendlyName}.remote", $"origin");
                }

                rootdirRepo.Network.Push(branches, new PushOptions());
            }
#endif
        }
    }

    class NopProgressBarCopyCommitProgressHandler : ICopyCommitProgressHandler
    {
    }
}
