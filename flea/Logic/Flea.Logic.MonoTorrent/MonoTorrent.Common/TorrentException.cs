using System;
using System.Runtime.Serialization;

namespace MonoTorrent.Common
{
    [Serializable]
    public class TorrentException : Exception
    {
        #region Constructor

        public TorrentException()
            : base()
        {
        }

        public TorrentException(string message)
            : base(message)
        {
        }

        public TorrentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public TorrentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}