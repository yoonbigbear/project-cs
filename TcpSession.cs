using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;

namespace Net
{
	public class TcpSession : IDisposable
	{
		public Socket _socket;

		// Receives
		protected PipeReader reader { get; set; }
		SocketAsyncEventArgs _recvEventArg;

		// Send
		protected PipeWriter writer { get; set; }
		Memory<byte> _sendBuffer = new Memory<byte>();
		SocketAsyncEventArgs _sendEventArg;

		public bool IsSocketDisposed { get; private set; }
		public bool IsConnected { get; private set; }

		public void Dispose()
		{
			reader.Complete();
			throw new NotImplementedException();
		}

		public void Connect(Socket socket)
		{
			_socket = socket;

			IsSocketDisposed = false;

			//NetworkStream을 이용한 Tcp소켓 프로그래밍. 
			var stream = new NetworkStream(socket);

			//Reader Writer 셋
			PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: ushort.MaxValue));
			PipeWriter.Create(stream);

			// Send/Recv event 콜백 등록
			_recvEventArg = new();
			_recvEventArg.Completed += OnAsyncComplete;
			_sendEventArg = new();
			_sendEventArg.Completed += OnAsyncComplete;

			//keep alive option?

			OnConnect();

			IsConnected = true;

			//Start Receive


			// 가끔 disconnect로 받을수도잇음
			if (IsSocketDisposed)
				return;

			OnConnected();
		}

		bool Disconnect()
		{
			if (!IsConnected)
				return false;

			_recvEventArg.Completed -= OnAsyncComplete;
			_sendEventArg.Completed -= OnAsyncComplete;

			OnDisconnect();

			try
			{
				try
				{
					//클라 서버 양쪽 모두 연결해제
					_socket.Shutdown(SocketShutdown.Both);
				}
				catch (SocketException) { }

				_socket.Close();
				_socket.Dispose();
				_recvEventArg.Dispose();
				_sendEventArg.Dispose();

				IsSocketDisposed = true;
			}
			catch (ObjectDisposedException) { }

			IsConnected = false;

			OnDisconnected();

			return true;
		}

		void OnAsyncComplete(object sender, SocketAsyncEventArgs args)
		{

		}

		public virtual Int64 Send(ReadOnlySpan<byte> buffer)
		{
			if (!IsConnected) return 0;
			if (buffer.IsEmpty) return 0;
			
			var sent = _socket.Send(buffer, SocketFlags.None, out SocketError ec);
			if (sent> 0)
			{
				
			}

			if (ec != SocketError.Success)
			{
				Debug.Assert(false);
				Disconnect();
			}

			return sent;
		}

		public virtual Int64 Receive(byte[] buffer, long offset, long size)
		{
			if (!IsConnected) return 0;
			if (size == 0) return 0;

			var received = _socket.Receive(buffer, (int)offset, (int)size, SocketFlags.None, out SocketError ec);
			if (received > 0)
			{

			}
			if (ec != SocketError.Success)
			{
				Debug.Assert(false);
				Disconnect();
			}

			return received;
		}

		protected void OnConnect() { }
		protected void OnConnected() { }
		protected void OnDisconnect() { }
		protected void OnDisconnected() { }
	}
}
