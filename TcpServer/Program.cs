
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Net;

internal class Program
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;

		Server server = new(new IPEndPoint(IPAddress.Parse(address), port));
		server.Start();
		Console.WriteLine("Start Server...");

		while (true)
		{
			var pkt = server.PacketHandler.Pop();
			if (pkt.HasValue)
			{
				server.PacketHandler.Deserialize(pkt.Value);
				server.Broadcast(pkt.Value.FirstSpan);
			}

			Thread.Yield();
		}

		server.Stop();
		server.Dispose();
	}
}
