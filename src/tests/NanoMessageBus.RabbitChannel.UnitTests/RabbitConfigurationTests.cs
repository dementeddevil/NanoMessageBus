#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.RabbitChannel
{
	using Machine.Specifications;

	[Subject(typeof(RabbitConfiguration))]
	public class when_the_configuration
	{
		Establish context = () => { };
		Because of = () => { };
		It should_ = () => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169