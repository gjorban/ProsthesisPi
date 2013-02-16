using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net.Sockets;

using ProsthesisCore.Utility;

namespace ProsthesisClientTest
{
    internal class ProsthesisSocketClient
    {
        public delegate void OnSocketDataReceive(ProsthesisSocketClient source, byte[] data, int numBytes);
        /// <summary>
        /// Called when a connection response occurs. This is not guaranteed to be fired from the same thread 
        /// </summary>
        public event Action<ProsthesisSocketClient, bool> ConnectFinished = null;
        /// <summary>
        /// Called when a connection has been closed. This is not guaranteed to be fired from the same thread 
        /// </summary>
        public event Action<ProsthesisSocketClient> ConnectionClosed = null;

        /// <summary>
        /// Called to signal that data is available on the socket. This is guarantee to be called from another thread.
        /// </summary>
        private OnSocketDataReceive OnDataReceive = null;

        public TcpClient Client
        {
            get { return mTCPClient; }
        }

        public bool Connected { get { return mTCPClient == null ? false : mTCPClient.Connected; } }

        private TcpClient mTCPClient = null;
        private byte[] mBuffer = null;

        private string mEndPointIP = string.Empty;
        private int mPort = 0;

        private static AsyncCallback mConnectionCallback = null;
        private static AsyncCallback mDataReadyCallback = null;
        private static WaitCallback mSocketWorkerStartCallback = null;

        private Logger mLogger = null;

        /// <summary>
        /// Create a new prosthesis socket client
        /// </summary>
        /// <param name="onDataCallback">Callback for when data is available over the socket</param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="logger">The logger which this socket will use. Must be non-null</param>
        public ProsthesisSocketClient(OnSocketDataReceive onDataCallback, string ipAddress, int port, Logger logger)
        {
            mTCPClient = new TcpClient();
            OnDataReceive = onDataCallback;
            mEndPointIP = ipAddress;
            mPort = port;

            mLogger = logger;
            //Create a small buffer to appease the socket library
            mBuffer = new byte[4];

            mConnectionCallback = new AsyncCallback(OnConnection);
            mSocketWorkerStartCallback = new WaitCallback(OnConnectionStart);
            mDataReadyCallback = new AsyncCallback(OnDataAvailable);
        }

        public void StartConnect()
        {
            if (mTCPClient != null && !mTCPClient.Connected)
            {
                mTCPClient.BeginConnect(mEndPointIP, mPort, mConnectionCallback, this);
            }
        }

        public void Shutdown()
        {
            if (mTCPClient != null)
            {
                mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Closing connection to {0}", mEndPointIP));
                mTCPClient.Close();
                mTCPClient = null;
            }

            if (ConnectFinished != null)
            {
                ConnectFinished(this, false);
            }

            if (ConnectionClosed != null)
            {
                ConnectionClosed(this);
            }

            ConnectFinished = null;
            OnDataReceive = null;
        }

        /// <summary>
        /// Sends the specified bytes over the socket if connected
        /// </summary>
        /// <param name="data"></param>
        /// <param name="len"></param>
        public bool Send(byte[] data, int offset, int len)
        {
            if (mTCPClient != null && mTCPClient.Connected)
            {
                return mTCPClient.Client.Send(data, offset, len, SocketFlags.None) == len;
            }
            else
            {
                return false;
            }
        }


        #region Async callbacks
        private void OnConnection(IAsyncResult rs)
        {
            try
            {
                mTCPClient.EndConnect(rs);
                if (mTCPClient.Connected)
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Successfully connected to {0}", mTCPClient.Client.RemoteEndPoint.AddressFamily));
                    //Queue the rest of the job to be executed latter
                    ThreadPool.QueueUserWorkItem(mSocketWorkerStartCallback, this);

                }
                else
                {
                    mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Failed to  connection to {0}", mTCPClient.Client.RemoteEndPoint.AddressFamily));
                }

                if (ConnectFinished != null)
                {
                    ConnectFinished(this, mTCPClient.Connected);
                }
                ConnectFinished = null;
            }
            catch (Exception e)
            {
                mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Exception caught while attempting to connect: {0}", e));
                Shutdown();
            }
        }

        private void OnConnectionStart(object context)
        {
            try
            {
                mTCPClient.Client.BeginReceive(mBuffer, 0, 0, SocketFlags.None, mDataReadyCallback, this);
            }
            catch (Exception e)
            {
                mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Caught exception when starting data receiving from {0}: {1}", mTCPClient.Client.RemoteEndPoint.AddressFamily, e));
                Shutdown();
            }
        }

        private void OnDataAvailable(IAsyncResult rs)
        {
            try
            {
                if (Client == null || !Client.Connected)
                {
                    return;
                }
                Client.Client.EndReceive(rs);
            }
            catch
            {
                Shutdown();
            }

            if (!Client.Connected || Client.Available == 0)
            {
                Shutdown();
            }
            else
            {
                //Receive data here
                NetworkStream stream = Client.GetStream();
                while (Client.Connected && stream.DataAvailable)
                {
                    byte[] buff = new byte[1024];
                    int readCount = stream.Read(buff, 0, 1024);
                    if (readCount > 0)
                    {
                        try
                        {
                            if (OnDataReceive != null)
                            {
                                OnDataReceive(this, buff, readCount);
                            }
                        }
                        catch (Exception e)
                        {
                            mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Exception caught when firing OnData callback: {0}", e));
                        }
                        /*  mPacketParser.AddData(buff, readCount);

                          try
                          {
                              while (mPacketParser.MoveNext())
                              {
                                  ProsthesisCore.Messages.ProsthesisMessage msg = mPacketParser.Current;
                                  if (msg != null)
                                  {
                                      if (msg is ProsthesisCore.Messages.ProsthesisHandshakeResponse)
                                      {
                                          ProsthesisCore.Messages.ProsthesisHandshakeResponse hsResp = msg as ProsthesisCore.Messages.ProsthesisHandshakeResponse;
                                          mLogger.LogMessage(Logger.LoggerChannels.Network, string.Format("Got response. Auth is {0}", hsResp.AuthorizedConnection));
                                      }
                                  }
                              }
                          }
                          catch (Exception e)
                          {
                              mLogger.LogMessage(Logger.LoggerChannels.Faults, string.Format("Caught proto exception {0}", e));
                          }*/
                    }
                }

                Client.Client.BeginReceive(mBuffer, 0, 0, SocketFlags.None,
                    mDataReadyCallback, this);
            }
        }
        #endregion
    }
}
