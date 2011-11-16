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

	[Subject(typeof(DefaultChannelGroup))]
	public class when_asynchronously_dispatching_to_a_full_duplex_group
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}

	[Subject(typeof(DefaultChannelGroup))]
	public class when_synchronously_dispatching_to_a_full_duplex_group
	{
		Establish context = () => { };
		Because of = () => { };
		It should_throw_an_exception = () => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169