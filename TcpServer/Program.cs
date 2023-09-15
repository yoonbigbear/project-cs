
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
}
