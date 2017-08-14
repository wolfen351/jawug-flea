using System.Collections.Generic;

namespace MonoTorrent.Client.Messages.UdpTracker
{
    class ScrapeResponseMessage : UdpTrackerMessage
    {
        #region Internals

        private List<ScrapeDetails> scrapes;

        public override int ByteLength
        {
            get { return 8 + (scrapes.Count*12); }
        }

        public List<ScrapeDetails> Scrapes
        {
            get { return scrapes; }
        }

        #endregion

        #region Constructor

        public ScrapeResponseMessage()
            : this(0, new List<ScrapeDetails>())
        {
        }

        public ScrapeResponseMessage(int transactionId, List<ScrapeDetails> scrapes)
            : base(2, transactionId)
        {
            this.scrapes = scrapes;
        }

        #endregion

        #region Members

        public override void Decode(byte[] buffer, int offset, int length)
        {
            if (Action != ReadInt(buffer, ref offset))
                ThrowInvalidActionException();
            TransactionId = ReadInt(buffer, ref offset);
            while (offset <= (buffer.Length - 12))
            {
                int seeds = ReadInt(buffer, ref offset);
                int complete = ReadInt(buffer, ref offset);
                int leeches = ReadInt(buffer, ref offset);
                scrapes.Add(new ScrapeDetails(seeds, leeches, complete));
            }
        }

        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;

            written += Write(buffer, written, Action);
            written += Write(buffer, written, TransactionId);
            for (int i = 0; i < scrapes.Count; i++)
            {
                written += Write(buffer, written, scrapes[i].Seeds);
                written += Write(buffer, written, scrapes[i].Complete);
                written += Write(buffer, written, scrapes[i].Leeches);
            }

            return written - offset;
        }

        #endregion
    }
}