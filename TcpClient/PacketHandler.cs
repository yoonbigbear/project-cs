using System.Buffers;
using System.Collections.Concurrent;
public enum PacketId
{
	CHAT = 1,
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

	public Memory<byte> Serialize(PacketId id, byte[] bytes)
	{
		switch ((PacketId)id)
		{
			case PacketId.CHAT:
				{
					ArraySegment<byte> arr = new ArraySegment<byte>(new byte[4 + bytes.Length]);
					Buffer.BlockCopy(BitConverter.GetBytes((short)PacketId.CHAT), 0, arr.Array, 0, 2);
					Buffer.BlockCopy(BitConverter.GetBytes((short)bytes.Length), 0, arr.Array, 2, 2);
					Buffer.BlockCopy(bytes, 0, arr.Array, 4, bytes.Length);
					return arr;
				}
		}
		return null;
	}
}