using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetCore
{
	public class TcpSession : IDisposable
	{
		public Socket _socket;

		// Receives
		protected PipeReader _reader { get; set; }
		protected PipeWriter _writer { get; set; }
		public TcpServer Server { get; private set; }
		public bool IsSocketDisposed { get; protected set; }
		public bool IsConnected { get; protected set; }
		public int BytesReceived { get; protected set; }
		public bool IsDIsposed { get; protected set; }

#pragma warning disable 8618
		public TcpSession(TcpServer server) => Server = server;
		public TcpSession() { }
#pragma warning restore 8618

		public void Dispose()
		{
			// finalize는 사실상 언제 호출될지 모르므로 대신하는 역할
			Dispose(disposing: true);

			// finallize 호출되지 않도록 설정
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDIsposed)
			{
				if (disposing)
				{
					//dispose managed resources
					Disconnect();
				}

				// dispose unmanaged resources

				// large field to null

				IsDIsposed = true;
			}
		}

		public void Connect(Socket socket)
		{
			_socket = socket;

			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
			//keep alive option?
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);

			IsSocketDisposed = false;

			//NetworkStream을 이용한 Tcp소켓 프로그래밍. 
			var stream = new NetworkStream(socket);

			//Reader Writer 셋
			_reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: 1470));
			_writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

			OnConnect();

			IsConnected = true;

			//Start Receive
			ReceiveAsync();

			// 가끔 disconnect로 받을수도잇음
			if (IsSocketDisposed)
				return;

			OnConnected();
		}

		protected bool Disconnect()
		{
			if (!IsConnected)
				return false;

			OnDisconnect();

			try
			{
				try
				{
					//클라 서버 양쪽 모두 연결해제
					_socket.Shutdown(SocketShutdown.Both);
				}
				catch (SocketException) { }
				finally
				{
					_socket.Close();
					_socket.Dispose();
				}

				IsSocketDisposed = true;
			}
			catch (ObjectDisposedException) { }

			IsConnected = false;

			_reader.CompleteAsync();
			_writer.CompleteAsync();

			OnDisconnected();

			return true;
		}

		public virtual bool SendAsync(byte[] buffer)
		{
			if (!IsConnected)
				return false;

			if (buffer.Length == 0)
				return true;

			_writer.WriteAsync(buffer);
			
			return true;
		}

		public virtual Int64 Send(ReadOnlySpan<byte> buffer)
		{
			if (!IsConnected) return 0;
			if (buffer.IsEmpty) return 0;

			var sent = _socket.Send(buffer, SocketFlags.None, out SocketError ec);
			if (sent > 0)
			{

			}

			if (ec != SocketError.Success)
			{
				Error(ec);
				Disconnect();
			}

			return sent;
		}

		public virtual async void ReceiveAsync()
		{
			try
			{
				while (true)
				{
					ReadResult result = await _reader.ReadAsync();
					var buffer = result.Buffer;

					//Socket 종료 이벤트 감지
					if (result.IsCompleted)
						break;

					if (buffer.IsEmpty)
						break;

					var remainData = buffer.Length;
					if (remainData == 0)
						break;

					OnPacketRead(buffer);

					_reader.AdvanceTo(buffer.Start, buffer.End);
				}
			}
			catch
			{
			}
			Disconnect();
		}

		protected void Error(SocketError error)
		{
			if ((error == SocketError.ConnectionAborted) ||
				(error == SocketError.ConnectionRefused) ||
				(error == SocketError.ConnectionReset) ||
				(error == SocketError.Shutdown))
				return;

			OnError(error);
		}

		protected virtual void OnError(SocketError er) { }
		protected virtual void OnConnect() { }
		protected virtual void OnConnected() { }
		protected virtual void OnDisconnect() { }
		protected virtual void OnDisconnected() { }
		protected virtual void OnPacketRead(ReadOnlySequence<byte> buf) { }
	}
}
