using Net;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

public class Server : TcpServer
{
	public Server(EndPoint endPoint) : base(endPoint) { }

	static int sessionid;

	protected Dictionary<int, TcpSession> _tcpSessions { get; set; } = new();
	public PacketHandler PacketHandler { get; set; } = new PacketHandler();

	Session CreateSession() => new(this);

	public int RegisterSession(TcpSession session)
	{
		_tcpSessions.Add(++sessionid, session);
		return sessionid;
	}
	public void UnregisterSession(int sessionId) => _tcpSessions.Remove(sessionid);
	public TcpSession FindSession(int index) => _tcpSessions[index];
	public void Broadcast(ReadOnlySpan<byte> bytes)
	{
		foreach (var item in _tcpSessions.Values)
		{
			item.Send(bytes);
		}
	}

	//listening 전 서버 시작시 필요한 기본 세팅
	protected override void OnStart() 
	{
		PacketHandler.Handler.Add(PacketId.CHAT, ChatCallback);
	}
	// listen 호출 후
	protected override void OnStarted() { }
	// accept 막기 전
	protected override void OnStop() 
	{
		Console.WriteLine("On Stop");

		foreach (var item in _tcpSessions.Values)
		{
			item.Dispose();
		}
	}
	// 모든 종료가 완료됨
	protected override void OnStopped() { }
	// 세션 연결
	protected override void OnConnect(SocketAsyncEventArgs arg)
	{
		Console.WriteLine("new session connected");

		var session = CreateSession();

		//들어온 소켓을 세션의 소켓으로 넘긴다.
		session.Connect(arg.AcceptSocket);

		RegisterSession(session);
	}

	// 세션 연결 완료.
	protected override void OnConnected() { }
	protected override void OnDisconnect() { }
	protected override void OnDisconnected() { }


	public static void ChatCallback(ReadOnlySequence<byte> sequence)
	{
		var msg = Chat.Parser.ParseFrom(sequence);
		//Console.WriteLine($"{msg}");
	}
}