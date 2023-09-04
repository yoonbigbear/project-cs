
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Net;

internal class Program
{
	static void Main(string[] args)
	{
		TcpServer server = new();
		server.Tcp(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));

		server.Start();

		server.Stop();
	}
}
