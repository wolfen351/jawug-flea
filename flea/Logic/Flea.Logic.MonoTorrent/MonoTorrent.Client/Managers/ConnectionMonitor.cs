using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     This class is used to track upload/download speed and bytes uploaded/downloaded for each connection
    /// </summary>
    public class ConnectionMonitor
    {
        #region Internals

        private SpeedMonitor dataDown;
        private SpeedMonitor dataUp;
        private object locker = new object();
        private SpeedMonitor protocolDown;
        private SpeedMonitor protocolUp;

        public long DataBytesDownloaded
        {
            get { return dataDown.Total; }
        }

        public long DataBytesUploaded
        {
            get { return dataUp.Total; }
        }

        public int DownloadSpeed
        {
            get { return dataDown.Rate + protocolDown.Rate; }
        }

        public long ProtocolBytesDownloaded
        {
            get { return protocolDown.Total; }
        }

        public long ProtocolBytesUploaded
        {
            get { return protocolUp.Total; }
        }

        public int UploadSpeed
        {
            get { return dataUp.Rate + protocolUp.Rate; }
        }

        #endregion

        #region Constructor

        internal ConnectionMonitor()
            : this(12)
        {
        }

        internal ConnectionMonitor(int averagingPeriod)
        {
            dataDown = new SpeedMonitor(averagingPeriod);
            dataUp = new SpeedMonitor(averagingPeriod);
            protocolDown = new SpeedMonitor(averagingPeriod);
            protocolUp = new SpeedMonitor(averagingPeriod);
        }

        #endregion

        #region Members

        internal void BytesSent(int bytesUploaded, TransferType type)
        {
            lock (locker)
            {
                if (type == TransferType.Data)
                    dataUp.AddDelta(bytesUploaded);
                else
                    protocolUp.AddDelta(bytesUploaded);
            }
        }

        internal void BytesReceived(int bytesDownloaded, TransferType type)
        {
            lock (locker)
            {
                if (type == TransferType.Data)
                    dataDown.AddDelta(bytesDownloaded);
                else
                    protocolDown.AddDelta(bytesDownloaded);
            }
        }

        internal void Reset()
        {
            lock (locker)
            {
                dataDown.Reset();
                dataUp.Reset();
                protocolDown.Reset();
                protocolUp.Reset();
            }
        }

        internal void Tick()
        {
            lock (locker)
            {
                dataDown.Tick();
                dataUp.Tick();
                protocolDown.Tick();
                protocolUp.Tick();
            }
        }

        #endregion
    }
}