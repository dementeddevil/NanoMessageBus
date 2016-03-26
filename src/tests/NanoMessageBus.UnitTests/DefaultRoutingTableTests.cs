using System.Threading.Tasks;

#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace NanoMessageBus
{
    using System;
    using System.Collections.Generic;
    using Machine.Specifications;
    using Moq;
    using It = Machine.Specifications.It;
    using FluentAssertions;

    [Subject(typeof(DefaultRoutingTable))]
	public class when_a_null_message_handler_is_provided : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Add((IMessageHandler<object>)null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_a_null_delivery_context_callback_is_provided : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Add((Func<IHandlerContext, IMessageHandler<object>>)null));

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_attempting_to_route_with_a_null_handler_context : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Route(null, string.Empty).Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_attempting_to_route_with_a_null_message : with_the_routing_table
	{
		Because of = () =>
			Try(() => routes.Route(mockContext.Object, null).Await());

		It should_throw_an_exception = () =>
			thrown.Should().BeOfType<ArgumentNullException>();
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_a_registered_handler : with_the_routing_table
	{
		Establish context = () =>
			routes.Add(new GenericHandler<string>(x => received = x));

		Because of = () =>
			handled = routes.Route(mockContext.Object, "Hello, World!").Await();

		It should_pass_the_message_to_the_handler = () =>
			received.Should().Be("Hello, World!");

		It should_indicate_the_message_was_handled_by_the_handler = () =>
			handled.Should().Be(1);

		static object received;
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_the_registerd_handler_throws_an_AbortCurrentHandlerException : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new GenericHandler<string>(x =>
			{
				throw new AbortCurrentHandlerException("Reason");
			}));
			routes.Add(new StringHandler());
		};

		Because of = () =>
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_suppress_the_thrown_exception = () =>
			thrown.Should().BeNull();

		It should_continue_processing_the_existing_message = () =>
			handled.Should().Be(2);

		class StringHandler : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
			    return Task.FromResult(true);
			}
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_a_callback_handler_throws_an_AbortCurrentHandlerException : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(x => new GenericHandler<string>(message =>
			{
				throw new AbortCurrentHandlerException("Reason");
			}));

			routes.Add(new StringHandler());
		};

		Because of = () =>
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_suppress_the_thrown_exception = () =>
			thrown.Should().BeNull();

		It should_continue_processing_the_existing_message = () =>
			handled.Should().Be(2);

		class StringHandler : IMessageHandler<string>
		{
		    public Task HandleAsync(string message)
		    {
		        return Task.FromResult(true);
		    }
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_the_registered_handler_throws_an_exception : with_the_routing_table
	{
		Establish context = () => routes.Add(new GenericHandler<string>(x =>
		{
			throw toThrow;
		}));

		Because of = () =>
			Try(() => routes.Route(mockContext.Object, string.Empty).Await());

		It should_not_suppress_the_thrown_exception = () =>
			thrown.Should().Be(toThrow);

		static readonly Exception toThrow = new Exception("my exception");
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_the_same_message_to_a_duplicate_handler_registration : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new StringHandler());
			routes.Add(new GenericHandler<string>());
			routes.Add(new GenericHandler<string>(x => last = true));
		};

		Because of = () =>
			handled = routes.Route(mockContext.Object, "last registration only").Await();

		It should_route_to_any_unique_registrations = () =>
			unique.Should().BeTrue();

		It should_NOT_route_to_the_first_duplicate_registration = () =>
			count.Should().Be(0);

		It should_route_to_most_recent_duplicate_registration = () =>
			last.Should().BeTrue();

		It should_only_route_to_unique_registrations = () =>
			handled.Should().Be(2);

		static bool unique;
		static bool last;

		class StringHandler : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				unique = true;
			    return Task.FromResult(true);
			}
		}
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
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_the_first_registration_first = () =>
			first.Should().BeTrue();

		It should_route_to_the_next_registration_last = () =>
			last.Should().BeTrue();

		It should_indicate_that_multiple_handlers_handled_the_message = () =>
			handled.Should().Be(2);

		static bool first;
		static bool last;

		class SecondHandler : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				if (first)
					last = true;
                return Task.FromResult(true);
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
			routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_the_lowest_sequence_first = () =>
			first.Should().BeTrue();

		It should_route_to_the_highest_sequence_last = () =>
			last.Should().BeTrue();

		static bool first;
		static bool last;

		class RegisteredFirstButRunLast : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				if (first) last = true;
                return Task.FromResult(true);
			}
		}
		class RegisteredLastButRunFirst : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				if (!last) first = true;
                return Task.FromResult(true);
			}
		}
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_with_registered_handlers_of_various_message_types : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(new GenericHandler<string>(x => matched = true));
			routes.Add(new GenericHandler<int>()); // default action of incrementing count should not occur
		};

		Because of = () =>
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_the_handler_that_matches_the_message_type = () =>
			matched.Should().BeTrue();

		It should_NOT_route_to_the_handler_that_does_not_match_the_message_type = () =>
			count.Should().Be(0);

		It should_indicate_that_only_the_matching_handler_handled_the_message = () =>
			handled.Should().Be(1);

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
			handled = routes.Route(mockContext.Object, "Hello, World!").Await();

		It should_provide_the_handler_context = () =>
			receivedContext.Should().Be(mockContext.Object);

		It should_provide_the_message_to_the_registered_callback_handler = () =>
			receivedMessage.Should().Be("Hello, World!");

		It should_indicate_that_the_registered_callback_handler_handled_the_message = () =>
			handled.Should().Be(1);

		static IHandlerContext receivedContext;
		static string receivedMessage;
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_a_callback_which_returns_no_handler : with_the_routing_table
	{
		Establish context = () =>
			routes.Add(ctx => (IMessageHandler<string>)null);

		Because of = () =>
			Try(() => handled = routes.Route(mockContext.Object, string.Empty).Await());

		It should_not_throw_an_exception = () =>
			thrown.Should().BeNull();

		It should_indicate_that_there_were_no_handlers_which_handled_the_message = () =>
			handled.Should().Be(0);
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_to_the_callback_which_throws_an_exception : with_the_routing_table
	{
		Establish context = () => routes.Add(ctx => new GenericHandler<string>(msg =>
		{
			throw toThrow;
		}));

		Because of = () =>
			Try(() => routes.Route(mockContext.Object, string.Empty).Await());

		It should_not_suppress_the_thrown_exception = () =>
			thrown.Should().Be(toThrow);

		static readonly Exception toThrow = new Exception("my exception");
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_the_same_message_to_a_duplicate_callback_registration : with_the_routing_table
	{
		Establish context = () =>
		{
			routes.Add(context => new StringHandler(), 1, typeof(StringHandler));
			routes.Add(GetHandler<string>, 1, typeof(GenericHandler<string>));
			routes.Add(context => new GenericHandler<string>(x => last = true), 1, typeof(GenericHandler<string>));
		};
		static IMessageHandler<T> GetHandler<T>(IHandlerContext context)
		{
			return new GenericHandler<T>();
		}

		Cleanup after = () => GetHandler<int>(null); // code coverage

		Because of = () =>
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_any_unique_registrations = () =>
			unique.Should().BeTrue();

		It should_NOT_route_to_the_first_duplicate_registration = () =>
			count.Should().Be(0);

		It should_route_to_the_last_duplicate_registration = () =>
			last.Should().BeTrue();

		It should_indicate_that_the_message_was_handled_by_unique_handler_registrations_only = () =>
			handled.Should().Be(2);

		static bool unique;
		static bool last;

		class StringHandler : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				unique = true;
			    return Task.FromResult(true);
			}
		}
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
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_the_first_registration_first = () =>
			first.Should().BeTrue();

		It should_route_to_the_next_registration_last = () =>
			last.Should().BeTrue();

		It should_indicate_that_the_message_was_handled_by_each_registration = () =>
			handled.Should().Be(2);

		static bool first;
		static bool last;

		class SecondHandler : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				if (first)
					last = true;
			    return Task.FromResult(true);
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
			routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_the_lowest_sequence_first = () =>
			first.Should().BeTrue();

		It should_route_to_the_highest_sequence_last = () =>
			last.Should().BeTrue();

		static bool first;
		static bool last;

		class RegisteredFirstButRunLast : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				if (first) last = true;
			    return Task.FromResult(true);
			}
		}
		class RegisteredLastButRunFirst : IMessageHandler<string>
		{
			public Task HandleAsync(string message)
			{
				if (!last) first = true;
			    return Task.FromResult(true);
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
			handled = routes.Route(mockContext.Object, string.Empty).Await();

		It should_route_to_the_handler_that_matches_the_message_type = () =>
			values.ContainsKey(typeof(string)).Should().BeTrue();

		It should_NOT_route_to_the_handler_that_does_not_match_the_message_type = () =>
			values.ContainsKey(typeof(int)).Should().BeFalse();

		It should_indicate_that_the_message_was_handled_by_the_approriate_registered_callback_handlers = () =>
			handled.Should().Be(1);

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
			routes.Route(mockContext.Object, string.Empty).Await();

		It should_only_dispatch_to_handlers_while_the_context_can_continue_handling = () =>
			count.Should().Be(2);
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_routing_a_message_with_no_registered_handlers : with_the_routing_table
	{
		Because of = () =>
			handled = routes.Route(mockContext.Object, "Hello, World!").Await();

		It should_indicate_that_no_handlers_handled_the_message = () =>
			handled.Should().Be(0);
	}

	[Subject(typeof(DefaultRoutingTable))]
	public class when_adding_a_route_using_a_dependency_resolver : with_the_routing_table
	{
		Establish context = () =>
		{
			mockResolver = new Mock<IDependencyResolver>();
			mockResolver.Setup(x => x.As<IDisposable>()).Returns(actualResolver);
			mockContext.Setup(x => x.CurrentResolver).Returns(mockResolver.Object);

			routes.Add<IDisposable, string>(x =>
			{
				resolverAfterCallback = x;
				return new GenericHandler<string>(msg => receivedMessage = msg);
			});
		};

		Because of = () =>
			handled = routes.Route(mockContext.Object, "Hello, World!").Await();

		It should_provide_the_dependency_resolver = () =>
			resolverAfterCallback.Should().Be(actualResolver);

		It should_provide_the_message_to_the_registered_callback_handler = () =>
			receivedMessage.Should().Be("Hello, World!");

		It should_indicate_that_the_registered_callback_handler_handled_the_message = () =>
			handled.Should().Be(1);

		static readonly IDisposable actualResolver = new Mock<IDisposable>().Object;
		static Mock<IDependencyResolver> mockResolver;
		static IDisposable resolverAfterCallback;
		static string receivedMessage;
	}

	public abstract class with_the_routing_table
	{
		Establish context = () =>
		{
			routes = new DefaultRoutingTable();
			mockContext = new Mock<IHandlerContext>();

			mockContext.Setup(x => x.ContinueHandling).Returns(true);

			count = handled = 0;
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
		protected static int handled;

		protected class GenericHandler<T> : IMessageHandler<T>
		{
			public Task HandleAsync(T message)
			{
				this.callback(message);
			    return Task.FromResult(true);
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
#pragma warning restore 169, 414