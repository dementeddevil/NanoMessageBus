#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Serialization
{
	using Machine.Specifications;

	[Subject(typeof(BinarySerializer))]
	public class when_the_serializer
	{
		Establish context = () => { };
		Because of = () => { };
		It should_ = () => { };
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169