#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
	using System;
	using System.Collections.Generic;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject(typeof(DefaultRoutingTable))]
	public class when_a_null_message_handler_is_provided : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Add((IMessageHandler<object>)null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_a_null_delivery_context_callback_is_provided : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Add((Func<IHandlerContext, IMessageHandler<object>>)null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_attempting_to_route_with_a_null_handler_context : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Route(null, string.Empty));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_attempting_to_route_with_a_null_message : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Route(mockContext.Object, null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_a_registered_handler : with_the_routing_table
	{
		Establish context = () =>
			routes.Add(new GenericHandler<string>(x => received = x));

		Because of = () =>
			routes.Route(mockContext.Object, "Hello, World!");

		It should_pass_the_message_to_the_handler = () =>
			received.ShouldEqual("Hello, World!");

		static object received;
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_the_same_message_to_a_duplicate_handler_registration : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new GenericHandler<string>());
			routes.Add(new GenericHandler<string>());
		};

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_only_route_to_one_registration = () =>
			count.ShouldEqual(1);
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_the_same_message_to_multiple_handlers : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new GenericHandler<string>(x => first = true));
			routes.Add(new SecondHandler());
		};

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_route_to_the_first_registration_first = () =>
			first.ShouldBeTrue();

		It should_route_to_the_next_registration_last = () =>
			last.ShouldBeTrue();

		static bool first;
		static bool last;

		class SecondHandler : IMessageHandler<string>
		{
			public void Handle(string message)
			{
				if (first)
					last = true;
			}
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_multiple_handlers_with_explicit_sequence : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new RegisteredFirstButRunLast(), 2);
			routes.Add(new RegisteredLastButRunFirst(), 1);
		};

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_route_to_the_lowest_sequence_first = () =>
			first.ShouldBeTrue();

		It should_route_to_the_highest_sequence_last = () =>
			last.ShouldBeTrue();

		static bool first;
		static bool last;

		class RegisteredFirstButRunLast : IMessageHandler<string>
		{
			public void Handle(string message)
			{
				if (first) last = true;
			}
		}
		class RegisteredLastButRunFirst : IMessageHandler<string>
		{
			public void Handle(string message)
			{
				if (!last) first = true;
			}
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_with_registered_handlers_of_various_message_types : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new GenericHandler<string>(x => matched = true));
			routes.Add(new GenericHandler<int>()); // increment count
		};

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_route_to_the_handler_that_matches_the_message_type = () =>
			matched.ShouldBeTrue();

		It should_NOT_route_to_the_handler_that_does_not_match_the_message_type = () =>
			count.ShouldEqual(0);

		static bool matched;
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_a_registered_callback : with_the_routing_table
	{
		Establish context = () => routes.Add(ctx =>
		{
			receivedContext = ctx;
			return new GenericHandler<string>(msg => receivedMessage = msg);
		});

		Because of = () =>
			routes.Route(mockContext.Object, "Hello, World!");

		It should_provide_the_handler_context = () =>
			receivedContext.ShouldEqual(mockContext.Object);

		It should_provide_the_message_to_the_registered_callback_handler = () =>
			receivedMessage.ShouldEqual("Hello, World!");

		static IHandlerContext receivedContext;
		static string receivedMessage;
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_a_callback_which_returns_no_handler : with_the_routing_table
	{
		Establish context = () => routes.Add(ctx => (IMessageHandler<string>)null);

		Because of = () =>
			Try(() => routes.Route(mockContext.Object, string.Empty));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_the_same_message_to_a_duplicate_callback_registration : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(GetHandler, 1, typeof(GenericHandler<string>));
			routes.Add(GetHandler, 1, typeof(GenericHandler<string>));
		};
		static IMessageHandler<string> GetHandler(IHandlerContext context)
		{
			return new GenericHandler<string>();
		}

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_only_route_to_one_registration = () =>
			count.ShouldEqual(1);
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_the_same_message_to_multiple_callbacks : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(ctx => new GenericHandler<string>(x => first = true));
			routes.Add(ctx => new SecondHandler());
		};

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_route_to_the_first_registration_first = () =>
			first.ShouldBeTrue();

		It should_route_to_the_next_registration_last = () =>
			last.ShouldBeTrue();

		static bool first;
		static bool last;

		class SecondHandler : IMessageHandler<string>
		{
			public void Handle(string message)
			{
				if (first)
					last = true;
			}
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_multiple_callbacks_with_explicit_sequence : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(ctx => new RegisteredFirstButRunLast(), 2);
			routes.Add(ctx => new RegisteredLastButRunFirst(), 1);
		};

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_route_to_the_lowest_sequence_first = () =>
			first.ShouldBeTrue();

		It should_route_to_the_highest_sequence_last = () =>
			last.ShouldBeTrue();

		static bool first;
		static bool last;

		class RegisteredFirstButRunLast : IMessageHandler<string>
		{
			public void Handle(string message)
			{
				if (first) last = true;
			}
		}
		class RegisteredLastButRunFirst : IMessageHandler<string>
		{
			public void Handle(string message)
			{
				if (!last) first = true;
			}
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_with_registered_callbacks_of_various_message_types : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(BuildHandler<string>);
			routes.Add(BuildHandler<int>);
		};
		static IMessageHandler<T> BuildHandler<T>(IHandlerContext ctx)
		{
			return new GenericHandler<T>(x => values[typeof(T)] = x);
		}

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_route_to_the_handler_that_matches_the_message_type = () =>
			values.ContainsKey(typeof(string)).ShouldBeTrue();

		It should_NOT_route_to_the_handler_that_does_not_match_the_message_type = () =>
			values.ContainsKey(typeof(int)).ShouldBeFalse();

		static readonly IDictionary<Type, object> values = new Dictionary<Type, object>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message : with_the_routing_table
	{
		Establish context = () =>
		{
			mockContext.Setup(x => x.ContinueHandling).Returns(() => count < 2);

			routes.Add(GetHandler);
			routes.Add(GetHandler);
			routes.Add(GetHandler); // never called
		};
		static IMessageHandler<string> GetHandler(IHandlerContext context)
		{
			return new GenericHandler<string>();
		}

		Because of = () =>
			routes.Route(mockContext.Object, string.Empty);

		It should_only_dispatch_to_handlers_while_the_context_can_continue_handling = () =>
			count.ShouldEqual(2);
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_with_no_registered_handlers : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Route(mockContext.Object, string.Empty));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	public abstract class with_the_routing_table
	{
		Establish context = () =>
		{
			routes = new DefaultRoutingTable();
			mockContext = new Mock<IHandlerContext>();
			mockContext.Setup(x => x.ContinueHandling).Returns(true);
			count = 0;
			thrown = null;
		};
		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static DefaultRoutingTable routes;
		protected static Mock<IHandlerContext> mockContext;
		protected static Exception thrown;
		protected static int count;

		protected class GenericHandler<T> : IMessageHandler<T>
		{
			public void Handle(T message)
			{
				this.callback(message);
			}

			public GenericHandler()
				: this(null)
			{
			}
			public GenericHandler(Action<T> callback)
			{
				this.callback = callback ?? (x => count++);
			}

			private readonly Action<T> callback;
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169