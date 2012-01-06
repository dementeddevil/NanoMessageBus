#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Threading;
	using Machine.Specifications;

	[Subject(typeof(SystemTime))]
	public class when_resolving_the_system_time
	{
		It should_be_the_current_time = () =>
			SystemTime.UtcNow.ShouldBeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(10));
	}

	[Subject(typeof(SystemTime))]
	public class when_setting_the_system_time
	{
		Because of = () =>
			SystemTime.NowResolver = () => Instant;

		It should_be_the_time_specified = () =>
			SystemTime.UtcNow.ShouldEqual(Instant);

		It should_remain_the_time_specified = () =>
		{
			Thread.Sleep(1);
			SystemTime.UtcNow.ShouldEqual(Instant);
		};

		Cleanup after = () =>
			SystemTime.NowResolver = null;

		static readonly DateTime Instant = DateTime.Parse("2010-01-02 03:04:05.678");
	}

	[Subject(typeof(SystemTime))]
	public class when_getting_epoch_time
	{
		Establish context = () =>
			SystemTime.NowResolver = () => Instant;

		Because of = () =>
		{
			seconds = SystemTime.UtcNow.ToEpochTime();
			converted = seconds.ToDateTime();
		};

		It should_yield_the_number_of_seconds_since_Unix_epoch_time_1970 = () =>
			seconds.ShouldEqual(946684800);

		It should_be_able_to_convert_epoch_time_back_to_a_regular_DateTime = () =>
			converted.ShouldEqual(Instant);

		Cleanup after = () =>
			SystemTime.NowResolver = null;

		static long seconds;
		static DateTime converted;
		static readonly DateTime Instant = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
	}

	[Subject(typeof(SystemTime))]
	public class when_instructing_the_current_thread_to_sleep
	{
		Because of = () =>
			Sleep.Sleep();

		It should_let_the_current_thread_sleep = () =>
			SystemTime.UtcNow.ShouldBeCloseTo(Now + Sleep, TimeSpan.FromMilliseconds(200));

		static readonly TimeSpan Sleep = TimeSpan.FromMilliseconds(250);
		static readonly DateTime Now = SystemTime.UtcNow;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169