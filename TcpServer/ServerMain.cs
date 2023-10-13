using NetCore;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

internal class ServerMain
{
	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;

		DBTest.CreateAccountTableAndProcedure();
		DBTest.CreateCharacterTableAndProcedure();
		DBTest.CreateBagTableAndProcedure();
		DBTest.CreateGuildTableAndProcedure();
		DBTest.CreateMailDatabaseProcedure();

		Server server = new(new IPEndPoint(IPAddress.Parse(address), port));
		server.Start();
		Console.WriteLine("Start Server...");

		while (true)
		{
			var buffers = server.PacketHandler.Pop();
			foreach (var e in buffers)
			{
				server.PacketHandler.Deserialize(e.Item1, e.Item2);
			}
			buffers.Clear();
		}

		server.Stop();
		server.Dispose();
	}

}