using System;
using MonoTorrent.Client.Connections;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Common;

namespace MonoTorrent.Client.Encryption
{
    internal static class EncryptorFactory
    {
        #region Types

        private class EncryptorAsyncResult : AsyncResult
        {
            #region Internals

            public int Available;
            public byte[] Buffer;
            public IEncryption Decryptor;
            public IEncryption Encryptor;
            public IEncryptor EncSocket;
            public PeerId Id;
            public byte[] InitialData;
            public InfoHash[] SKeys;

            #endregion

            #region Constructor

            public EncryptorAsyncResult(PeerId id, AsyncCallback callback, object state)
                : base(callback, state)
            {
                Id = id;
                Decryptor = new PlainTextEncryption();
                Encryptor = new PlainTextEncryption();
            }

            #endregion
        }

        #endregion

        #region Static

        private static readonly AsyncCallback CompletedEncryptedHandshakeCallback = CompletedEncryptedHandshake;
        private static readonly AsyncIOCallback HandshakeReceivedCallback = HandshakeReceived;

        #endregion

        #region Members

        private static EncryptionTypes CheckRC4(PeerId id)
        {
            // If the connection is *not* incoming, then it will be associated with an Engine
            // so we can check what encryption levels the engine allows.
            EncryptionTypes t;
            if (id.Connection.IsIncoming)
                t = EncryptionTypes.All;
            else
                t = id.TorrentManager.Engine.Settings.AllowedEncryption;

            // We're allowed use encryption if the engine settings allow it and the peer supports it
            // Binary AND both the engine encryption and peer encryption and check what levels are supported
            t &= id.Peer.Encryption;
            return t;
        }

        internal static IAsyncResult BeginCheckEncryption(PeerId id, int bytesToReceive, AsyncCallback callback,
            object state)
        {
            return BeginCheckEncryption(id, bytesToReceive, callback, state, null);
        }

        internal static IAsyncResult BeginCheckEncryption(PeerId id, int bytesToReceive, AsyncCallback callback,
            object state, InfoHash[] sKeys)
        {
            EncryptorAsyncResult result = new EncryptorAsyncResult(id, callback, state);
            result.SKeys = sKeys;

            IConnection c = id.Connection;
            ClientEngine.MainLoop.QueueTimeout(TimeSpan.FromSeconds(10), delegate
            {
                if (id.Encryptor == null || id.Decryptor == null)
                    id.CloseConnection();
                return false;
            });

            try
            {
                // If the connection is incoming, receive the handshake before
                // trying to decide what encryption to use
                if (id.Connection.IsIncoming)
                {
                    result.Buffer = new byte[bytesToReceive];
                    NetworkIO.EnqueueReceive(c, result.Buffer, 0, result.Buffer.Length, null, null, null,
                        HandshakeReceivedCallback, result);
                }
                else
                {
                    EncryptionTypes usable = CheckRC4(id);
                    bool hasPlainText = Toolbox.HasEncryption(usable, EncryptionTypes.PlainText);
                    bool hasRC4 = Toolbox.HasEncryption(usable, EncryptionTypes.RC4Full) ||
                                  Toolbox.HasEncryption(usable, EncryptionTypes.RC4Header);
                    if (id.Engine.Settings.PreferEncryption)
                    {
                        if (hasRC4)
                        {
                            result.EncSocket = new PeerAEncryption(id.TorrentManager.InfoHash, usable);
                            result.EncSocket.BeginHandshake(id.Connection, CompletedEncryptedHandshakeCallback, result);
                        }
                        else
                        {
                            result.Complete();
                        }
                    }
                    else
                    {
                        if (hasPlainText)
                        {
                            result.Complete();
                        }
                        else
                        {
                            result.EncSocket = new PeerAEncryption(id.TorrentManager.InfoHash, usable);
                            result.EncSocket.BeginHandshake(id.Connection, CompletedEncryptedHandshakeCallback, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Complete(ex);
            }
            return result;
        }

        internal static void EndCheckEncryption(IAsyncResult result, out byte[] initialData)
        {
            EncryptorAsyncResult r = (EncryptorAsyncResult) result;

            if (!r.IsCompleted)
                r.AsyncWaitHandle.WaitOne();

            if (r == null)
                throw new ArgumentException("Invalid async result");

            if (r.SavedException != null)
                throw r.SavedException;

            r.Id.Encryptor = r.Encryptor;
            r.Id.Decryptor = r.Decryptor;
            initialData = r.InitialData;

            r.AsyncWaitHandle.Close();
        }

        private static void HandshakeReceived(bool succeeded, int count, object state)
        {
            EncryptorAsyncResult result = (EncryptorAsyncResult) state;
            IConnection connection = result.Id.Connection;

            try
            {
                if (!succeeded)
                    throw new EncryptionException("Couldn't receive the handshake");

                result.Available += count;
                HandshakeMessage message = new HandshakeMessage();
                message.Decode(result.Buffer, 0, result.Buffer.Length);
                bool valid = message.ProtocolString == VersionInfo.ProtocolStringV100;
                EncryptionTypes usable = CheckRC4(result.Id);

                bool canUseRC4 = Toolbox.HasEncryption(usable, EncryptionTypes.RC4Header) ||
                                 Toolbox.HasEncryption(usable, EncryptionTypes.RC4Full);
                // If encryption is disabled and we received an invalid handshake - abort!
                if (valid)
                {
                    result.InitialData = result.Buffer;
                    result.Complete();
                    return;
                }
                if (!canUseRC4 && !valid)
                {
                    result.Complete(new EncryptionException("Invalid handshake received and no decryption works"));
                    return;
                }
                if (canUseRC4)
                {
                    // The data we just received was part of an encrypted handshake and was *not* the BitTorrent handshake
                    result.EncSocket = new PeerBEncryption(result.SKeys, EncryptionTypes.All);
                    result.EncSocket.BeginHandshake(connection, result.Buffer, 0, result.Buffer.Length,
                        CompletedEncryptedHandshakeCallback, result);
                }
                else
                {
                    result.Complete();
                }
            }
            catch (Exception ex)
            {
                result.Complete(ex);
                return;
            }
        }

        private static void CompletedEncryptedHandshake(IAsyncResult result)
        {
            EncryptorAsyncResult r = (EncryptorAsyncResult) result.AsyncState;
            try
            {
                r.EncSocket.EndHandshake(result);

                r.Decryptor = r.EncSocket.Decryptor;
                r.Encryptor = r.EncSocket.Encryptor;
                r.InitialData = r.EncSocket.InitialData;
            }
            catch (Exception ex)
            {
                r.SavedException = ex;
            }

            r.Complete();

            result.AsyncWaitHandle.Close();
            //r.AsyncWaitHandle.Close();
        }

        #endregion
    }
}