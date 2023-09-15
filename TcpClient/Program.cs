using MessagePack;
using System.Net;
using System.Security.Principal;

public class Program
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;
		HashSet<TcpNet> sessions = new(); ;

		for (int i = 0; i < 1110; ++i)
		{
			var ep = new IPEndPoint(IPAddress.Parse(address), port);
			TcpNet server = new TcpNet(ep, address, port);
			if (server.Connect())
				sessions.Add(server);
		}

		Console.WriteLine("Start Client...");
		while (true)
		{

				Thread.Sleep(2000);

				var chat = new ChatMP
				{
					Name = "client",
					Message = "Hello server",
				};
				var bytes = MessagePackSerializer.Serialize(chat);

				foreach (var e in sessions)
					e.Send(bytes);
		}

		foreach (var e in sessions)
			e.Dispose();
	}
}
