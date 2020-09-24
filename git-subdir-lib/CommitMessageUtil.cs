using System;
using System.Linq;
using LibGit2Sharp;

namespace GitSubdirTools.Libs
{
    public static class CommitMessageUtil
    {
        public const string BasedirPrefix = "Git-Subdir-Tools-Basedir-Commit: ";
        public const string SubdirPrefix  = "Git-Subdir-Tools-Subdir-Commit: ";

        public static ObjectId? ReadBasedirCommitNameFromMessage(string message)
        {
            string[] lastTwoLines = message.Split('\n').TakeLast(2).ToArray();

            if (lastTwoLines.Length != 2) return null;
            if (!lastTwoLines[0].StartsWith(BasedirPrefix)) return null;
            if (lastTwoLines[1].Length != 0) return null;

            return ParseCommitRefToCommitHash(lastTwoLines[0].AsSpan(BasedirPrefix.Length));
        }

        public static ObjectId? ReadSubdirCommitNameFromMessage(string message, string subdirPath)
        {
            var prefix = $"{SubdirPrefix}{subdirPath}:";
            foreach (var lineStr in message.Split('\n'))
            {
                var line = lineStr.AsSpan();
                if (!line.StartsWith(prefix)) continue;
                var commitRef = line[prefix.Length..].RemovePrefix(" ");
                var hash      = ParseCommitRefToCommitHash(commitRef);
                if (hash == null) continue;
                return hash;
            }

            return null;
        }

        private static ObjectId? ParseCommitRefToCommitHash(ReadOnlySpan<char> commitRef)
        {
            var atIndex = commitRef.IndexOf('@');
            if (atIndex == -1) return null;
            var region = commitRef[(atIndex + 1)..];
            foreach (var c in region)
                if (!('0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F'))
                    return null;

            return new ObjectId(region.ToString());
        }
    }
}
