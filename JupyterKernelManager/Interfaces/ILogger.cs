namespace JupyterKernelManager
{
    /// <summary>
    /// Used for classes to write logging messages to.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Write a message at the default logging level.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        void Write(string message, params object[] parameters);

        /// <summary>
        /// Write a message at the specified logging level.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        void Write(int logLevel, string message, params object[] parameters);
    }
}