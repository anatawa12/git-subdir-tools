using System;

namespace GitSubdirTools.Cmd
{
    public class InvalidUsageException : Exception
    {
        public InvalidUsageException(string message) : base(message)
        {
        }
    }
}
