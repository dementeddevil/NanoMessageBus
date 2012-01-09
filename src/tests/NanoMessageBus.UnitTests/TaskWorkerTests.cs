#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Threading;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(TaskWorker<IMessagingChannel>))]
	public class when_constructing_a_worker_with_null_state : with_a_worker
	{
		Because of = () =>
			Try(() => new TaskWorker<IMessagingChannel>(null, tokenSource.Token, 1, 1));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorker<IMessagingChannel>))]
	public class when_constructing_a_worker_with_state : with_a_worker
	{
		Establish context = () =>
		{
			minWorkers = 42;
			Build();
		};

		It should_make_the_state_visible = () =>
			worker.State.ShouldEqual(mockChannel.Object);

		It should_have_the_correct_number_of_active_workers = () =>
			worker.ActiveWorkers.ShouldEqual(minWorkers);
	}

	[Subject(typeof(TaskWorker<IMessagingChannel>))]
	public class when_performing_a_null_operation : with_a_worker
	{
		Because of = () =>
			Try(() => worker.PerformOperation(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorker<IMessagingChannel>))]
	public class when_performing_an_operation : with_a_worker
	{
		Because of = () =>
			worker.PerformOperation(IncrementCallCount);

		It should_invoke_the_operation_provided = () =>
			callCount.ShouldEqual(1);
	}

	[Subject(typeof(TaskWorker<IMessagingChannel>))]
	public class when_the_operation_performed_throws_an_exception : with_a_worker
	{
		Because of = () =>
			Try(() => worker.PerformOperation(() => { throw exception; }));

		It should_bubble_up_the_exception = () =>
			thrown.ShouldEqual(exception);

		static readonly Exception exception = new Exception("custom");
	}

	[Subject(typeof(TaskWorker<IMessagingChannel>))]
	public class when_token_has_been_cancelled : with_a_worker
	{
		Establish context = () =>
			tokenSource.Cancel();

		Because of = () =>
			worker.PerformOperation(IncrementCallCount);

		It should_not_invoke_the_operation = () =>
			callCount.ShouldEqual(0);

		It should_dispose_of_the_state = () =>
			mockChannel.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class with_a_worker
	{
		Establish context = () =>
		{
			mockChannel = new Mock<IMessagingChannel>();

			tokenSource = new CancellationTokenSource();
			minWorkers = maxWorkers = 1;
			callCount = 0;

			Build();
		};
		protected static void Build()
		{
			worker = new TaskWorker<IMessagingChannel>(mockChannel.Object, tokenSource.Token, minWorkers, maxWorkers);
		}
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}
		protected static void IncrementCallCount()
		{
			callCount++;
		}

		protected static CancellationTokenSource tokenSource;
		protected static int minWorkers;
		protected static int maxWorkers;
		protected static TaskWorker<IMessagingChannel> worker;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Exception thrown;
		protected static int callCount;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169