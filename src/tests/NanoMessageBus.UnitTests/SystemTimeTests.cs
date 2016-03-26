using FluentAssertions;

#pragma warning disable 169, 414
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
			SystemTime.UtcNow.Should().BeCloseTo(DateTime.UtcNow + TimeSpan.FromMilliseconds(10));
	}

	[Subject(typeof(SystemTime))]
	public class when_setting_the_system_time
	{
		Because of = () =>
			SystemTime.TimeResolver = () => Instant;

		It should_be_the_time_specified = () =>
			SystemTime.UtcNow.Should().Be(Instant);

		It should_remain_the_time_specified = () =>
		{
			Thread.Sleep(1);
			SystemTime.UtcNow.Should().Be(Instant);
		};

		Cleanup after = () =>
			SystemTime.TimeResolver = null;

		static readonly DateTime Instant = DateTime.Parse("2010-01-02 03:04:05.678");
	}

	[Subject(typeof(SystemTime))]
	public class when_getting_epoch_time
	{
		Establish context = () =>
			SystemTime.TimeResolver = () => Instant;

		Because of = () =>
		{
			milliseconds = SystemTime.UtcNow.ToEpochTime();
			converted = milliseconds.ToDateTime();
		};

		It should_yield_the_number_of_seconds_since_Unix_epoch_time_1970 = () =>
			milliseconds.Should().Be(946684800000);

		It should_be_able_to_convert_epoch_time_back_to_a_regular_DateTime = () =>
			converted.Should().Be(Instant);

		Cleanup after = () =>
			SystemTime.TimeResolver = null;

		static long milliseconds;
		static DateTime converted;
		static readonly DateTime Instant = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
	}

	[Subject(typeof(SystemTime))]
	public class when_instructing_the_current_thread_to_sleep
	{
		Because of = () =>
			Sleep.Sleep();

		It should_let_the_current_thread_sleep = () =>
			SystemTime.UtcNow.Should().BeCloseTo(Now + Sleep + TimeSpan.FromMilliseconds(200));

		static readonly TimeSpan Sleep = TimeSpan.FromMilliseconds(250);
		static readonly DateTime Now = SystemTime.UtcNow;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414