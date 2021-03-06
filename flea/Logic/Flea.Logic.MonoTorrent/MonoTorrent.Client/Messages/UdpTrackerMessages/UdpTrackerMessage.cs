namespace MonoTorrent.Client.Messages.UdpTracker
{
    public abstract class UdpTrackerMessage : Message
    {
        #region Internals

        private int action;
        private int transactionId;

        public int Action
        {
            get { return action; }
        }

        public int TransactionId
        {
            get { return transactionId; }
            protected set { transactionId = value; }
        }

        #endregion

        #region Constructor

        public UdpTrackerMessage(int action, int transactionId)
        {
            this.action = action;
            this.transactionId = transactionId;
        }

        #endregion

        #region Members

        public static UdpTrackerMessage DecodeMessage(byte[] buffer, int offset, int count, MessageType type)
        {
            UdpTrackerMessage m = null;
            int action = type == MessageType.Request ? ReadInt(buffer, offset + 8) : ReadInt(buffer, offset);
            switch (action)
            {
                case 0:
                    if (type == MessageType.Request)
                        m = new ConnectMessage();
                    else
                        m = new ConnectResponseMessage();
                    break;
                case 1:
                    if (type == MessageType.Request)
                        m = new AnnounceMessage();
                    else
                        m = new AnnounceResponseMessage();
                    break;
                case 2:
                    if (type == MessageType.Request)
                        m = new ScrapeMessage();
                    else
                        m = new ScrapeResponseMessage();
                    break;
                case 3:
                    m = new ErrorMessage();
                    break;
                default:
                    throw new ProtocolException($"Invalid udp message received: {buffer[offset]}");
            }

            try
            {
                m.Decode(buffer, offset, count);
            }
            catch
            {
                m = new ErrorMessage(0, "Couldn't decode the tracker response");
            }
            return m;
        }

        protected static void ThrowInvalidActionException()
        {
            throw new MessageException("Invalid value for 'Action'");
        }

        #endregion
    }
}