using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public enum PacketId : ushort
{
	CHAT = 21000,
}

namespace NetCore
{
	public class PacketHandler
	{
		object _lock = new();

		List<ReadOnlySequence<byte>> backPacketBuffers { get; set; } = new();
		List<ReadOnlySequence<byte>> frontPacketBuffers { get; set; } = new();

		public static Dictionary<ushort, Action<ReadOnlySequence<byte>>> Handler { get; set; } = new();

		public void Push(ReadOnlySequence<byte> packet)
		{
            lock (_lock)
            {
				backPacketBuffers.Add(packet);
            }
		}

		public List<ReadOnlySequence<byte>> Pop()
		{
			lock (_lock)
			{
				frontPacketBuffers.Clear();
				frontPacketBuffers.AddRange(backPacketBuffers);
				backPacketBuffers.Clear();
			}
			return frontPacketBuffers;
		}

		public void Deserialize(ReadOnlySequence<byte> packet)
		{
			var id = BitConverter.ToUInt16(packet.FirstSpan.Slice(0, 2/*id*/));
			var size = BitConverter.ToUInt16(packet.FirstSpan.Slice(2, 2/*size*/));
			var body = packet.Slice(4, size);

			//원래는 여기서 실행 안함
			try
			{
				Handler[id].Invoke(body);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

		}

		public void Serialize(ushort id, byte[] bytes, TcpSession e)
		{
			Span<byte> buf = stackalloc byte[4 + bytes.Length];

			buf[0] = (byte)(((ushort)id) & 0x00FF);
			buf[1] = (byte)((((ushort)id) & 0xFF00) >> 8);
			buf[2] = (byte)(((ushort)bytes.Length) & 0x00FF);
			buf[3] = (byte)((((ushort)bytes.Length) & 0xFF00) >> 8);
			bytes.CopyTo(buf.Slice(4));

			e.Send(buf);
		}
	}
}