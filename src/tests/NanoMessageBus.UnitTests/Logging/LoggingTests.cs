#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Logging
{
	using System;
	using Machine.Specifications;

	[Subject(typeof(LogFactory))]
	public class when_logging_null_messages_using_the_null_logger : with_a_logger
	{
		Because of = () => Try(() =>
		{
			var logger = LogFactory.Build(typeof(int));
			logger.Verbose(null);
			logger.Debug(null);
			logger.Info(null);
			logger.Warn(null);
			logger.Error(null);
			logger.Fatal(null);
		});

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(LogFactory))]
	public class when_logging_null_messages_using_the_console_logger : with_a_logger
	{
		Because of = () => Try(() =>
		{
			LogFactory.LogWith(type => new ConsoleLogger(type, Threshold.Verbose));
			var logger = LogFactory.Build(typeof(int));
			logger.Verbose(null);
			logger.Debug(null);
			logger.Info(null);
			logger.Warn(null);
			logger.Error(null);
			logger.Fatal(null);
		});

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(LogFactory))]
	public class when_logging_null_messages_using_the_trace_logger : with_a_logger
	{
		Because of = () => Try(() =>
		{
			LogFactory.LogWith(type => new TraceLogger(type, Threshold.Verbose));
			var logger = LogFactory.Build(typeof(int));
			logger.Verbose(null);
			logger.Debug(null);
			logger.Info(null);
			logger.Warn(null);
			logger.Error(null);
			logger.Fatal(null);
		});

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(LogFactory))]
	public class when_the_logging_threshold_is_not_high_enough_using_the_console_logger : with_a_logger
	{
		Establish context = () =>
			LogFactory.LogWith(type => new ConsoleLogger(type, Threshold.Fatal));

		Because of = () =>
		{
			var logger = LogFactory.Build(typeof(int));
			logger.Verbose(null);
			logger.Debug(null);
			logger.Info(null);
			logger.Warn(null);
			logger.Error(null);
		};

		It should_not_log_anything = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(LogFactory))]
	public class when_the_logging_threshold_is_not_high_enough_using_the_trace_logger : with_a_logger
	{
		Establish context = () =>
			LogFactory.LogWith(type => new TraceLogger(type, Threshold.Fatal));

		Because of = () =>
		{
			var logger = LogFactory.Build(typeof(int));
			logger.Verbose(null);
			logger.Debug(null);
			logger.Info(null);
			logger.Warn(null);
			logger.Error(null);
		};

		It should_not_log_anything = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(LogFactory))]
	public class when_a_null_logger_callback_is_configured : with_a_logger
	{
		Establish context = () =>
			LogFactory.LogWith((Func<Type, ILog>)null);

		Because of = () =>
			LogFactory.Build(typeof(int)).Verbose(string.Empty);

		It should_not_throw_an_exception_when_invoked = () =>
			thrown.ShouldBeNull();
	}

	public abstract class with_a_logger
	{
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169