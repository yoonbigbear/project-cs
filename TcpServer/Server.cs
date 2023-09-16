using Net;
using System.Net;

public class Server : TcpServer
{
	public Server(EndPoint endPoint) : base(endPoint) { }

	//listening 전 서버 시작시 필요한 기본 세팅
	protected override void OnStart() { }
	// listen 호출 후
	protected override void OnStarted() { }
	// accept 막기 전
	protected override void OnStop() { }
	// 모든 종료가 완료됨
	protected override void OnStopped() { }
	// 세션 연결
	protected override void OnConnect() => Console.WriteLine("new session connected");
	// 세션 연결 완료.
	protected override void OnConnected() { }
	// 
	protected override void OnDisconnect() { }
	protected override void OnDisconnected() { }
}