using Net;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

public class TcpClient : IDisposable
{
	public string Address { get; set; }
	public int Port { get; set; }
	public EndPoint EndPoint { get; set; }

	PipeReader _reader;
	PipeWriter _writer;
	SocketAsyncEventArgs _connectEventArg;
	SocketAsyncEventArgs _recvEventArg;
	SocketAsyncEventArgs _sendEventArg;
	Socket _socket;

	public bool IsConnected { get; private set; } = false;
	public bool IsConnecting { get; private set; }
	public bool IsSocketDisposed { get; private set; }
	public bool IsDisposed { get; private set; }

	protected virtual Socket CreateSocket() { return new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); }

	public TcpClient(EndPoint endPoint, string address, int port)
	{
		EndPoint = endPoint;
		Address = address;
		Port = port;
	}

	public virtual bool Connect()
	{
		if (IsConnected || IsConnecting)
		{
			return false;
		}

		_connectEventArg = new();
		_connectEventArg.RemoteEndPoint = EndPoint;
		_connectEventArg.Completed += OnAsyncComplete;
		_recvEventArg = new();
		_recvEventArg.Completed += OnAsyncComplete;
		_sendEventArg = new();
		_sendEventArg.Completed += OnAsyncComplete;
		_socket = CreateSocket();


		IsSocketDisposed = false;

		try
		{
			//서버에 연결.
			_socket.Connect(EndPoint);
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
			throw e;

		}

		IsConnected = true;

		return true;
	}

	public virtual bool Disconnect()
	{
		if (!IsConnected && !IsConnecting)
		{
			return false;
		}

		if (IsConnecting)
		{
			Socket.CancelConnectAsync(_connectEventArg);
		}

		_reader.Complete();
		_writer.Complete();

		_connectEventArg.Completed -= OnAsyncComplete;
		_recvEventArg.Completed -= OnAsyncComplete;
		_sendEventArg.Completed -= OnAsyncComplete;

		try
		{
			try
			{
				_socket.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException) { }

			_socket.Close();

			_socket.Dispose();

			_connectEventArg.Dispose();
			_recvEventArg.Dispose();
			_sendEventArg.Dispose();

			IsSocketDisposed = true;
		}
		catch(ObjectDisposedException)
		{

		}

		IsConnected = false;

		return true;
	}

	public virtual bool Reconnect()
	{
		if (!Disconnect())
			return false;

		return Connect();
	}

	public virtual bool ConnectAsync()
	{
		if (IsConnected || IsConnecting)
		{
			return false;
		}

		_connectEventArg = new();
		_connectEventArg.RemoteEndPoint = EndPoint;
		_connectEventArg.Completed += OnAsyncComplete;
		_recvEventArg = new();
		_recvEventArg.Completed += OnAsyncComplete;
		_sendEventArg = new();
		_sendEventArg.Completed += OnAsyncComplete;

		_socket = CreateSocket();

		IsSocketDisposed = false;

		IsConnecting = true;

		if (!_socket.ConnectAsync(_connectEventArg))
			ProcessConnect(_connectEventArg);

		return true;
	}

	public virtual async ValueTask<bool> DisconnectAsync() => Disconnect();

	public virtual async ValueTask<bool> ReconnectAsync()
	{
		await DisconnectAsync();
		return ConnectAsync();
	}

	public virtual long Send(ReadOnlySpan<byte> buffer)
	{
		if (!IsConnected)
			return 0;

		if (buffer.IsEmpty)
			return 0;

		long sent = _socket.Send(buffer, SocketFlags.None, out SocketError ec);
		if (sent > 0)
		{
		}

		if (ec != SocketError.Success)
		{
			//error
			Disconnect();
		}

		return sent;
	}

	public virtual bool SendAsync(ReadOnlyMemory<byte> buffer)
	{
		if (!IsConnected)
			return false;

		if (buffer.IsEmpty)
			return false;

		_writer.WriteAsync(buffer);

		return true;
	}

	public virtual long Receive(byte[] buffer, long offset, long size)
	{
		if (!IsConnected)
			return 0;

		if (size == 0)
			return 0;

		long recevied = _socket.Receive(buffer, (int)offset, (int)size, SocketFlags.None, out SocketError ec);
		if (recevied > 0)
		{

		}

		if (ec != SocketError.Success)
		{
			//error
			Disconnect();
		}

		return recevied;
	}

	public virtual async void ReceiveAsync()
	{
		if (!IsConnected)
			return;

		try
		{
			ReadResult result = await _reader.ReadAsync();
			var buffer = result.Buffer;

			if (buffer.IsEmpty)
			{
				Disconnect();
				return;
			}

			var remainingData = buffer.Length;
			if (remainingData == 0) 
			{
				Disconnect();
				return;
			}

			_reader.AdvanceTo(buffer.Start, buffer.End);
		}
		catch (SocketException) { }
		{
		}
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
			Debug.WriteLine("Server 연결 성공");

			//NetworkStream을 이용한 Tcp소켓 프로그래밍. 
			var stream = new NetworkStream(_socket);
			//Reader Writer 셋
			_reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: ushort.MaxValue));
			_writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

			IsConnected = true;

			//Receive

			if (IsSocketDisposed)
				return;

		}
		else
		{
			//error
		}
	}

	async void TryReceive()
	{
	}

	bool ProcessReceive(SocketAsyncEventArgs e)
	{
		if (!IsConnected)
			return false;

		long size = e.BytesTransferred;

		if (size > 0)
		{

		}

		if (e.SocketError == SocketError.Success)
		{
			if (size > 0)
				return true;
			else
				DisconnectAsync();
		}
		else
		{
			//Error
			DisconnectAsync();
		}

		return false;
	}

	async void TrySend()
	{

	}

	bool ProcessSend(SocketAsyncEventArgs e)
	{
		if (!IsConnected)
			return false;

		long size = e.BytesTransferred;

		if (size > 0)
		{

		}

		if (e.SocketError == SocketError.Success)
			return true;
		else
		{
			DisconnectAsync();
			return false;
		}
	}


	#region Dispose
	protected virtual void Dispose(bool disposing)
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

	public void Dispose()
	{
		// 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}