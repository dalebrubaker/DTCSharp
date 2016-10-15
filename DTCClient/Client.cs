using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using DTCPB;

namespace DTCClient
{
    public class Client : IDisposable
    {
        private readonly string _server;
        private readonly int _port;
        private readonly Timer _heartbeatTimer;
        private bool _isDisposed;
        private const int HeartbeatInterval = 3 * 60 * 1000; // milliseconds

        public Client(string server, int port)
        {
            _server = server;
            _port = port;
            _heartbeatTimer = new Timer(HeartbeatInterval);
            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
        }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            _heartbeatTimer.Start();
        }

        public void Disconnect()
        {
            _heartbeatTimer.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _heartbeatTimer.Dispose();
                _isDisposed = true;
            }
        }
    }
}
