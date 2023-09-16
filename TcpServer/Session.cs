using Net;
using System.Net.Sockets;

public class Session : TcpSession
{
	protected virtual void OnError(SocketError er) { }
	// 소켓 생성 후 패킷 recv/send 시작하기 전
	protected virtual void OnConnect() { }
	// recv/send 시작 한 후.
	protected virtual void OnConnected() { }
	// 소켓 콜백 제거 후 스트림 종료 전
	protected virtual void OnDisconnect() { }
	// 스트림 종료 후
	protected virtual void OnDisconnected() { }
}
