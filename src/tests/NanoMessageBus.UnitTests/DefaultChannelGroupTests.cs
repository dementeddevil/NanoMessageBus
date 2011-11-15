#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using Machine.Specifications;

	[Subject(typeof(DefaultChannelGroup))]
	public class when_
	{
		Establish context = () => { };
		Because of = () => { };
		It should_ = () => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169