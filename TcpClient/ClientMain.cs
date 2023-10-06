using Google.Protobuf;
using System.Buffers;
using System.Net;
using NetCore;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public partial class Chat
{
}

public class ClientMain
{
	public static int globalcount = 0;

	static void Main(string[] args)
	{
		string address = "127.0.0.1";
		int port = 8081;
		HashSet<ServerConnection> sessions = new(); ;

		//패킷 핸들러 등록
		PacketHandler.Handler.Add((ushort)PacketId.CHAT, ChatCallback);
		PacketHandler.Handler.Add((ushort)PacketId.CREATEACCOUNTACK, CreateAccount_ACK);
		PacketHandler.Handler.Add((ushort)PacketId.CREATECHARACTERACK, CreateCharacter_ACK);


		Console.WriteLine("Start Client...");

		for (int i = 0; i < 10000; ++i)
		{
			Thread.Sleep(1);
			var ep = new IPEndPoint(IPAddress.Parse(address), port);
			ServerConnection server = new ServerConnection(ep);
			server.ConnectAsync();
			sessions.Add(server);
		}

		while (true)
		{
			foreach (var e in sessions)
			{
				var pktlist = e.PacketHandler.Pop();
				foreach(var pkt in pktlist)
				{
					e.PacketHandler.Deserialize(pkt.Item1, pkt.Item2);
				}
			}
		}
		foreach (var e in sessions)
			e.Dispose();
	}

	public static void ChatCallback(TcpSession session, ArraySegment<byte> sequence)
	{
		var msg = Chat.Parser.ParseFrom(sequence);
		//Console.WriteLine($"{msg}");
	}

	public static void CreateAccount_ACK(TcpSession session, ArraySegment<byte> sequence)
	{
		var msg = CreateAccountACK.Parser.ParseFrom(sequence);
		Debug.Assert(msg.Result == 0);

		ServerConnection ss = session as ServerConnection;

		CreateCharacterREQ req = new CreateCharacterREQ
		{
			Name = $"ch_{session.GetHashCode()}",
		};
		ss.Serialize((ushort)PacketId.CREATECHARACTERREQ, req.ToByteArray());
	}

	public static void CreateCharacter_ACK(TcpSession session, ArraySegment<byte> sequence)
	{
		var msg = CreateCharacterACK.Parser.ParseFrom(sequence);
		Debug.Assert(msg.Result == 0);

		Console.WriteLine($"ID={msg.CharacterUid}");
	}
}

