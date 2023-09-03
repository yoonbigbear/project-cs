
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

public class Server
{
	Socket _acceptorSocket;
	SocketAsyncEventArgs _acceptorEventArg;

	EndPoint _endPoint;

	public Server() { }

	public void Tcp(EndPoint endPoint)
	{
		_endPoint = endPoint;
	}

	public bool Start()
	{
		_acceptorSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

		_acceptorEventArg = new SocketAsyncEventArgs();
		_acceptorEventArg.Completed += OnAsyncCompleted;

		_acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
		_acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
		if (_acceptorSocket.AddressFamily == AddressFamily.InterNetworkV6)
			_acceptorSocket.DualMode = true;

		// Endpoint에 소켓 연결
		_acceptorSocket.Bind(_endPoint);

		//실제 생성된 endpoint로 업데이트
		_endPoint = _acceptorSocket.LocalEndPoint;

		//최대 팬딩제한 두고 Listen시작.
		_acceptorSocket.Listen(10);

		//Accept.
		StartAccept(_acceptorEventArg);

		return true;
	}

	public bool Stop()
	{
		_acceptorEventArg.Completed -= OnAsyncCompleted;
		try
		{
			_acceptorSocket.Close();
			_acceptorSocket.Dispose();
			_acceptorEventArg.Dispose();
		}
		catch (ObjectDisposedException ex) { throw ex; }

		return true;
	}

	void StartAccept(SocketAsyncEventArgs args)
	{
		//context가 재사용되고 있어서 소켓을 우선 비워야 한다.
		args.AcceptSocket = null;

		if (_acceptorSocket.AcceptAsync(args))
			ProcessAccept(args);
	}

	void ProcessAccept(SocketAsyncEventArgs args)
	{
		if (args.SocketError == SocketError.Success)
		{
			//세션 새로 생성.
			//세션 처리
		}
		else
		{
			//에러 이슈잉
		}

		//다시 Accept 재시작
	}

	// Socket.AcceptAsync() 에서 호출되는 완료 콜백.
	void OnAsyncCompleted(object sender, SocketAsyncEventArgs args)
	{

	}
}



internal class Program
{
	static void Main(string[] args)
	{
		Server server = new();
		server.Tcp(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));

		server.Start();

		server.Stop();
	}
}
