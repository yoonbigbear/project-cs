using MessagePack;
using System.Net;
using System.Security.Principal;

public class Program
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;

		
		var ep = new IPEndPoint(IPAddress.Parse(address), port);
		TcpNet server = new TcpNet(ep, address, port);
		server.ConnectAsync();

		Console.WriteLine("Start Client...");
		while (true)
		{
			if (Console.ReadKey(true).Key == ConsoleKey.Escape)
			{
				return;
			}
			else
			{

				Thread.Sleep(2000);

				var chat = new ChatMP
				{
					Name = "client",
					Message = "Hello server",
				};
				var bytes = MessagePackSerializer.Serialize(chat);
				server.Send(bytes);
			}
		}

		server.Dispose();
	}
}
