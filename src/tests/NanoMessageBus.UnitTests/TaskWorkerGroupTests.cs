using FluentAssertions;

#pragma warning disable 169, 414
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
			thrown.Should().BeOfType<ArgumentException>();
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
			thrown.Should().BeOfType<ArgumentException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_a_null_state_callback_is_provided_during_initialization : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Initialize(null, RestartDelegate));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_a_null_restart_callback_is_provided_during_initialization : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Initialize(BuildChannel, null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_initializing_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.Initialize(BuildChannel, RestartDelegate));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_initializing_an_already_initialized_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(BuildChannel, RestartDelegate);

		Because of = () =>
			Try(() => workerGroup.Initialize(BuildChannel, RestartDelegate));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_null_activity : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.StartActivity(null));

		It should_throw_an_exception = () =>
		   thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity_without_initializing_first : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.StartActivity(EmptyActivity));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity_against_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.StartActivity(EmptyActivity));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
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
			thrown.Should().BeOfType<InvalidOperationException>();
	}
	
	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_acquiring_state_throws_an_exception : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(() => { throw new Exception(); }, RestartDelegate);

		Because of = () =>
			TryAndWait(() => workerGroup.StartActivity(EmptyActivity));

		It should_bubble_the_exception_to_the_main_thread = () =>
			thrown.Should().NotBeNull();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_an_activity : with_a_worker_group
	{
		Establish context = () =>
		{
			minWorkers = maxWorkers = 42;
			Build();

			workerGroup.Initialize(() =>
			{
				Interlocked.Increment(ref invocations);
				return mockChannel.Object;
			}, RestartDelegate);
		};

		Because of = () =>
		{
			workerGroup.StartActivity(EmptyActivity);
			Thread.Sleep(10);
		};

		It should_invoke_the_state_callback_provided_for_the_minWorkers_value_provided = () =>
			invocations.Should().Be(minWorkers);
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_the_state_callback_returns_null : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(() => null, () => restarted = true);

		Because of = () =>
			TryAndWait(() => workerGroup.StartActivity(EmptyActivity));

		It should_wait_for_restart_to_be_invoked = () =>
			Thread.Sleep(100);

		It should_NOT_throw_an_exception = () =>
			thrown.Should().BeNull();

		It should_invoke_the_restart_callback = () =>
			restarted.Should().Be(true);

		static bool restarted;
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_running_an_activity : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(BuildChannel, RestartDelegate);

		Because of = () =>
			TryAndWait(() => workerGroup.StartActivity(x => callback = x.State));

		It should_pass_the_current_state_to_the_callback = () =>
			callback.Should().Be(mockChannel.Object);

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
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_starting_a_queue_against_an_uninitialized_worker_group : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.StartQueue());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
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
		{
			workerGroup.StartQueue();

			if (!MicrosoftRuntime)
				Thread.Sleep(1000);
		};

		It should_invoke_the_state_callback_provided_for_the_minWorkers_value_provided = () =>
			invocations.Should().Be(minWorkers);
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
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_enqueing_a_null_worker_item : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Enqueue(null));

		It should_throw_an_exception = () =>
		   thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_enqueing_a_worker_item_to_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			enqueued = workerGroup.Enqueue(x => { });

		It should_not_enqueue_the_work_item = () =>
		   enqueued.Should().BeFalse();

		static bool enqueued;
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
			callback.Should().Be(mockChannel.Object);

		static IMessagingChannel callback;
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_enqueing_a_work_item_beyond_the_max_capacity_of_the_worker_group_buffer : with_a_worker_group
	{
		Establish context = () =>
		{
			minWorkers = maxWorkers = 1;
			maxBufferSize = 1;
			Build();

			workerGroup.Enqueue(FirstCallback); // should never be invoked
			workerGroup.Enqueue(NextCallback);
			workerGroup.Initialize(BuildChannel, RestartDelegate);
		};

		Because of = () =>
			TryAndWait(() => workerGroup.StartQueue());

		It should_discard_the_earliest_work_item = () =>
			firstWorkItemPerformed.Should().BeFalse();

		It should_invoke_the_latest_work_item = () =>
			secondWorkItemPerformed.Should().BeTrue();

		Cleanup after = () =>
			FirstCallback(null); // code coverage

		static void FirstCallback(IWorkItem<IMessagingChannel> item)
		{
			firstWorkItemPerformed = true;
		}
		static void NextCallback(IWorkItem<IMessagingChannel> item)
		{
			secondWorkItemPerformed = true;
			workerGroup.Dispose();
		}

		static bool firstWorkItemPerformed;
		static bool secondWorkItemPerformed;
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_an_uninitialized_worker_group : with_a_worker_group
	{
		Because of = () =>
			Try(() => workerGroup.Restart());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_a_not_yet_started_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Initialize(BuildChannel, RestartDelegate);

		Because of = () =>
			Try(() => workerGroup.Restart());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<InvalidOperationException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_restarting_a_disposed_worker_group : with_a_worker_group
	{
		Establish context = () =>
			workerGroup.Dispose();

		Because of = () =>
			Try(() => workerGroup.Restart());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_the_state_callback_returns_null_during_restart_operations : with_a_worker_group
	{
		Establish context = () =>
		{
			workerGroup.Initialize(() => null, () => ++invocations > 0);
			workerGroup.StartQueue();
		};

		Because of = () =>
			TryAndWait(() => workerGroup.Restart());

		It should_still_invoke_the_restart_callback = () =>
			invocations.Should().BeGreaterThan(0);
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_the_worker_group_is_disposed_while_restarting : with_a_worker_group
	{
		Establish context = () => workerGroup.Initialize(BuildChannel, () =>
		{
			workerGroup.Dispose();
			return true;
		});

		Because of = () => TryAndWait(() => workerGroup.StartActivity(x =>
		{
			invocations++;
			workerGroup.Restart();
		}));

		It should_not_resume_the_activity = () =>
			invocations.Should().Be(1);
	}

	[Subject(typeof(TaskWorkerGroup<IMessagingChannel>))]
	public class when_multiple_workers_attempt_to_restart_simultaneously : with_a_worker_group
	{
		Establish context = () => workerGroup.Initialize(BuildChannel, () =>
		{
			invocations++;
			workerGroup.Restart(); // while this restart is running, start another
			return true;
		});

		Because of = () =>
		{
			TryAndWait(() => workerGroup.StartActivity(x =>
			{
				if (invocations == 0)
					workerGroup.Restart(); // kick off the restart
				else
					workerGroup.Dispose();
			}));
			Thread.Sleep(100);
		};

		It should_only_allow_a_single_restart_instance_at_a_time = () =>
			invocations.Should().Be(1);
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
		{
			SystemTime.SleepResolver = null;

			// code coverage
			invocations = -1;
			restarted = 1;
			CountInvocations(null);
			Increment();
		};

		static bool Restart()
		{
			Thread.Sleep(1);
			if (++restartAttempts < RestartAttempts)
				return false;

			restarted = 0;
			return true;
		}
		static void CountInvocations(IWorkItem<IMessagingChannel> worker)
		{
			while (invocations >= 0)
				worker.PerformOperation(Increment);
		}
		private static Task Increment()
		{
			if (restarted > 0)
				Interlocked.Increment(ref activityNotCanceled);

			Interlocked.Increment(ref invocations);
		    return Task.FromResult(true);
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
			activityNotCanceled.Should().Be(0);

		It should_invoke_the_restart_callback_until_it_returns_true = () =>
			restartAttempts.Should().Be(RestartAttempts);

		It should_then_resume_invocations_to_the_previously_executing_activity = () =>
			invocations.Should().BeGreaterThan(invocationsBeforeRestart);

		static int restarted;
		static int activityNotCanceled;
		static int invocationsBeforeRestart;
		static int restartAttempts;
		const int RestartAttempts = 5;
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
			Thread.Sleep(100);
		};

		It should_dispose_all_state_objects_retrieved_through_the_state_callback = () =>
			mockChannel.Verify(x => x.Dispose(), Times.Exactly(3));

		It should_clear_the_worker_collection = () =>
			workerGroup.Workers.Count().Should().Be(0);
	}

	public abstract class with_a_worker_group
	{
		Establish context = () =>
		{
			SystemTime.SleepResolver = x => { };

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
			workerGroup = new TaskWorkerGroup<IMessagingChannel>(minWorkers, maxWorkers, maxBufferSize);
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
		{
			workerGroup.Dispose();
			SystemTime.SleepResolver = null;
		};

		protected static Mock<IMessagingChannel> mockChannel;
		protected static TaskWorkerGroup<IMessagingChannel> workerGroup;
		protected static int minWorkers;
		protected static int maxWorkers;
		protected static int maxBufferSize;
		protected static int invocations;
		protected static Exception thrown;

		protected static readonly Action<IWorkItem<IMessagingChannel>> EmptyActivity = x => { };
		protected static readonly Func<IMessagingChannel> BuildChannel = () => mockChannel.Object;
		protected static readonly Func<bool> RestartDelegate = () => true;
		protected static readonly bool MicrosoftRuntime = Type.GetType("Mono.Runtime") == null;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414