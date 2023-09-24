namespace Profiler
{
	class GCTracer
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
}
