using System.Diagnostics;

namespace NetCore
{
	public class GCTracer
	{
		static long before = 0;
		static long after = 0;
		public static void Collect()
		{
			GC.Collect();
			Thread.Sleep(1000);
			before = GC.GetTotalMemory(false);
		}
		public static void Dispose()
		{
			after = GC.GetTotalMemory(false);
			Console.WriteLine($"GC heap alloc :{after - before}");
			GC.Collect();
			Thread.Sleep(1000);
		}
	}

	public class ElapsedTimer
	{
		static Stopwatch _start = new();

		public static void Start()
		{
			_start.Reset();
			_start.Start();
		}
		public static void End()
		{
			_start.Stop();
			Console.WriteLine($"Elapsed ms... {_start.ElapsedMilliseconds}");
		}
	}
}
