using System.Net.Sockets;

namespace Net
{
	public class TcpSession : IDisposable
	{
		public Socket _socket;

		// Receives
		SocketAsyncEventArgs _recvEventArg;

		public bool IsSocketDisposed { get; private set; }

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		void Connect(Socket socket)
		{
			_socket = socket;

			IsSocketDisposed = false;

		}
	}
}
