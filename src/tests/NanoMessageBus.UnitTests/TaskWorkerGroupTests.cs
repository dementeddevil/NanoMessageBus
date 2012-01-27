#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_a_minWorkers_value_less_than_1_is_provided_during_construction : with_a_worker_group
	{
		Establish context = () =>
			minWorkers = 0;

		Because of = () =>
			Try(Build);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_the_maxWorkers_value_less_than_the_minWorkers_value_is_provided_during_construction : with_a_worker_group
	{
		Establish context = () =>
		{
			minWorkers = 2;
			maxWorkers = 1;
		};

		Because of = () =>
			Try(Build);

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_a_null_state_callback_is_provided_during_initialization : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Initialize(null, RestartDelegate));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_a_null_restart_callback_is_provided_during_initialization : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Initialize(BuildChannel, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_initializing_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.Initialize(BuildChannel, RestartDelegate));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_initializing_an_already_initialized_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(BuildChannel, RestartDelegate);

		Because of = () =>
			Try(() => workerGroup.Initialize(BuildChannel, RestartDelegate));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_null_activity : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.StartActivity(null));

		It should_throw_an_exception = () =>
		   thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity_without_initializing_first : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.StartActivity(EmptyActivity));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity_against_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.StartActivity(EmptyActivity));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity_against_a_previously_started_worker_group : with_a_worker_group
	{
		Establish context = () =>
		{
			workerGroup.Initialize(BuildChannel, RestartDelegate);
			workerGroup.StartActivity(EmptyActivity);
		};

		Because of = () =>
			Try(() => workerGroup.StartActivity(EmptyActivity));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}
	
	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_acquiring_state_throws_an_exception : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(() => { throw new Exception(); }, RestartDelegate);

		Because of = () =>
			TryAndWait(() => workerGroup.StartActivity(EmptyActivity));

		It should_bubble_the_exception_to_the_main_thread = () =>
			thrown.ShouldNotBeNull();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity : with_a_worker_group
	{
		Establish context = () =>
		{
			minWorkers = maxWorkers = 3;
			Build();

			workerGroup.Initialize(() =>
			{
				Interlocked.Increment(ref invocations);
				return mockChannel.Object;
			}, RestartDelegate);
		};

		Because of = () =>
			workerGroup.StartActivity(EmptyActivity);

		It should_invoke_the_state_callback_provided_for_the_minWorkers_value_provided = () =>
			invocations.ShouldEqual(minWorkers);
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_the_state_callback_returns_null : with_a_worker_group
	{
		Establish context = () => 
			workerGroup.Initialize(() => null, RestartDelegate);

		Because of = () =>
			TryAndWait(() => workerGroup.StartActivity(EmptyActivity));

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_running_an_activity : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(BuildChannel, RestartDelegate);

		Because of = () =>
			TryAndWait(() => workerGroup.StartActivity(x => callback = x.State));

		It should_pass_the_current_state_to_the_callback = () =>
			callback.ShouldEqual(mockChannel.Object);

		static IMessagingChannel callback;
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_queue_against_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.StartQueue());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_queue_against_an_uninitialized_worker_group : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.StartQueue());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_queue_against_a_previously_started_worker_group : with_a_worker_group
	{
		Establish context = () =>
		{
			workerGroup.Initialize(BuildChannel, RestartDelegate);
			workerGroup.StartQueue();
		};

		Because of = () =>
			Try(() => workerGroup.StartQueue());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_queue_using_a_worker_group : with_a_worker_group
	{
		Establish context = () =>
		{
			minWorkers = maxWorkers = 3;
			Build();

			workerGroup.Initialize(() =>
			{
				Interlocked.Increment(ref invocations);
				return mockChannel.Object;
			}, RestartDelegate);
		};

		Because of = () =>
			workerGroup.StartQueue();

		It should_invoke_the_state_callback_provided_for_the_minWorkers_value_provided = () =>
			invocations.ShouldEqual(minWorkers);
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_enqueing_a_null_worker_item : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Enqueue(null));

		It should_throw_an_exception = () =>
		   thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_enqueing_a_work_item_to_a_started_worker_group : with_a_worker_group
	{
		Establish context = () =>
		{
			workerGroup.Initialize(BuildChannel, RestartDelegate);
			workerGroup.Enqueue(x =>
			{
				callback = x.State;
				workerGroup.Dispose();
			});
		};

		Because of = () =>
			TryAndWait(() => workerGroup.StartQueue());

		It should_invoke_the_work_item_callback_provided = () =>
			callback.ShouldEqual(mockChannel.Object);

		static IMessagingChannel callback;
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_an_uninitialized_worker_group : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Restart());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_a_not_yet_started_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(BuildChannel, RestartDelegate);

		Because of = () =>
			Try(() => workerGroup.Restart());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.Restart());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_an_active_worker_group : with_a_worker_group
	{
		Establish context = () =>
		{
			SystemTime.SleepResolver = x => { };
			workerGroup.Initialize(BuildChannel, Restart);
			workerGroup.StartActivity(CountInvocations);
		};
		Cleanup after = () =>
			SystemTime.SleepResolver = null;

		static bool Restart()
		{
			Thread.Sleep(1);
			if (++restartAttempts < 5)
				return false;

			restarted = 0;
			return true;
		}
		static void CountInvocations(IWorkItem<IMessagingChannel> worker)
		{
			while (invocations >= 0)
				worker.PerformOperation(() =>
				{
					if (restarted > 0)
						Interlocked.Increment(ref activityNotCanceled);

					Interlocked.Increment(ref invocations);
				});
		}

		Because of = () =>
		{
			Thread.Sleep(100);
			workerGroup.Restart();
			Interlocked.Increment(ref restarted);
			invocationsBeforeRestart = invocations;
			Thread.Sleep(100);
		};

		It should_initiate_cancellation_current_activities = () =>
			activityNotCanceled.ShouldEqual(0);

		It should_invoke_the_restart_callback_until_it_returns_true = () =>
			restartAttempts.ShouldEqual(5);

		It should_then_resume_invocations_to_the_previously_executing_activity = () =>
			invocations.ShouldBeGreaterThan(invocationsBeforeRestart);

		static int restarted;
		static int activityNotCanceled;
		static int invocationsBeforeRestart;
		static int restartAttempts;
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_disposing_an_active_worker_group : with_a_worker_group
	{
		Establish context = () =>
		{
			minWorkers = maxWorkers = 3;
			Build();

			workerGroup.Initialize(BuildChannel, RestartDelegate);
			workerGroup.StartQueue();
		};

		Because of = () =>
		{
			TryAndWait(() => workerGroup.Dispose());
			Thread.Sleep(50);
		};

		It should_dispose_all_state_objects_retrieved_through_the_state_callback = () =>
			mockChannel.Verify(x => x.Dispose(), Times.Exactly(3));

		It should_clear_the_worker_collection = () =>
			workerGroup.Workers.Count().ShouldEqual(0);
	}

	public abstract class with_a_worker_group
	{
		Establish context = () =>
		{
			workerGroup = null;
			thrown = null;
			minWorkers = 1;
			maxWorkers = 2;
			invocations = 0;

			mockChannel = new Mock<IMessagingChannel>();

			RestartDelegate(); // uncovered code now covered
			Build();
		};
		protected static void Build()
		{
			workerGroup = new TaskWorkerGroup<IMessagingChannel>(minWorkers, maxWorkers);
		}
		protected static void Try(Action action)
		{
			thrown = thrown ?? Catch.Exception(action);
		}
		protected static void TryAndWait(Action callback)
		{
			Try(() =>
			{
				callback();
				Task.WaitAll(workerGroup.Workers.ToArray());
			});
		}

		Cleanup after = () =>
			workerGroup.Dispose();

		protected static Mock<IMessagingChannel> mockChannel;
		protected static TaskWorkerGroup<IMessagingChannel> workerGroup;
		protected static int minWorkers = 1;
		protected static int maxWorkers = 1;
		protected static int invocations;
		protected static Exception thrown;

		protected static readonly Action<IWorkItem<IMessagingChannel>> EmptyActivity = x => { };
		protected static readonly Func<IMessagingChannel> BuildChannel = () => mockChannel.Object;
		protected static readonly Func<bool> RestartDelegate = () => true;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169