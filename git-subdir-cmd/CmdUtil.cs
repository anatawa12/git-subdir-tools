using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitSubdirTools.Libs;
using LibGit2Sharp;

namespace GitSubdirTools.Cmd
{
    public static class CmdUtil
    {
        public static IList<Branch> Branches(
            ICollection<string> branchNames,
            Repository          ofRepository,
            bool                notFoundError       = true,
            bool                remoteTrackingError = true)
        {
            if (branchNames.Count == 0)
            {
                return ofRepository.Branches
                    .Where(branch => !branch.IsRemote)
                    .ToArray();
            }
            else
            {
                var branches = new List<Branch>(branchNames.Count);
                foreach (var branchName in branchNames)
                {
                    var branch = ofRepository.Branches[branchName];
                    if (branch == null)
                    {
                        if (notFoundError)
                            throw new Exception($"branch '{branchName}' not found.");
                    }
                    else if (branch.IsRemote)
                    {
                        if (remoteTrackingError)
                            throw new Exception($"branch '{branchName}' is remote branch.");
                    }
                    else
                    {
                        branches.Add(branch);
                    }
                }

                return branches;
            }
        }

        public static Repository PrepareRepository(
            string repositoryDescriptor,
            string descriptorIsFor,
            bool   allowEmpty,
            bool   allowRemote,
            int    maxDepth,
            string mustLocalTailingMessage  = "",
            string mustExistsTailingMessage = "")
        {
            string repositoryPath;
#if SUPPORT_REMOTE_REPOSITORY
            if (UrlSchemas.Any(repositoryDescriptor.StartsWith))
            {
                if (!allowRemote)
                    throw new InvalidUsageException($"{descriptorIsFor} must be local repository"
                                                    + mustLocalTailingMessage + ".");
                var tempDir = Path.GetTempFileName();
                File.Delete(tempDir);
                Directory.CreateDirectory(tempDir);
                var options = new CloneOptions
                {
                    RecurseSubmodules = false
                };
                repositoryPath = Repository.Init(repositoryDescriptor);
                Directory.CreateDirectory(repositoryPath);
                
                repositoryPath = CommandGit.ShallowClone(repositoryDescriptor, tempDir, maxDepth);

                using var repo = new Repository(repositoryPath);
                foreach (var branch in repo.Branches)
                {
                    if (branch.IsRemote && branch.RemoteName == "origin")
                    {
                        var localName = branch.FriendlyName.RemovePrefix("origin/");
                        repo.Config.Set($"branch.{localName}.remote", "origin");
                        repo.Config.Set($"branch.{localName}.merge", $"refs/heads/{localName}");
                        repo.Branches.Add(localName, branch.Tip);
                    }
                }
            }
            else
#endif
            {
                repositoryPath = Path.Join(repositoryDescriptor, ".git");
                if (!Directory.Exists(repositoryPath))
                {
                    if (!allowEmpty)
                        throw new InvalidUsageException($"{descriptorIsFor} is not exists."
                                                        + mustExistsTailingMessage);

                    repositoryPath = Repository.Init(repositoryDescriptor);
                }
            }

            return new Repository(repositoryPath);
        }

#if SUPPORT_REMOTE_REPOSITORY
        private static readonly string[] UrlSchemas = {"file://", "http://", "https://", "ssh://", "git://"};
#endif

        public static string GetDescription(
            Repository descriptionRepository, string? descriptionByOption,
            Repository otherRepository,
            string     repositoryType)
        {
            if (descriptionByOption != null) return descriptionByOption;

            var descriptionRepositoryUrlString = descriptionRepository.Network.Remotes["origin"]?.Url;
            var otherRepositoryUriString       = otherRepository.Network.Remotes["origin"]?.Url;
            if (descriptionRepositoryUrlString != null)
            {
                var descriptionRepositoryUri = new Uri(descriptionRepositoryUrlString);
                if (otherRepositoryUriString == null)
                {
                    return descriptionRepositoryUri.ToString();
                }
                else
                {
                    var subdirUri = new Uri(otherRepositoryUriString);

                    var rootdirService = GetHostingService(descriptionRepositoryUri);
                    var subdirService  = GetHostingService(subdirUri);

                    var canUseDescriptor = rootdirService != HostingService.NotFound && rootdirService == subdirService;

                    if (canUseDescriptor)
                    {
                        return descriptionRepositoryUri.PathAndQuery.RemoveSuffix(".git").RemovePrefix("/");
                    }
                    else
                    {
                        return descriptionRepositoryUri.ToString();
                    }
                }
            }

            throw new InvalidUsageException($"remote repository of {repositoryType} not found ");
        }

        private static HostingService GetHostingService(Uri uri)
        {
            for (var i = (HostingService) 0; i < HostingService.MaxId; i++)
            {
                if (_hostingServiceHosts[i] == uri.Host)
                    return i;
            }

            return HostingService.NotFound;
        }

        private static Dictionary<HostingService, string> _hostingServiceHosts = new Dictionary<HostingService, string>
        {
            {HostingService.GithubCom, "github.com"},
            {HostingService.GitlabCom, "gitlab.com"},
            {HostingService.BitbucketOrg, "bitbucket.org"},
        };
    }

    internal enum HostingService
    {
        GithubCom,    // github.com/{identifier}.git
        GitlabCom,    // gitlab.com/{identifier}.git
        BitbucketOrg, // bitbucket.org/{identifier}.git

        MaxId    = BitbucketOrg,
        NotFound = -1
    }
}
