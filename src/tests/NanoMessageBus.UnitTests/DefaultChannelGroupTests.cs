#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultChannelGroup))]
	public class when_constructing_a_new_channel_group : with_a_channel_group
	{
		Establish context = () =>
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);

		It should_contain_the_same_dispatch_mode_as_the_configuration = () =>
			channelGroup.DispatchOnly.ShouldBeTrue();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_the_group_is_initialized : with_a_channel_group
	{
		Establish context = () =>
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);

		Because of = () =>
			channelGroup.Initialize();

		It should_initialize_the_worker_group = () =>
			mockWorkers.Verify(x => x.Initialize(
				Moq.It.IsAny<Func<IMessagingChannel>>(),
				Moq.It.IsAny<Func<bool>>()), Times.Once());

		It should_provide_a_callback_to_the_worker_group_to_build_a_channel = () =>
			stateCallback().ShouldEqual(mockChannel.Object);
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_the_group_is_initialized_more_than_once : with_a_channel_group
	{
		Establish context = () =>
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);

		Because of = () =>
		{
			channelGroup.Initialize();
			channelGroup.Initialize();
		};

		It should_only_initialize_on_the_first_call = () =>
			mockWorkers.Verify(x => x.Initialize(
				Moq.It.IsAny<Func<IMessagingChannel>>(),
				Moq.It.IsAny<Func<bool>>()), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_initialize_a_disposed_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Dispose();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.Initialize());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_initializing_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);

		Because of = () =>
			channelGroup.Initialize();

		It should_instruct_the_worker_group_to_start_watching_the_work_item_queue = () =>
			mockWorkers.Verify(x => x.StartQueue(), Times.Once());

		It should_connect_to_the_messaging_infrastructure = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_initializing_an_dispatch_only_group_throws_a_ChannelConnectionException : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Throws(new ChannelConnectionException());
		};

		Because of = () =>
			channelGroup.Initialize();

		It should_consume_the_exception = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_reestablishing_a_connection : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			restarted = restartCallback();

		It should_open_a_new_channel = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(1));

		It should_indicate_success = () =>
			restarted.ShouldBeTrue();

		static bool restarted;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_reestablishing_a_connection_throws_a_ChannelConnectionException : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Throws(new ChannelConnectionException());
			channelGroup.Initialize();
		};

		Because of = () =>
			restarted = restartCallback();

		It should_attempt_to_open_a_new_channel = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(1));

		It should_indicate_failure = () =>
			restarted.ShouldBeFalse();

		static bool restarted;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_reestablishing_a_connection_throws_an_ObjectDisposedException : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConnector
				.Setup(x => x.Connect(ChannelGroupName))
				.Callback(() =>
				{
					throw new ObjectDisposedException(string.Empty); // throw after first call
				});
			channelGroup.Initialize();
		};

		Because of = () =>
			restarted = restartCallback();

		It should_attempt_to_open_a_new_channel = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(1));

		It should_indicate_failure = () =>
			restarted.ShouldBeFalse();

		static bool restarted;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_opening_a_caller_owned_channel_on_an_uninitialized_group : with_a_channel_group
	{
		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.OpenChannel());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_opening_a_caller_owned_channel_on_a_disposed_group : with_a_channel_group
	{
		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.OpenChannel());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_opening_a_caller_owned_channel : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);
			channelGroup.Initialize();
		};

		Because of = () =>
			opened = channelGroup.OpenChannel();

		It should_open_a_new_channel = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(1));

		It should_return_a_reference_to_the_opened_channel = () =>
			opened.ShouldEqual(mockChannel.Object);

		static IMessagingChannel opened;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_opening_a_caller_owned_channel_fails_to_connect_to_the_messaging_infrastructure : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Throws(new ChannelConnectionException());
			channelGroup.Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.OpenChannel());

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_a_message_with_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockWorkers
				.Setup(x => x.Enqueue(Moq.It.IsAny<Action<IWorkItem<IMessagingChannel>>>()))
				.Returns(true)
				.Callback<Action<IWorkItem<IMessagingChannel>>>(x => x(mockWorker.Object));

			channelGroup.Initialize();
		};

		Because of = () =>
			enqueued = channelGroup.BeginDispatch(x => IncrementInvocations());

		It should_indicate_the_work_item_was_enqueued = () =>
			enqueued.ShouldBeTrue();

		It should_prepare_a_dispatch_context_on_the_underlying_channel = () =>
			mockChannel.Verify(x => x.PrepareDispatch(null, null), Times.Once());

		It should_invoke_the_callback_method_provided = () =>
			invocations.ShouldEqual(1);

		static IChannelTransaction current;
		static bool enqueued;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_the_underlying_workers_are_no_longer_enqueuing_envelope_for_dispatch : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);

			mockWorkers
				.Setup(x => x.Enqueue(Moq.It.IsAny<Action<IWorkItem<IMessagingChannel>>>()))
				.Returns(false);

			channelGroup.Initialize();
		};

		Because of = () =>
			enqueued = channelGroup.BeginDispatch(x => { });

		It should_indicate_the_message_was_not_enqueued = () =>
			enqueued.ShouldBeFalse();

		static bool enqueued;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_throws_a_ChannelConnectionException : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockChannel.Setup(x => x.PrepareDispatch(null, null)).Throws(new ChannelConnectionException());
			mockWorkers
				.Setup(x => x.Enqueue(Moq.It.IsAny<Action<IWorkItem<IMessagingChannel>>>()))
				.Callback<Action<IWorkItem<IMessagingChannel>>>(x =>
				{
					// prevent an infinite loop because the queue happens to run on the same thread during this test
					if (count++ < 1) x(mockWorker.Object);
				});

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginDispatch(x => { });

		It should_attempt_to_prepare_a_dispatch_context = () =>
			mockChannel.Verify(x => x.PrepareDispatch(null, null), Times.Once());

		It should_restart_the_worker_group = () =>
			mockWorkers.Verify(x => x.Restart(), Times.Once());

		It should_re_enqueue_the_failed_async_dispatch = () =>
			mockWorkers.Verify(x => x.Enqueue(Moq.It.IsAny<Action<IWorkItem<IMessagingChannel>>>()), Times.Exactly(2));

		static int count;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_to_a_full_duplex_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(x => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_callback_is_provided_for_asynchronous_dispatch : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_asynchronously_dispatching_a_message_without_first_initializing_the_group : with_a_channel_group
	{
		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(x => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_against_a_disposed_group : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);

			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(x => { }));

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages_on_a_full_duplex_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			channelGroup.BeginReceive(x => { });

		It should_startup_the_worker_group = () =>
			mockWorkers.Verify(x => x.StartActivity(Moq.It.IsAny<Action<IWorkItem<IMessagingChannel>>>()), Times.Once());

		It should_have_the_worker_attempt_to_receive_a_message_on_the_channel_using_the_callback_provided = () =>
			mockChannel.Verify(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_the_attempt_to_receive_a_message_throws_a_ChannelConnectionException : with_a_channel_group
	{
		private Establish context = () =>
		{
			mockWorkers.Setup(x => x.Restart());
			mockChannel
				.Setup(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()))
				.Callback<Action<IDeliveryContext>>(callback => callback(null));
			mockWorker
				.Setup(x => x.PerformOperation(Moq.It.IsAny<Action>()))
				.Callback<Action>(x => x());

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginReceive(x => { throw new ChannelConnectionException(); });

		It should_restart_the_workers = () =>
			mockWorkers.Verify(x => x.Restart(), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages_without_providing_a_callback : with_a_channel_group
	{
		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_begin_receiving_messages_without_first_initializing_the_group : with_a_channel_group
	{
		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages_against_a_disposed_group : with_a_channel_group
	{
		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(c => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_more_than_one_callback_has_been_provided_for_receiving_messages_from_the_group : with_a_channel_group
	{
		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.BeginReceive(callback);
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static readonly Action<IDeliveryContext> callback = channel => { };
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_from_a_dispatch_only_channel_group : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			channelGroup.Initialize();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginReceive(callback));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();

		static readonly Action<IDeliveryContext> callback = channel => { };
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_the_underlying_channel_throws_an_ObjectDisposedException : with_a_channel_group
	{
		private Establish context = () =>
		{
			mockChannel
				.Setup(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()))
				.Throws(new ObjectDisposedException("disposed"));

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginReceive(x => { });

		It should_consume_the_exception = () =>
			mockChannel.Verify(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_restart_a_disposed_worker_group : with_a_channel_group
	{
		private Establish context = () =>
		{
			mockWorkers
				.Setup(x => x.Restart())
				.Throws(new ObjectDisposedException("disposed"));
			mockChannel
				.Setup(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()))
				.Throws(new ChannelConnectionException());

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginReceive(x => { });

		It should_consume_the_exception = () =>
			mockWorkers.Verify(x => x.Restart(), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_disposing_the_channel_group : with_a_channel_group
	{
		Establish context = () =>
			mockWorkers.Setup(x => x.Dispose());

		Because of = () =>
			channelGroup.Dispose();

		It should_dispose_of_the_worker_group = () =>
			mockWorkers.Verify(x => x.Dispose(), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_disposing_the_channel_group_more_than_once : with_a_channel_group
	{
		private Establish context = () =>
		{
			mockWorkers.Setup(x => x.Dispose());
			channelGroup.Dispose();
		};

		Because of = () =>
			channelGroup.Dispose();

		It should_only_dispose_of_the_worker_group_the_first_time = () =>
			mockWorkers.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class with_a_channel_group
	{
		Establish context = () =>
		{
			mockConnector = new Mock<IChannelConnector>();
			mockChannel = new Mock<IMessagingChannel>();
			mockConfig = new Mock<IChannelGroupConfiguration>();
			mockWorkers = new Mock<IWorkerGroup<IMessagingChannel>>();
			mockWorker = new Mock<IWorkItem<IMessagingChannel>>();

			var mockEnvelope = new Mock<ChannelEnvelope>();
			mockEnvelope.Setup(x => x.Message).Returns(new Mock<ChannelMessage>().Object);
			envelope = mockEnvelope.Object;

			stateCallback = null;
			restartCallback = null;
			invocations = 0;

			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);
			mockConfig.Setup(x => x.GroupName).Returns(ChannelGroupName);

			mockWorker.Setup(x => x.State).Returns(mockChannel.Object);
			mockWorkers
				.Setup(x => x.Initialize(Moq.It.IsAny<Func<IMessagingChannel>>(), Moq.It.IsAny<Func<bool>>()))
				.Callback<Func<IMessagingChannel>, Func<bool>>((state, restart) =>
				{
					stateCallback = state;
					restartCallback = restart;
				});

			mockWorkers
				.Setup(x => x.StartActivity(Moq.It.IsAny<Action<IWorkItem<IMessagingChannel>>>()))
				.Callback<Action<IWorkItem<IMessagingChannel>>>(x => x(mockWorker.Object)); // invoke callback as soon as it's provided

			channelGroup = new DefaultChannelGroup(mockConnector.Object, mockConfig.Object, mockWorkers.Object);
		};

		Cleanup after = () =>
		{
			channelGroup.Dispose();
			SystemTime.TimeResolver = null;
			SystemTime.SleepResolver = null;
		};

		protected static void IncrementInvocations()
		{
			invocations++;
		}

		protected const string ChannelGroupName = "Test Channel Group";
		protected static DefaultChannelGroup channelGroup;
		protected static Mock<IChannelConnector> mockConnector;
		protected static Mock<IChannelGroupConfiguration> mockConfig;
		protected static Mock<IWorkerGroup<IMessagingChannel>> mockWorkers;
		protected static Mock<IWorkItem<IMessagingChannel>> mockWorker;
		protected static ChannelEnvelope envelope;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Exception thrown;
		protected static Func<IMessagingChannel> stateCallback;
		protected static Func<bool> restartCallback;
		protected static int invocations;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169, 414