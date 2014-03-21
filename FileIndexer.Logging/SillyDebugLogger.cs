using FileIndexer.Core.Interfaces;
using System.Diagnostics;

namespace FileIndexer.Logging
{
    // Does no good and a little bit of evil
    public sealed class SillyDebugLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.Write(message);
        }
    }
}