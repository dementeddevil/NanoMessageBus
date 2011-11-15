#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using Machine.Specifications;

	[Subject(typeof(RabbitTransaction))]
	public class when_the_transaction
	{
		Establish context = () => { };
		Because of = () => { };
		It should_ = () => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169