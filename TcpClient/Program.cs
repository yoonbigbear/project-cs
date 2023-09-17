using Google.Protobuf;
using System.Diagnostics;
using System.Net;
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
			Header = 1, // 16/16 씩 나눠서 키, 사이즈로?
			Message = "chat from",
		}; //매번 힙할당 마음에 안든다. 수정할 필요 있음.

		chat.CalculateSize();
		Console.WriteLine("Start Client...");
		while (true)
		{
			Thread.Sleep(2000);

			foreach (var e in sessions)
			{
				e.SendAsync(chat.ToByteArray());
			}
		}

		foreach (var e in sessions)
			e.Dispose();
	}
}
