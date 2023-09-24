using Google.Protobuf;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using Net;

public partial class Chat
{
}

public class Program
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;
		HashSet<ServerSession> sessions = new(); ;

		//패킷 핸들러 등록
		PacketHandler.Handler.Add((ushort)PacketId.CHAT, ChatCallback);

		for (int i = 0; i < 5000; ++i)
		{
			var ep = new IPEndPoint(IPAddress.Parse(address), port);
			ServerSession server = new ServerSession(ep, address, port);
			if (server.ConnectAsync())
				sessions.Add(server);

		}

		Chat chat = new Chat
		{
			Message = "chat from",
		};
		var bytes = chat.ToByteArray();


		Console.WriteLine("Start Client...");
		while (true)
		{
			Thread.Sleep(1000);

			foreach (var e in sessions)
			{
				{
					e.PacketHandler.Serialize((ushort)PacketId.CHAT, bytes, e);
				}
				{
					var pkt = e.PacketHandler.Pop();
					if (pkt.HasValue)
					{
						e.PacketHandler.Deserialize(pkt.Value);
					}
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

