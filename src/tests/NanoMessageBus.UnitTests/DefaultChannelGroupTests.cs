#pragma warning disable 169
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

		It should_establish_a_messaging_channel = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Once());

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
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Once());
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
	public class when_initializing_throws_a_ChannelConnectionException : with_a_channel_group
	{
		Establish context = () =>
			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Throws(new ChannelConnectionException());

		Because of = () =>
			channelGroup.Initialize();

		It should_consume_the_exception = () =>
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_initializing_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockWorkers.Setup(x => x.StartQueue());
		};

		Because of = () =>
			channelGroup.Initialize();

		It should_instruct_the_worker_group_to_start_watching_the_work_item_queue = () =>
			mockWorkers.Verify(x => x.StartQueue(), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_reestablishing_a_connection : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			restarted = restartCallback();

		It should_open_a_new_channel = () => // once for the ChannelGroup initialize and once for restart
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(2));

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

		It should_attempt_to_open_a_new_channel = () => // once for the ChannelGroup initialize and once for restart
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(2));

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
					if (invocations++ > 0)
						throw new ObjectDisposedException(string.Empty); // throw after first call
				});
			channelGroup.Initialize();
		};

		Because of = () =>
			restarted = restartCallback();

		It should_attempt_to_open_a_new_channel = () => // once for the ChannelGroup initialize and once for restart
			mockConnector.Verify(x => x.Connect(ChannelGroupName), Times.Exactly(2));

		It should_indicate_failure = () =>
			restarted.ShouldBeFalse();

		static bool restarted;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_a_message_to_a_dispatch_only_group : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockChannel.Setup(x => x.Send(envelope));
			mockChannel.Setup(x => x.CurrentTransaction).Returns(new Mock<IChannelTransaction>().Object);
			mockWorkers
				.Setup(x => x.Enqueue(Moq.It.IsAny<Action<IMessagingChannel>>()))
				.Callback<Action<IMessagingChannel>>(x => x(mockChannel.Object));

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginDispatch(envelope, trx =>
			{
				current = trx;
				IncrementInvocations();
			});

		It should_pass_the_message_to_exactly_one_of_the_underlying_channels = () =>
			mockChannel.Verify(x => x.Send(envelope), Times.Once());

		It should_invoke_the_callback_method_provided = () =>
			invocations.ShouldEqual(1);

		It should_provide_the_current_transaction_to_the_callback = () =>
			current.ShouldEqual(mockChannel.Object.CurrentTransaction);

		static IChannelTransaction current;
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_throws_a_ChannelConnectionException : with_a_channel_group
	{
		Establish context = () =>
		{
			mockConfig.Setup(x => x.DispatchOnly).Returns(true);
			mockChannel.Setup(x => x.Send(envelope)).Throws(new ChannelConnectionException());
			mockWorkers
				.Setup(x => x.Enqueue(Moq.It.IsAny<Action<IMessagingChannel>>()))
				.Callback<Action<IMessagingChannel>>(x => x(mockChannel.Object));

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginDispatch(envelope, trx => { });

		It should_attempt_to_send_the_message = () =>
			mockChannel.Verify(x => x.Send(envelope), Times.Once());

		It should_restart_the_worker_group = () =>
			mockWorkers.Verify(x => x.Restart(), Times.Once());
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_to_a_full_duplex_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_message_is_provided_to_asynchronously_dispatch : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(null, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_no_completion_callback_is_provided_for_asynchronous_dispatch : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_asynchronously_dispatching_a_message_without_first_initializing_the_group : with_a_channel_group
	{
		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<InvalidOperationException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_against_a_disposed_group : with_a_channel_group
	{
		Establish context = () =>
		{
			channelGroup.Initialize();
			channelGroup.Dispose();
		};

		Because of = () =>
			thrown = Catch.Exception(() => channelGroup.BeginDispatch(envelope, trx => { }));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ObjectDisposedException>();
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_attempting_to_receive_messages_on_a_full_duplex_group : with_a_channel_group
	{
		Establish context = () =>
			channelGroup.Initialize();

		Because of = () =>
			channelGroup.BeginReceive(callback);

		It should_startup_the_worker_group = () =>
			mockWorkers.Verify(x => x.StartActivity(Moq.It.IsAny<Action<IMessagingChannel>>()), Times.Once());

		It should_have_the_worker_attempt_to_receive_a_message_on_the_channel_using_the_callback_provided = () =>
			mockChannel.Verify(x => x.Receive(callback), Times.Once());

		static readonly Action<IDeliveryContext> callback = context => { };
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_the_attempt_to_receive_a_message_throws_a_ChannelConnectionException : with_a_channel_group
	{
		private Establish context = () =>
		{
			mockWorkers.Setup(x => x.Restart());
			mockChannel
				.Setup(x => x.Receive(Moq.It.IsAny<Action<IDeliveryContext>>()))
				.Throws(new ChannelConnectionException());

			channelGroup.Initialize();
		};

		Because of = () =>
			channelGroup.BeginReceive(x => { });

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
			envelope = new Mock<ChannelEnvelope>().Object;
			stateCallback = null;
			restartCallback = null;
			invocations = 0;

			mockConnector.Setup(x => x.Connect(ChannelGroupName)).Returns(mockChannel.Object);
			mockConfig.Setup(x => x.GroupName).Returns(ChannelGroupName);

			mockWorkers
				.Setup(x => x.Initialize(Moq.It.IsAny<Func<IMessagingChannel>>(), Moq.It.IsAny<Func<bool>>()))
				.Callback<Func<IMessagingChannel>, Func<bool>>((state, restart) =>
				{
					stateCallback = state;
					restartCallback = restart;
				});

			mockWorkers
				.Setup(x => x.StartActivity(Moq.It.IsAny<Action<IMessagingChannel>>()))
				.Callback<Action<IMessagingChannel>>(x => x(mockChannel.Object)); // invoke callback as soon as it's provided

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
		protected static ChannelEnvelope envelope;
		protected static Mock<IMessagingChannel> mockChannel;
		protected static Exception thrown;
		protected static Func<IMessagingChannel> stateCallback;
		protected static Func<bool> restartCallback;
		protected static int invocations;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169