namespace NanoMessageBus
{
	using System;
	using System.Threading;

	/// <summary>
	/// Provides the ability to override the current moment in time to facilitate testing.
	/// Original idea by Ayende Rahien:
	/// http://ayende.com/Blog/archive/2008/07/07/Dealing-with-time-in-tests.aspx
	/// </summary>
	public static class SystemTime
	{
		/// <summary>
		/// Represents "zero" for Unix Epoch Time.
		/// </summary>
		public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// The callback to be used to resolve the current moment in time.
		/// </summary>
		public static Func<DateTime> TimeResolver;

		/// <summary>
		/// Gets the current moment in time.
		/// </summary>
		/// <remarks>
		/// Method invocation is not thread safe if the resolver is being changed by multiple threads. The intended design
		/// is for testing where it can be set, any tests run, and then the value cleared.
		/// </remarks>
		public static DateTime UtcNow => TimeResolver == null ? DateTime.UtcNow : TimeResolver();

	    /// <summary>
		/// Gets the number of milliseconds that have elapsed between the instant and Unix Epoch Time (12:00 AM January 1, 1970).
		/// </summary>
		/// <param name="instant">The instant from which epoch time will be computed.</param>
		/// <returns>The number of milliseconds that have elapsed since the instant provided.</returns>
		public static long ToEpochTime(this DateTime instant)
		{
			return (long)(instant - EpochTime).TotalMilliseconds;
		}

		/// <summary>
		/// Gets the point in time represented by the instant specified in milliseconds since the epoch.
		/// </summary>
		/// <param name="epochTime">The point in time, according to Unix Epoch Time to be converted, expressed in milliseconds.</param>
		/// <returns>The point in time expressed as a DateTime.</returns>
		public static DateTime ToDateTime(this long epochTime)
		{
			return EpochTime + TimeSpan.FromMilliseconds(epochTime);
		}

		/// <summary>
		/// The callback to be used to instruct the current thread to sleep.
		/// </summary>
		public static Action<TimeSpan> SleepResolver;

		/// <summary>
		/// Instructs the current thread to sleep for the specified amount of time.
		/// </summary>
		/// <param name="value">The amount of time for the current thread to sleep.</param>
		public static void Sleep(this TimeSpan value)
		{
			(SleepResolver ?? Thread.Sleep)(value);
		}
	}
}