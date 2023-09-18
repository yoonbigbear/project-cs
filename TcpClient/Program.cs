using Google.Protobuf;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Principal;

public class Program
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;
		HashSet<ServerSession> sessions = new(); ;

		for (int i = 0; i < 5; ++i)
		{
			var ep = new IPEndPoint(IPAddress.Parse(address), port);
			ServerSession server = new ServerSession(ep, address, port);
			if (server.Connect())
				sessions.Add(server);
		}

		Chat chat = new Chat
		{
			Message = "chat from",
		};

		

		Console.WriteLine("Start Client...");
		while (true)
		{
			Thread.Sleep(2000);

			foreach (var e in sessions)
			{
				e.SendAsync(e.PacketHandler.Serialize(PacketId.CHAT, chat.ToByteArray()));

				var pkt = e.PacketHandler.Pop();
				if (pkt.HasValue)
				{
					e.PacketHandler.Deserialize(pkt.Value);
				}
			}
		}

		foreach (var e in sessions)
			e.Dispose();
	}
}
