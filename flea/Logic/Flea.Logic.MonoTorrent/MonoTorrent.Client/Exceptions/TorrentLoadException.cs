using System;
using System.Runtime.Serialization;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    public class TorrentLoadException : TorrentException
    {
        #region Constructor

        public TorrentLoadException()
            : base()
        {
        }


        public TorrentLoadException(string message)
            : base(message)
        {
        }


        public TorrentLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        public TorrentLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}