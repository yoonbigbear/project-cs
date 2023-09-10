﻿using MessagePack;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
		SocketAsyncEventArgs _sendEventArg;

		public bool IsSocketDisposed { get; private set; }
		public bool IsConnected { get; private set; }
		public int BytesReceived { get; private set; }
		public bool IsDIsposed { get; private set; }

		public void Dispose()
		{
			// finalize는 사실상 언제 호출될지 모르므로 대신하는 역할
			Dispose(true);
			
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
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

			IsSocketDisposed = false;

			//NetworkStream을 이용한 Tcp소켓 프로그래밍. 
			var stream = new NetworkStream(socket);

			//Reader Writer 셋
			reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: ushort.MaxValue));
			writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

			// Send/Recv event 콜백 등록
			_recvEventArg = new();
			_recvEventArg.Completed += OnAsyncComplete;
			_sendEventArg = new();
			_sendEventArg.Completed += OnAsyncComplete;

			//keep alive option?

			OnConnect();

			IsConnected = true;

			//Start Receive
			ReceiveAsync();

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

			reader.CompleteAsync();
			writer.CompleteAsync();

			OnDisconnected();

			return true;
		}

		void OnAsyncComplete(object sender, SocketAsyncEventArgs args)
		{
			if (IsSocketDisposed)
				return;

			switch(args.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					if (ProcessReceive(args))
					{

					}
					break;
				case SocketAsyncOperation.Send:
					if (ProcessSend(args))
					{

					}
					break;
				default:
					throw new ArgumentException("");
			}
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
				Error(ec);
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
				//receive handler
			}
			if (ec != SocketError.Success)
			{
				Error(ec);
				Disconnect();
			}

			return received;
		}

		public virtual bool SendAsync(ReadOnlySpan<byte> buffer)
		{
			if (!IsConnected)
				return false;

			if (buffer.IsEmpty)
				return true;

			writer.WriteAsync(buffer.ToArray());

			return true;
		}

		public virtual async void ReceiveAsync()
		{
			try
			{
				ReadResult result = await reader.ReadAsync();
				var buffer = result.Buffer;

				// read 종료.
				if (buffer.IsEmpty)
				{
					Disconnect();
					return;
				}

				var remainData = buffer.Length;
				if (remainData == 0)
				{
					Disconnect();
					return;
				}

				var message = MessagePackSerializer.Deserialize<ChatMP>(buffer);
				var json = MessagePackSerializer.ConvertToJson(buffer);
				Console.Write($"{message}: ");
				Console.WriteLine(json);

				reader.AdvanceTo(buffer.Start, buffer.End);

				//클라이언트 리스폰드
				{
					var chat = new ChatMP
					{
						Name = "client",
						Message = "Hello server",
					};
					var bytes = MessagePackSerializer.Serialize(chat);
					_socket.Send(bytes);
				}

				//다음 패킷 receive
				ReceiveAsync();
			}
			catch (Exception ex)
			{
				throw ex;
			}

		}

		bool ProcessReceive(SocketAsyncEventArgs e)
		{
			if (!IsConnected) return false;

			var size = e.BytesTransferred;

			if (size > 0)
			{
				BytesReceived += size;
			}

			if (e.SocketError == SocketError.Success)
			{
				if (size > 0)
					return true;
				else
					Disconnect();
			}
			else
			{
				//Error
				Error(e.SocketError);
				Disconnect();
			}
			return false;
		}

		bool ProcessSend(SocketAsyncEventArgs e)
		{
			if (!IsConnected)
				return false;

			var size = e.BytesTransferred;
			if (size > 0)
			{ 
				//OnSent
			}

			if (e.SocketError == SocketError.Success)
				return true;
			else
			{
				//error
				Error(e.SocketError);
				Disconnect();
				return false;
			}
		}

		void Error(SocketError error)
		{
			if ((error == SocketError.ConnectionAborted) ||
				(error == SocketError.ConnectionRefused) ||
				(error == SocketError.ConnectionReset) ||
				(error == SocketError.Shutdown))
				return;

			OnError(error);
		}

		void OnError(SocketError er)
		{

		}

		protected void OnConnect() { }
		protected void OnConnected() { }
		protected void OnDisconnect() { }
		protected void OnDisconnected() { }
	}
}
