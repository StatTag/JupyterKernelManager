namespace JupyterKernelManager
{
    /// <summary>
    /// The default behavior of this logger is to eat all log messages.
    /// </summary>
    public class DefaultLogger : ILogger
    {
        public void Write(string message, params object[] parameters)
        {
        }

        public void Write(int logLevel, string message, params object[] parameters)
        {
        }
    }
}