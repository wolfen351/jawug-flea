using System;
using System.Collections.Generic;
using MonoTorrent.Client.Messages;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    /// <summary>
    ///     Generates a sliding window with high, medium, and low priority sets. The high priority set is downloaded first and
    ///     in order.
    ///     The medium and low priority sets are downloaded rarest first.
    ///     This is intended to be used with a BitTorrent streaming application.
    ///     The high priority set represents pieces that are needed SOON. This set is updated by calling code, to adapt for
    ///     events
    ///     (e.g. user fast-forwards or seeks, etc.)
    /// </summary>
    public class SlidingWindowPicker : PiecePicker
    {
        #region Internals

        private int highPrioritySetSize; // size of high priority set, in pieces
        // this represents the last byte played in a video player, as the high priority
        // set designates pieces that are needed VERY SOON
        private int highPrioritySetStart; // gets updated by calling code, or as pieces get downloaded

        private int ratio = 4; // ratio from medium priority to high priority set size

        /// <summary>
        ///     Gets or sets the size, in pieces, of the high priority set.
        /// </summary>
        public int HighPrioritySetSize
        {
            get { return highPrioritySetSize; }
            set { highPrioritySetSize = value; }
        }

        /// <summary>
        ///     Gets or sets first "high priority" piece. The n pieces after this will be requested in-order,
        ///     the rest of the file will be treated rarest-first
        /// </summary>
        public int HighPrioritySetStart
        {
            get { return highPrioritySetStart; }
            set
            {
                if (highPrioritySetStart < value)
                    highPrioritySetStart = value;
            }
        }

        /// <summary>
        ///     Read-only value for size of the medium priority set. To set the medium priority size, use MediumToHighRatio.
        /// </summary>
        public int MediumPrioritySetSize
        {
            get { return highPrioritySetSize*ratio; }
        }

        public int MediumPrioritySetStart
        {
            get { return HighPrioritySetStart + HighPrioritySetSize + 1; }
        }

        /// <summary>
        ///     This is the size ratio between the medium and high priority sets. Equivalent to mu in Tribler's Give-to-get paper.
        ///     Default value is 4.
        /// </summary>
        public int MediumToHighRatio
        {
            get { return ratio; }
            set { ratio = value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Empty constructor for changing piece pickers
        /// </summary>
        public SlidingWindowPicker(PiecePicker picker)
            : base(picker)
        {
        }


        /// <summary>
        ///     Creates a new piece picker with support for prioritization of files. The sliding window will be positioned to the
        ///     start
        ///     of the first file to be downloaded
        /// </summary>
        /// <param name="bitField">The bitfield associated with the torrent</param>
        /// <param name="torrentFiles">The files that are available in this torrent</param>
        /// <param name="highPrioritySetSize">Size of high priority set</param>
        internal SlidingWindowPicker(PiecePicker picker, int highPrioritySetSize)
            : this(picker, highPrioritySetSize, 4)
        {
        }


        /// <summary>
        ///     Create a new SlidingWindowPicker with the given set sizes. The sliding window will be positioned to the start
        ///     of the first file to be downloaded
        /// </summary>
        /// <param name="bitField">The bitfield associated with the torrent</param>
        /// <param name="torrentFiles">The files that are available in this torrent</param>
        /// <param name="highPrioritySetSize">Size of high priority set</param>
        /// <param name="mediumToHighRatio">Size of medium priority set as a multiple of the high priority set size</param>
        internal SlidingWindowPicker(PiecePicker picker, int highPrioritySetSize, int mediumToHighRatio)
            : base(picker)
        {
            this.highPrioritySetSize = highPrioritySetSize;
            ratio = mediumToHighRatio;
        }

        #endregion

        #region Members

        /// <summary>
        /// </summary>
        /// <param name="ownBitfield"></param>
        /// <param name="files"></param>
        /// <param name="requests"></param>
        /// <param name="unhashedPieces"></param>
        public override void Initialise(BitField bitfield, TorrentFile[] files, IEnumerable<Piece> requests)
        {
            base.Initialise(bitfield, files, requests);

            // set the high priority set start to the beginning of the first file that we have to download
            foreach (TorrentFile file in files)
            {
                if (file.Priority == Priority.DoNotDownload)
                    highPrioritySetStart = file.EndPieceIndex;
                else
                    break;
            }
        }

        public override MessageBundle PickPiece(PeerId id, BitField peerBitfield, List<PeerId> otherPeers, int count,
            int startIndex, int endIndex)
        {
            MessageBundle bundle;
            int start, end;

            if (HighPrioritySetStart >= startIndex && HighPrioritySetStart <= endIndex)
            {
                start = HighPrioritySetStart;
                end = Math.Min(endIndex, HighPrioritySetStart + HighPrioritySetSize - 1);
                if ((bundle = base.PickPiece(id, peerBitfield, otherPeers, count, start, end)) != null)
                    return bundle;
            }

            if (MediumPrioritySetStart >= startIndex && MediumPrioritySetStart <= endIndex)
            {
                start = MediumPrioritySetStart;
                end = Math.Min(endIndex, MediumPrioritySetStart + MediumPrioritySetSize - 1);
                if ((bundle = base.PickPiece(id, peerBitfield, otherPeers, count, start, end)) != null)
                    return bundle;
            }

            return base.PickPiece(id, peerBitfield, otherPeers, count, startIndex, endIndex);
        }

        #endregion
    }
}