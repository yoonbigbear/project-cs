using System.Buffers;
using System.Collections.Concurrent;

public enum PacketId : ushort
{
	CHAT = 21000,
}

public class PacketHandler
{
	public ConcurrentQueue<ReadOnlySequence<byte>> packetBuffers { get; set; } = new ConcurrentQueue<ReadOnlySequence<byte>>();

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
		var id = BitConverter.ToInt16(packet.FirstSpan.Slice(0, 2/*id*/));
		var size = BitConverter.ToInt16(packet.FirstSpan.Slice(2, 2/*size*/));
		var body = packet.FirstSpan.Slice(4, size);

		switch ((PacketId)id)
		{
			case PacketId.CHAT:
				var msg = Chat.Parser.ParseFrom(body);
				Console.WriteLine($"{msg}");
				break;
		}
	}

	public void Serialize(PacketId id, byte[] bytes, ref Span<byte> buf)
	{
		buf[0] = (byte)(((ushort)id) & 0x00FF);
		buf[1] = (byte)((((ushort)id) & 0xFF00) >> 8);
		buf[2] = (byte)(((ushort)bytes.Length) & 0x00FF);
		buf[3] = (byte)((((ushort)bytes.Length) & 0xFF00) >> 8);
		bytes.CopyTo(buf.Slice(4));
	}

}