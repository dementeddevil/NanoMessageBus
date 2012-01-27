#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Logging
{
	using System;
	using Machine.Specifications;

	[Subject(typeof(LogFactory))]
	public class when_logging_null_values_using_the_null_logger : with_a_logger
	{
		Because of = () => Try(() =>
		{
			var logger = LogFactory.Builder(typeof(int));
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
	public class when_logging_null_values_using_the_console_logger : with_a_logger
	{
		Because of = () => Try(() =>
		{
			LogFactory.Builder = type => new ConsoleLogger(typeof(int));
			var logger = LogFactory.Builder(typeof(int));
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
	public class when_logging_null_values_using_the_trace_logger : with_a_logger
	{
		Because of = () => Try(() =>
		{
			LogFactory.Builder = type => new TraceLogger(typeof(int));
			var logger = LogFactory.Builder(typeof(int));
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