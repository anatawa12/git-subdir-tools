using GitSubdirTools.Libs;
using LibGit2Sharp;
using ShellProgressBar;

namespace GitSubdirTools.Cmd
{
    public class ConsoleProgressBarCopyCommitProgressHandler : ICopyCommitProgressHandler
    {
        private readonly ProgressBar _progressBar;

        private string _runningStatus;

        public ConsoleProgressBarCopyCommitProgressHandler()
        {
            _progressBar = new ProgressBar(1, "copying commit", new ProgressBarOptions
            {
                ShowEstimatedDuration = false
            });
            _runningStatus = "copying commits...";
            _progressBar.MaxTicks = 0;
        }

        private void WriteBar()
        {
            var message = $"{_progressBar.CurrentTick}/{_progressBar.MaxTicks}: ";
            message += _runningStatus;
            _progressBar.Message = message;
        }

        public void OnStartCopy(Commit commit)
        {
            _runningStatus = $"tracing {commit!.Id.ToString(7)}...";
            WriteBar();
        }

        public void OnCopiedOneCommit(Commit commit, Commit? newCommit)
        {
            _runningStatus = $"{commit!.Id.ToString(7)} to {newCommit?.Id?.ToString(7) ?? "null"}";
            _progressBar.Tick();
            WriteBar();
        }

        public void OnFoundNewCommit(int newCommitCount)
        {
            _progressBar.MaxTicks += newCommitCount;
            WriteBar();
        }

        public void Dispose()
        {
            _progressBar.Dispose();
        }
    }
}
