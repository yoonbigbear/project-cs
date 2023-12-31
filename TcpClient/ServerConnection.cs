﻿using Google.Protobuf;
using NetCore;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

public class ServerConnection : TcpSession
{
	public EndPoint EndPoint { get; set; }
	public PacketHandler PacketHandler { get; set; } = new();

	SocketAsyncEventArgs _connectEventArg;

	public bool IsDisposed { get; private set; }
	public bool IsConnecting { get; protected set; }

	protected virtual Socket CreateSocket() { return new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); }

	public ServerConnection(EndPoint endPoint) : base()
	{
		EndPoint = endPoint;
		_connectEventArg = new();
	}

	public async void ConnectAsync()
	{
		if (IsConnected || IsConnecting)
		{
			return;
		}

		_socket = CreateSocket();

		IsSocketDisposed = false;

		IsConnecting = true;

		try
		{
			//서버에 연결.
			await _socket.ConnectAsync(EndPoint);
			if (_socket.Connected)
			{
				//Console.WriteLine("Connect with server...");

				//NetworkStream을 이용한 Tcp소켓 프로그래밍. 
				var stream = new NetworkStream(_socket);

				//Reader Writer 셋
				_reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: 1470));
				_writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

				IsConnected = true;

				//Receive
				ReceiveAsync();

				CreateAccountREQ req = new CreateAccountREQ
				{
					Name = $"클라이언트 {GetHashCode()}",
				};
				Serialize((ushort)PacketId.CREATEACCOUNTREQ, req.ToByteArray());
				Console.WriteLine(Interlocked.Increment(ref ClientMain.globalcount));


				if (IsSocketDisposed)
					return;

			}
			else
			{
				//error
				Error(SocketError.NotConnected);
			}
		}
		catch (SocketException e)
		{
			_socket.Close();

			_socket.Dispose();

			_connectEventArg.Dispose();

			//error
			Error(e.SocketErrorCode);

			return;
		}
		return;
	}

	public void Serialize(ushort id, byte[] bytes)
	{
		Span<byte> buf = stackalloc byte[4 + bytes.Length];

		buf[0] = (byte)(((ushort)id) & 0x00FF);
		buf[1] = (byte)((((ushort)id) & 0xFF00) >> 8);
		buf[2] = (byte)(((ushort)bytes.Length) & 0x00FF);
		buf[3] = (byte)((((ushort)bytes.Length) & 0xFF00) >> 8);
		bytes.CopyTo(buf.Slice(4));

		Send(buf);
	}

	protected override void OnPacketRead(ArraySegment<byte> buf) 
	{
		PacketHandler.Push(this, buf);
	}

	protected override void OnDisconnected()
	{
		Console.WriteLine($"Disconnected... ");
	}

	protected override void OnError(SocketError er)
	{
		base.OnError(er);
		Console.WriteLine($"error {er.GetTypeCode()}");
	}

	#region Dispose
	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				// TODO: 관리형 상태(관리형 개체)를 삭제합니다.
				Disconnect();
			}

			// TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
			// TODO: 큰 필드를 null로 설정합니다.
			IsDisposed = true;
		}
	}

	// // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
	// ~TcpClient()
	// {
	//     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
	//     Dispose(disposing: false);
	// }
	#endregion
}