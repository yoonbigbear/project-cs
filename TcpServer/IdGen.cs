public class IdGen
{
	public enum IdType : ulong
	{
		None = 0,
		Player = 1,
		Item = 2,
		Object = 3,
		Log = 4,
		End = 9
	}

	static int _guidSeqOffset = 0;
	const long _typeOffset = 1000;
	const long _uniqueOffset = 1000;
	const long _timeOffset = 1000000000000;

	public static long GenerateGUID(IdType type, int serverid)
	{
		long guid = 0;
		var uniqueSeq = (long)Interlocked.Increment(ref _guidSeqOffset);
		if (uniqueSeq > 900)
			_guidSeqOffset = 0;

		//type
		guid += (long)type * _typeOffset * _typeOffset * _timeOffset;
		//uniqueSeq
		guid += (long)uniqueSeq * _uniqueOffset * _timeOffset;
		//serverId
		guid += (long)serverid * _timeOffset;
		//mstime
		guid += (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		return guid;
	}

	static uint _eidSeqOffset = 0;
	public static uint GenerateEID()
	{
		return Interlocked.Increment(ref _eidSeqOffset);
	}
}
