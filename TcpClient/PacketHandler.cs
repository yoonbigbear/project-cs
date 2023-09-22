using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

public enum PacketId : ushort
{
	CHAT = 21000,
}

public class PacketHandler
{
	public ConcurrentQueue<ReadOnlySequence<byte>> packetBuffers { get; set; } = new ConcurrentQueue<ReadOnlySequence<byte>>();

	public static Dictionary<PacketId, Action<ReadOnlySequence<byte>>> Handler { get; set; } = new();

	public void Push(ReadOnlySequence<byte> packet)
	{
		packetBuffers.Enqueue(packet);
	}

	public Nullable<ReadOnlySequence<byte>> Pop()
	{
		if (packetBuffers.TryDequeue(out var packet))
			return packet;
		return null;
	}

	public void Deserialize(ReadOnlySequence<byte> packet)
	{
		var id = BitConverter.ToUInt16(packet.FirstSpan.Slice(0, 2/*id*/));
		var size = BitConverter.ToUInt16(packet.FirstSpan.Slice(2, 2/*size*/));
		var body = packet.Slice(4, size);

		//원래는 여기서 실행 안함
		try
		{
			Handler[(PacketId)id].Invoke(body);
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

	}

	public void Serialize(PacketId id, byte[] bytes, ServerSession e)
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