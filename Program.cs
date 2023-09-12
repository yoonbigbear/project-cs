
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using MessagePack;
using Net;

internal class Program
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;

		if (args.Length > 0)
		{
			if (args[0].Equals("server"))
			{

				TcpServer server = new(new IPEndPoint(IPAddress.Parse(address), port));

				server.Start();
				Console.WriteLine("Start Server...");
				while (true)
				{
					Thread.Yield();
				}
				server.Stop();
				server.Dispose();
			}
			else if (args[0].Equals("client"))
			{
				var ep = new IPEndPoint(IPAddress.Parse(address), port);
				TcpClient client = new TcpClient(ep, address, port);
				client.ConnectAsync();

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
					client.Send(bytes);
				}

				client.Dispose();
			}
		}
	}
}
