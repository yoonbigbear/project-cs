using MessagePack;
using System;

public interface IPacketHeader
{
	public short id { get; set; }
	public short size { get; set; }
}

[MessagePackObject]
public class ChatMP : IPacketHeader
{
	[Key(0)]
	public short id { get; set; }
	[Key(1)]
	public short size { get; set; }

	[Key(2)]
	public string Name { get; set; }
	[Key(3)]
	public string Message { get; set; }
}
