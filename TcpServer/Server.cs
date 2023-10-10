using Google.Protobuf;
using NetCore;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

public class SparseList<T>
	where T : class
{
	public List<T> items { get; private set; } = new();
	Queue<int> emptylist = new();

	public int Add(T item)
	{
		if (emptylist.Count > 0)
		{
			int idx = emptylist.Dequeue();
			items[idx] = item;
			return idx;
		}
		else
		{
			items.Add(item);
			return items.Count - 1;
		}
	}

	public void Remove(int idx)
	{
		emptylist.Enqueue(idx);
		items[idx] = null;
	}
}

public class SessionList
{
	public ConcurrentBag<TcpSession> sessions { get; private set; } = new();

	public int Add(TcpSession item)
	{
		sessions.Add(item);
		return sessions.Count - 1;
	}

	public void Remove(int idx)
	{
		var last = sessions.Last();
	}
}

public class Server : TcpServer
{
	public Server(EndPoint endPoint) : base(endPoint) { }

	static int sessionid;

	object _lock = new();

	//protected ConcurrentDictionary<int, TcpSession> _tcpSessions { get; set; } = new();
	protected SparseList<TcpSession> _tcpSessions { get; set; } = new();

	public PacketHandler PacketHandler { get; set; } = new PacketHandler();

	Session CreateSession() => new(this);

	public int RegisterSession(TcpSession session)
	{
		//_tcpSessions.TryAdd(++sessionid, session);
		lock (_lock)
		{
			return _tcpSessions.Add(session); ;
		}
		//return sessionid;
	}
	public void UnregisterSession(int sessionId) => _tcpSessions.Remove(sessionid);
	public TcpSession FindSession(int index) => _tcpSessions.items[index];
	public void Broadcast(ReadOnlySpan<byte> bytes)
	{
		foreach (var item in _tcpSessions.items)
		{
			if (item != null)
			{
				item.Send(bytes);
			}
		}
	}

	//listening 전 서버 시작시 필요한 기본 세팅
	protected override void OnStart()
	{
		PacketHandler.Handler.Add((ushort)PacketId.CHAT, ChatCallback);
		PacketHandler.Handler.Add((ushort)PacketId.CREATEACCOUNTREQ, CreateAccount_REQ);
		PacketHandler.Handler.Add((ushort)PacketId.CREATECHARACTERREQ, CreateCharacter_REQ);
		PacketHandler.Handler.Add((ushort)PacketId.INSERTITEMBULKREQ, InsertItemBuild_REQ);
	}
	// listen 호출 후
	protected override void OnStarted() { }
	// accept 막기 전
	protected override void OnStop()
	{
		Console.WriteLine("On Stop");

		foreach (var item in _tcpSessions.items)
		{
			item.Dispose();
		}
	}
	// 모든 종료가 완료됨
	protected override void OnStopped() { }
	// 세션 연결
	protected override void OnConnect(SocketAsyncEventArgs arg)
	{
		var session = CreateSession();

		//들어온 소켓을 세션의 소켓으로 넘긴다.
		session.Connect(arg.AcceptSocket);

		RegisterSession(session);
		Console.WriteLine($"session {_tcpSessions.items.Count}");
	}

	// 세션 연결 완료.
	protected override void OnConnected() { }
	protected override void OnDisconnect() { }
	protected override void OnDisconnected() { }

	public static void ChatCallback(TcpSession session, ArraySegment<byte> sequence)
	{
		Session ss = session as Session;
		Debug.Assert(ss != null);

		var msg = Chat.Parser.ParseFrom(sequence);
		//Console.WriteLine($"{msg}");
	}

	public static async void CreateAccount_REQ(TcpSession session, ArraySegment<byte> sequence)
	{
		Session ss = session as Session;
		Debug.Assert(ss != null);
		var msg = CreateAccountREQ.Parser.ParseFrom(sequence);
		var accountId = await DBTest.CreateAccount(msg.Name);
		Debug.Assert(accountId != -1);
		{
			CreateAccountACK ack = new();
			ack.Result = 0;
			ack.AcctId = accountId;
			ss.Serialize((ushort)PacketId.CREATEACCOUNTACK, ack.ToByteArray());
		}

	}

	public static async void CreateCharacter_REQ(TcpSession session, ArraySegment<byte> sequence)
	{
		Session ss = session as Session;
		Debug.Assert(ss != null);
		var msg = CreateCharacterREQ.Parser.ParseFrom(sequence);
		var character_uid = await DBTest.CreateCharacter(msg.Name);
		Debug.Assert(character_uid != -1);
		{
			CreateCharacterACK ack = new();
			ack.Result = 0;
			ack.CharacterUid = character_uid;
			ss.Serialize((ushort)PacketId.CREATECHARACTERACK, ack.ToByteArray());
		}
	}

	public static async void InsertItemBuild_REQ(TcpSession session, ArraySegment<byte> sequence)
	{
		Session ss = session as Session;
		Debug.Assert(ss != null);
		var msg = InsertItemBulkREQ.Parser.ParseFrom(sequence);

		List<ItemDB> items = new();
		foreach(var e in msg.Items)
		{
			var charid = e.CharId;
			var tid = e.Tid;
			var count = e.Count;

			items.Add(new ItemDB
			{
				dbid = IdGen.GenerateGUID(IdGen.IdType.Item, 1),
				charid = charid,
				tid = tid,
				count = count
			});
		}
		var result = DBTest.AddBulkItem(items);
	}
}