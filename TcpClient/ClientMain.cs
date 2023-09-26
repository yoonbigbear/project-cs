using Google.Protobuf;
using System.Buffers;
using System.Net;
using NetCore;
using System.Runtime.CompilerServices;

public partial class Chat
{
}

public class ClientMain
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;
		HashSet<ServerSession> sessions = new(); ;

		//패킷 핸들러 등록
		PacketHandler.Handler.Add((ushort)PacketId.CHAT, ChatCallback);

		for (int i = 0; i < 10; ++i)
		{
			var ep = new IPEndPoint(IPAddress.Parse(address), port);
			ServerSession server = new ServerSession(ep);
			server.ConnectAsync();
			sessions.Add(server);
		}

		Chat chat = new Chat
		{
			Message = "chat from",
		};
		var bytes = chat.ToByteArray();


		Console.WriteLine("Start Client...");
		for (int i = 0; i < 10; ++i) 
		{
			Thread.Sleep(100);

			foreach (var e in sessions)
			{
				{
					e.PacketHandler.Serialize((ushort)PacketId.CHAT, bytes, e);
				}
				{
					e.PacketHandler.Pop();
				}
			}
		}
		foreach (var e in sessions)
			e.Dispose();
	}

	public static void ChatCallback(ReadOnlySequence<byte> sequence)
	{
		var msg = Chat.Parser.ParseFrom(sequence);
		//Console.WriteLine($"{msg}");
	}

}

