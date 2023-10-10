using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public enum PacketId : ushort
{
	CHAT = 21000,
	CREATEACCOUNTREQ = 3000,
	CREATEACCOUNTACK= 3002,
	CREATECHARACTERREQ = 4000,
	CREATECHARACTERACK = 4002,
	INSERTITEMBULKREQ = 5000,
}

namespace NetCore
{
	public class PacketHandler
	{
		object _lock = new();

		List<(TcpSession, ArraySegment<byte>)> _backBuffer { get; set; } = new();
		List<(TcpSession, ArraySegment<byte>)> _frontBuffer { get; set; } = new();

		public static Dictionary<ushort, Action<TcpSession, ArraySegment<byte>>> Handler { get; set; } = new();

		public void Push(TcpSession session, ArraySegment<byte> packet)
		{
            lock (_lock)
            {
				_backBuffer.Add((session, packet));
            }
		}

		public List<(TcpSession, ArraySegment<byte>)> Pop()
		{
			lock (_lock)
			{
				_frontBuffer.Clear();
				_frontBuffer = _backBuffer.ToList();
				_backBuffer.Clear();
				return _frontBuffer;
			}
		}

		public void Deserialize(TcpSession session, ArraySegment<byte> packet)
		{
			var id = BitConverter.ToUInt16(packet.Slice(0, 2/*id*/));
			var size = BitConverter.ToUInt16(packet.Slice(2, 2/*size*/));
			var body = packet.Slice(4, size);

			//원래는 여기서 실행 안함
			try
			{
				Handler[id].Invoke(session, body);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}
	}
}