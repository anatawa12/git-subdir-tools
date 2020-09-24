using System.Diagnostics;
using System.IO;
using LibGit2Sharp;

namespace GitSubdirTools.Libs
{
    public static class CommandGit
    {
        public static void ShallowFetch(Repository targetRepo, string origin, string hashSha)
        {
            var info = new ProcessStartInfo("git");
            info.ArgumentList.Add("fetch");
            info.ArgumentList.Add("--depth");
            info.ArgumentList.Add("1");
            info.ArgumentList.Add(origin);
            info.ArgumentList.Add(hashSha);
            info.WorkingDirectory = targetRepo.Info.Path;
            info.RedirectStandardOutput = false;
            info.RedirectStandardInput = false;
            info.RedirectStandardError = false;
            var process = new Process
            {
                StartInfo = info
            };
            process.Start();
            process.WaitForExit();
        }

        public static string ShallowClone(string url, string workDir, int depth)
        {
            var repositoryPath = Path.Join(workDir, ".git");
            var info           = new ProcessStartInfo("git");
            info.ArgumentList.Add("clone");
            info.ArgumentList.Add(url);
            info.ArgumentList.Add(workDir);
            if (depth > 0)
            {
                info.ArgumentList.Add("--depth");
                info.ArgumentList.Add(depth.ToString("D"));
            }

            info.WorkingDirectory = workDir;
            info.RedirectStandardOutput = false;
            info.RedirectStandardInput = false;
            info.RedirectStandardError = false;
            var process = new Process
            {
                StartInfo = info
            };
            process.Start();
            process.WaitForExit();
            return repositoryPath;
        }
    }
}
