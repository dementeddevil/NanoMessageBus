#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using System;
	using System.Configuration;
	using System.Linq;
	using System.Threading;
	using Machine.Specifications;
	using RabbitMQ.Client;

	[Subject(typeof(RabbitChannel))]
	public class when_connecting_with_bad_credentials : using_the_channel
	{
		Establish context = () =>
			connectionFactory.Password = "bad password";

		Because of = () =>
			thrown = Catch.Exception(() => OpenSender());

		It should_throw_a_ChannelConnectionException = () =>
			thrown.ShouldBeOfType<ChannelConnectionException>();
	}

	[Subject(typeof(RabbitChannel))]
	public class when_dispatching_a_message : using_the_channel
	{
		Establish context = () =>
			OpenSender();

		Because of = () =>
			thrown = Catch.Exception(() => senderChannel.Send(BuildEnvelope("some message")));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(RabbitChannel))]
	public class when_sending_and_then_receiving_a_message : using_the_channel
	{
		Establish context = () =>
			receiverConfig.WithCleanQueue();

		Because of = () =>
		{
			OpenReceiver(delivery =>
				(received = (string)delivery.CurrentMessage.Messages.First()).Length > 0);
			OpenSender().Send(BuildEnvelope(message));
		};

		It should_wait_a_little_bit_to_receive_the_message = () =>
			WaitUntil(() => received != null, DefaultSleepTimeout);

		It should_receive_the_message_that_was_sent = () =>
			received.ShouldEqual(message);

		static string received;
		static readonly string message = Guid.NewGuid().ToString();
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_transaction_to_dispatch_a_message_is_not_committed
	{
		It should_not_dispatch_the_message;
	}

	[Subject(typeof(RabbitChannel))]
	public class when_the_transaction_to_receive_a_message_is_not_committed
	{
		It should_not_remove_the_message_from_the_input_queue;
	}

	public abstract class using_the_channel
	{
		Establish context = () =>
		{
			connector = null;
			senderChannel = null;
			receiverChannel = null;

			wireup = new RabbitWireup()
				.WithConnectionFactory(connectionFactory = new ConnectionFactory())
				.WithEndpoint(connectionUri = new Uri(ConfigurationManager.AppSettings["ConnectionUri"]))
				.WithShutdownTimout(ShutdownTimeout)
				.AddChannelGroup(x =>
				{
					senderConfig = x.WithGroupName(DefaultSenderChannelGroup);
				})
				.AddChannelGroup(x =>
				{
					receiverConfig = x
						.WithGroupName(DefaultReceiverChannelGroup)
						.WithInputQueue(DefaultReceiverInputQueue)
						.WithMessageTypes(new[] { typeof(string) });
				});
		};

		protected static IMessagingChannel OpenSender()
		{
			return senderChannel = OpenConnection().Connect(senderConfig.GroupName);
		}
		protected static IMessagingChannel OpenReceiver()
		{
			return receiverChannel = OpenConnection().Connect(receiverConfig.GroupName);
		}
		protected static IChannelConnector OpenConnection()
		{
			return connector = connector ?? wireup.Build();
		}

		protected static ChannelEnvelope BuildEnvelope(object message, params Uri[] recipients)
		{
			if (recipients == null || recipients.Length == 0)
				recipients = new[] { new Uri("direct://default/" + DefaultReceiverInputQueue) };

			var channelMessage = new ChannelMessage(
				Guid.NewGuid(), Guid.NewGuid(), null, null, new[] { message });
			return new ChannelEnvelope(channelMessage, recipients);
		}

		protected static void OpenReceiver(Func<IDeliveryContext, bool> callback)
		{
			OpenReceiver();
			new Thread(() => receiverChannel.Receive(delivery =>
			{
				if (!callback(delivery))
					receiverChannel.BeginShutdown();
			})).Start();
		}
		protected static void WaitUntil(Func<bool> callback, TimeSpan maxWait)
		{
			var started = SystemTime.UtcNow;

			while (!callback())
			{
				Thread.Sleep(10);
				if ((SystemTime.UtcNow - started) > maxWait)
					break;
			}
		}

		Cleanup after = () =>
		{
			if (senderChannel != null)
				senderChannel.Dispose();

			if (receiverChannel != null)
			{
				receiverChannel.BeginShutdown();
				receiverChannel.Dispose();	
			}

			if (connector != null)
				connector.Dispose();
		};

		protected const string DefaultReceiverChannelGroup = "receiver";
		protected const string DefaultSenderChannelGroup = "sender";
		protected const string DefaultReceiverInputQueue = "my-queue";
		protected static readonly TimeSpan DefaultSleepTimeout = TimeSpan.FromMilliseconds(250);
		static readonly TimeSpan ShutdownTimeout = TimeSpan.FromMilliseconds(100);
		protected static RabbitWireup wireup;
		protected static ConnectionFactory connectionFactory;
		protected static Uri connectionUri;
		protected static RabbitChannelGroupConfiguration receiverConfig;
		protected static RabbitChannelGroupConfiguration senderConfig;
		protected static RabbitConnector connector;
		protected static IMessagingChannel senderChannel;
		protected static IMessagingChannel receiverChannel;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169