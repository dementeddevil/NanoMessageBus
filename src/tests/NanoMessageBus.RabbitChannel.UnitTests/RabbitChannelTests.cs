#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using It = Machine.Specifications.It;

	[Subject(typeof(RabbitChannel))]
	public class when_the_channel : using_a_channel
	{
		Establish context = () => { };
		Because of = () => { };
		It should_ = () => { };
	}

	public abstract class using_a_channel
	{
		protected static RabbitTransactionType transactionType = RabbitTransactionType.None;
		protected static Mock<RabbitSubscription> mockSubscription;
		protected static Mock<IModel> mockRealChannel;
		protected static RabbitChannel channel;

		Establish context = () =>
		{
			mockSubscription = new Mock<RabbitSubscription>();
			mockRealChannel = new Mock<IModel>();

			Initialize();
		};

		protected static void Initialize()
		{
			channel = new RabbitChannel(mockRealChannel.Object, transactionType);
		}

		Cleanup after = () =>
		{
			mockSubscription = null;
			mockRealChannel = null;
			transactionType = RabbitTransactionType.None;
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169