
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Net;

internal class Program
{
	static void Main(string[] args)
	{
		if (args.Length > 0)
		{
			if (args[0].Equals("server"))
			{
				TcpServer server = new();
				server.Tcp(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));

				server.Start();

				while (true)
				{
					Thread.Yield();
				}
				server.Stop();
				server.Dispose();
			}
			else if (args[0].Equals("client"))
			{
				string address = "127.0.0.1";
				int port = 8080;

				var ep = new IPEndPoint(IPAddress.Parse(address), 8080);
				TcpClient client = new TcpClient(ep, address, port);
				client.ConnectAsync();

				while (true)
				{
					Thread.Yield();
				}

				client.Dispose();
			}
		}
	}
}
