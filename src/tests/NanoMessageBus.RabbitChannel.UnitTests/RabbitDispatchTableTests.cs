#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Machine.Specifications;

	[Subject(typeof(RabbitDispatchTable))]
	public class when_querying_for_dispatch_recipients_with_a_null_message_type : using_a_dispatch_table
	{
		Because of = () =>
			Try(() => subscribers = table[null]);

		It should_not_return_a_value = () =>
			subscribers.ShouldBeNull();

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();

		static ICollection<Uri> subscribers;
	}

	[Subject(typeof(RabbitDispatchTable))]
	public class when_querying_for_dispatch_recipients : using_a_dispatch_table
	{
		Because of = () =>
			subscribers = table[typeof(string)].ToArray();

		It should_return_a_single_subscriber = () => 
			subscribers.Length.ShouldEqual(1);

		It should_return_a_fanout_exchange_uri = () => 
			subscribers[0].Scheme.ShouldEqual("fanout");

		It should_contain_the_lowercase_value_of_the_type_name = () => 
			subscribers[0].Authority.ShouldEqual("system-string");

		It should_not_contain_a_routing_key = () => 
			subscribers[0].PathAndQuery.ShouldEqual("/");

		static Uri[] subscribers;
	}

	[Subject(typeof(RabbitDispatchTable))]
	public class when_adding_a_subscriber : using_a_dispatch_table
	{
		Because of = () =>
			Try(() => table.AddSubscriber(null, null, SystemTime.UtcNow));

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(RabbitDispatchTable))]
	public class when_adding_a_recipient : using_a_dispatch_table
	{
		Because of = () =>
			Try(() => table.AddRecipient(null, null));

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(RabbitDispatchTable))]
	public class when_removing_a_subscriber : using_a_dispatch_table
	{
		Because of = () =>
			Try(() => table.Remove(null, null));

		It should_do_nothing = () =>
			thrown.ShouldBeNull();
	}

	public abstract class using_a_dispatch_table
	{
		Establish context = () =>
		{
			table = new RabbitDispatchTable();
			thrown = null;
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static RabbitDispatchTable table;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169