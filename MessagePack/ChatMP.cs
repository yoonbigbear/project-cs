using MessagePack;

[MessagePackObject]
public class ChatMP
{
	[Key(0)]
	public string Name { get; set; }
	[Key(1)]
	public string Message { get; set; }
}
