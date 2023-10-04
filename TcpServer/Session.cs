using NetCore;
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
	protected override void OnDisconnected() { Console.WriteLine($"Disconnected... "); }
	protected override void OnPacketRead(ArraySegment<byte> reads)
	{
		((Server)Server).PacketHandler.Push(this, reads);
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

}
