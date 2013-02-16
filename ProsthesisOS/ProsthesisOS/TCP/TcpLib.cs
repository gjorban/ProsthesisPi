using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections;

namespace ProsthesisOS.TCP
{
    /// <SUMMARY>
    /// This class holds useful information for keeping track of each client connected
    /// to the server, and provides the means for sending/receiving data to the remote
    /// host.
    /// </SUMMARY>
    public class ConnectionState
    {
        internal Socket _conn;
        internal TcpServer _server;
        internal TcpServiceProvider _provider;
        internal byte[] _buffer;

        /// <SUMMARY>
        /// Tells you the IP Address of the remote host.
        /// </SUMMARY>
        public EndPoint RemoteEndPoint
        {
            get { return _conn.RemoteEndPoint; }
        }

        /// <SUMMARY>
        /// Returns the number of bytes waiting to be read.
        /// </SUMMARY>
        public int AvailableData
        {
            get { return _conn.Available; }
        }

        /// <SUMMARY>
        /// Tells you if the socket is connected.
        /// </SUMMARY>
        public bool Connected
        {
            get { return _conn.Connected; }
        }

        /// <SUMMARY>
        /// Reads data on the socket, returns the number of bytes read.
        /// </SUMMARY>
        public int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_conn.Available > 0)
                {
                    return _conn.Receive(buffer, offset, count, SocketFlags.None);
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <SUMMARY>
        /// Sends Data to the remote host.
        /// </SUMMARY>
        public bool Write(byte[] buffer, int offset, int count)
        {
            try
            {
                _conn.Send(buffer, offset, count, SocketFlags.None);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <SUMMARY>
        /// Ends connection with the remote host.
        /// </SUMMARY>
        public void EndConnection()
        {
            if (_conn != null && _conn.Connected)
            {
                _provider.OnDropConnection(this);
                _conn.Shutdown(SocketShutdown.Both);
                _conn.Close();
            }
            _server.DropConnection(this);
        }
    }

    /// <SUMMARY>
    /// Allows to provide the server with the actual code that is goint to service
    /// incoming connections.
    /// </SUMMARY>
    public abstract class TcpServiceProvider : ICloneable
    {
        /// <SUMMARY>
        /// Provides a new instance of the object.
        /// </SUMMARY>
        public virtual object Clone()
        {
            throw new Exception("Derived clases must override Clone method.");
        }

        /// <SUMMARY>
        /// Gets executed when the server accepts a new connection.
        /// </SUMMARY>
        public abstract void OnAcceptConnection(ConnectionState state);

        /// <SUMMARY>
        /// Gets executed when the server detects incoming data.
        /// This method is called only if OnAcceptConnection has already finished.
        /// </SUMMARY>
        public abstract void OnReceiveData(ConnectionState state);

        /// <SUMMARY>
        /// Gets executed when the server needs to shutdown the connection.
        /// </SUMMARY>
        public abstract void OnDropConnection(ConnectionState state);
    }

    public class TcpServer
    {
        private int mPort;
        private Socket mListener;
        private TcpServiceProvider mProvider;
        private ArrayList mConnections;
        private int mMaxConnections = 100;

        private AsyncCallback ConnectionReady;
        private WaitCallback AcceptConnection;
        private AsyncCallback ReceivedDataReady;

        public bool Active
        {
            get { return mListener != null ? mListener.Connected : false; }
        }

        /// <SUMMARY>
        /// Initializes server. To start accepting connections call Start method.
        /// </SUMMARY>
        public TcpServer(TcpServiceProvider provider, int port)
        {
            mProvider = provider;
            mPort = port;
            mListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
              ProtocolType.Tcp);
            mConnections = new ArrayList();
            ConnectionReady = new AsyncCallback(ConnectionReady_Handler);
            AcceptConnection = new WaitCallback(AcceptConnection_Handler);
            ReceivedDataReady = new AsyncCallback(ReceivedDataReady_Handler);
        }


        /// <SUMMARY>
        /// Start accepting connections.
        /// A false return value tell you that the port is not available.
        /// </SUMMARY>
        public bool Start()
        {
            try
            {
                mListener.Bind(new IPEndPoint(IPAddress.Any, mPort));
                mListener.Listen(100);
                mListener.BeginAccept(ConnectionReady, null);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <SUMMARY>
        /// Callback function: A new connection is waiting.
        /// </SUMMARY>
        private void ConnectionReady_Handler(IAsyncResult ar)
        {
            lock (this)
            {
                if (mListener == null)
                {
                    return;
                }

                Socket conn = mListener.EndAccept(ar);
                if (mConnections.Count >= mMaxConnections)
                {
                    //Max number of connections reached.
                    string msg = "SE001: Server busy";
                    conn.Send(Encoding.UTF8.GetBytes(msg), 0, msg.Length, SocketFlags.None);
                    conn.Shutdown(SocketShutdown.Both);
                    conn.Close();
                }
                else
                {
                    //Start servicing a new connection
                    ConnectionState st = new ConnectionState();
                    st._conn = conn;
                    st._server = this;
                    st._provider = (TcpServiceProvider)mProvider.Clone();
                    st._buffer = new byte[4];
                    mConnections.Add(st);
                    //Queue the rest of the job to be executed latter
                    ThreadPool.QueueUserWorkItem(AcceptConnection, st);
                }
                //Resume the listening callback loop
                mListener.BeginAccept(ConnectionReady, null);
            }
        }


        /// <SUMMARY>
        /// Executes OnAcceptConnection method from the service provider.
        /// </SUMMARY>
        private void AcceptConnection_Handler(object state)
        {
            ConnectionState st = state as ConnectionState;
            try 
            { 
                st._provider.OnAcceptConnection(st); 
            }
            catch
            {
                //report error in provider... Probably to the EventLog
            }
            //Starts the ReceiveData callback loop
            if (st._conn.Connected)
            {
                st._conn.BeginReceive(st._buffer, 0, 0, SocketFlags.None,
                  ReceivedDataReady, st);
            }
        }

        /// <SUMMARY>
        /// Executes OnReceiveData method from the service provider.
        /// </SUMMARY>
        private void ReceivedDataReady_Handler(IAsyncResult ar)
        {
            ConnectionState st = ar.AsyncState as ConnectionState;
            try
            {
                st._conn.EndReceive(ar);
            }
            catch
            {
                DropConnection(st);
                return;
            }
            //Im considering the following condition as a signal that the
            //remote host droped the connection.
            if (st._conn.Available == 0)
            {
                DropConnection(st);
            }
            else
            {
                try 
                { 
                    st._provider.OnReceiveData(st); 
                }
                catch
                {
                    //report error in the provider
                }
                //Resume ReceivedData callback loop
                if (st._conn.Connected)
                {
                    st._conn.BeginReceive(st._buffer, 0, 0, SocketFlags.None,
                      ReceivedDataReady, st);
                }
            }
        }


        /// <SUMMARY>
        /// Shutsdown the server
        /// </SUMMARY>
        public void Stop()
        {
            lock (this)
            {
                mListener.Close();
                mListener = null;
                //Close all active connections
                foreach (object obj in mConnections)
                {
                    ConnectionState st = obj as ConnectionState;
                    try 
                    { 
                        st._provider.OnDropConnection(st); 
                    }
                    catch
                    {
                        //some error in the provider
                    }
                    st._conn.Shutdown(SocketShutdown.Both);
                    st._conn.Close();
                }
                mConnections.Clear();
            }
        }


        /// <SUMMARY>
        /// Removes a connection from the list
        /// </SUMMARY>
        internal void DropConnection(ConnectionState st)
        {
            lock (this)
            {
                st._provider.OnDropConnection(st);
                st._conn.Shutdown(SocketShutdown.Both);
                st._conn.Close();
                if (mConnections.Contains(st))
                {
                    mConnections.Remove(st);
                }
            }
        }


        public int MaxConnections
        {
            get
            {
                return mMaxConnections;
            }
            set
            {
                mMaxConnections = value;
            }
        }


        public int CurrentConnections
        {
            get
            {
                lock (this) { return mConnections.Count; }
            }
        }
    }
}