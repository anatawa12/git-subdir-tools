namespace GitSubdirTools.Libs
{
    public class Logger
    {
        public bool TraceEnabled = false;

        public void Trace(string startLog)
        {
            if (TraceEnabled)
                System.Console.WriteLine(startLog);
        }

        public void Warn(string s)
        {
            System.Console.WriteLine(s);
        }
    }
}
