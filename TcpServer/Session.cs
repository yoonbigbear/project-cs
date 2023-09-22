using Net;
using System.Buffers;
using System.Net.Sockets;

public class Session : TcpSession
{
	public Session(Server server) : base(server) { }

	protected override void OnError(SocketError er) { }
	// 소켓 생override킷 recv/send 시작하기 전
	protected override void OnConnect() { }
	// recv/seoverride한 후.
	protected override void OnConnected() { }
	// 소켓 콜override후 스트림 종료 전
	protected override void OnDisconnect() { }
	// 스트림 override
	protected override void OnDisconnected() { }
	protected override void OnPacketRead(ReadOnlySequence<byte> reads)
	{
		//Console.WriteLine($"received client packet t:{Thread.CurrentThread.ManagedThreadId}");
		Server.PacketHandler.Push(reads);
	}
}
