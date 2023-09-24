using Net;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;

public class ServerSession : TcpSession
{
	public string Address { get; set; }
	public int Port { get; set; }
	public EndPoint EndPoint { get; set; }
	public PacketHandler PacketHandler { get; set; } = new();

	SocketAsyncEventArgs _connectEventArg;

	public bool IsDisposed { get; private set; }
	public bool IsConnecting { get; protected set; }

	protected virtual Socket CreateSocket() { return new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); }

	public ServerSession(EndPoint endPoint, string address, int port) : base()
	{
		EndPoint = endPoint;
		Address = address;
		Port = port;
		_connectEventArg = new();
	}

	public bool ConnectAsync()
	{
		if (IsConnected || IsConnecting)
		{
			return false;
		}

		_connectEventArg.RemoteEndPoint = EndPoint;
		_connectEventArg.Completed += OnAsyncComplete;
		_recvEventArg = new();
		_recvEventArg.Completed += OnAsyncComplete;
		_sendEventArg = new();
		_sendEventArg.Completed += OnAsyncComplete;

		_socket = CreateSocket();

		IsSocketDisposed = false;

		IsConnecting = true;

		try
		{
			//서버에 연결.
			_socket.Connect(EndPoint);
			ProcessConnect(_connectEventArg);
		}
		catch (SocketException e)
		{
			_connectEventArg.Completed -= OnAsyncComplete;
			_recvEventArg.Completed -= OnAsyncComplete;
			_sendEventArg.Completed -= OnAsyncComplete;

			_socket.Close();

			_socket.Dispose();

			_connectEventArg.Dispose();
			_recvEventArg.Dispose();
			_sendEventArg.Dispose();

			//error
			Error(e.SocketErrorCode);

		}

		return true;
	}

	public void DisconnectAsync() => Disconnect();

	public bool ReconnectAsync()
	{
		Disconnect();
		return ConnectAsync();
	}

	void OnAsyncComplete(object? sender, SocketAsyncEventArgs e)
	{
		if (IsSocketDisposed)
			return;

		switch (e.LastOperation)
		{
			case SocketAsyncOperation.Connect:
				ProcessConnect(e);
				break;
			case SocketAsyncOperation.Receive:
				if (ProcessReceive(e))
				{

				}
				break;
			case SocketAsyncOperation.Send:
				if (ProcessSend(e))
				{

				}
				break;
			default:
				throw new ArgumentException("");
		}
	}

	void ProcessConnect(SocketAsyncEventArgs e)
	{
		IsConnected	= true;

		if (e.SocketError == SocketError.Success)
		{
			Console.WriteLine("Connected to server...");

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


			if (IsSocketDisposed)
				return;

		}
		else
		{
			//error
			Error(e.SocketError);
		}
	}

	#region Dispose
	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				// TODO: 관리형 상태(관리형 개체)를 삭제합니다.
				DisconnectAsync();
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