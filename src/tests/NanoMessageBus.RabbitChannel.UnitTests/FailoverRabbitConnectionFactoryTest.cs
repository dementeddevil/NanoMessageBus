#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security.Cryptography.X509Certificates;
	using Machine.Specifications;
	using Moq;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Exceptions;
	using It = Machine.Specifications.It;

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_a_null_endpoint : using_the_failover_connection_factory
	{
		Because of = () =>
			Try(() => factory.AddEndpoint(null));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_an_endpoint_for_the_first_time : using_the_failover_connection_factory
	{
		Because of = () =>
			added = factory.AddEndpoint(DefaultEndpoint);

		It should_indicate_the_endpoint_was_successfully_added = () =>
			added.ShouldBeTrue();

		It should_add_the_endpoint_to_the_collection = () =>
			factory.Endpoints.Count().ShouldEqual(1);

		static bool added;
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_an_endpoint_multiple_times : using_the_failover_connection_factory
	{
		Establish context = () =>
			Add(DefaultEndpoint);

		Because of = () =>
			Add(DefaultEndpoint);

		It should_add_the_first_occurrence = () =>
			added.ShouldEqual(1);

		It should_NOT_add_the_endpoint_to_the_collection = () =>
			factory.Endpoints.Count().ShouldEqual(1);

		static void Add(Uri endpoint)
		{
			if (factory.AddEndpoint(endpoint))
				added++;
		}

		static int added;
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_an_endpoint_with_authentication_information : using_the_failover_connection_factory
	{
		Because of = () =>
			factory.AddEndpoint(new Uri("amqp://new-user:new-pass@localhost:5672/"));

		It should_set_overwrite_the_connection_username_with_the_endpoint_username = () =>
			factory.UserName.ShouldEqual("new-user");

		It should_set_overwrite_the_connection_password_with_the_endpoint_password = () =>
			factory.Password.ShouldEqual("new-pass");
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_an_endpoint_with_no_authentication_information : using_the_failover_connection_factory
	{
		Establish context = () =>
		{
			factory.UserName = "keep";
			factory.Password = "this";
		};

		Because of = () =>
			factory.AddEndpoint(new Uri("amqp://localhost:5672/"));

		It should_leave_the_existing_connection_username_alone = () =>
			factory.UserName.ShouldEqual("keep");

		It should_leave_the_existing_connection_password_alone = () =>
			factory.Password.ShouldEqual("this");
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_multiple_endpoints_as_a_collection : using_the_failover_connection_factory
	{
		Because of = () =>
			factory.AddEndpoints(Endpoints);

		It should_add_each_non_null_endpoint_to_the_collection = () =>
			factory.Endpoints.ShouldEqual(Endpoints.Where(x => x != null));

		static readonly IEnumerable<Uri> Endpoints = new[]
		{
			new Uri("amqp://machine-a"), 
			null,
			new Uri("amqp://machine-b"),
			null
		};
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_multiple_endpoints_as_a_set_of_parameters : using_the_failover_connection_factory
	{
		Because of = () =>
			factory.AddEndpoints(Endpoints[0], Endpoints[1], Endpoints[2], Endpoints[3]);

		It should_add_each_non_null_endpoint_to_the_collection = () =>
			factory.Endpoints.ShouldEqual(Endpoints.Where(x => x != null));

		static readonly IList<Uri> Endpoints = new[]
		{
			new Uri("amqp://machine-a"), 
			null,
			new Uri("amqp://machine-b"),
			null
		};
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_adding_a_string_representation_of_multiple_endpoints : using_the_failover_connection_factory
	{
		Because of = () =>
			factory.AddEndpoints("amqp://machine-a|||||||||amqp://machine-b|||amqp://machine-c");

		It should_add_split_the_string_on_pipe_characters = () =>
			factory.Endpoints.ShouldNotBeEmpty();

		It should_not_add_empty_elements_to_the_endpoint_collection = () =>
			factory.Endpoints.Count().ShouldEqual(3);
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_randomizing_the_set_of_endpoints : using_the_failover_connection_factory
	{
		Establish context = () =>
			factory.AddEndpoints(Endpoints);

		Because of = () =>
			factory.RandomizeEndpoints();

		It should_reorder_the_endpoints = () =>
			factory.Endpoints.SequenceEqual(Endpoints).ShouldBeFalse();

		static readonly IList<Uri> Endpoints = new[]
		{
			new Uri("amqp://machine-a"), 
			new Uri("amqp://machine-b"),
			new Uri("amqp://machine-c"),
			new Uri("amqp://machine-d")
		};
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_establishing_a_connection : using_the_failover_connection_factory
	{
		Establish context = () =>
		{
			factory = new FailoverFactoryStub();
			factory.AddEndpoints(Endpoints);
		};

		Because of = () =>
			factory.CreateConnection(42);

		It should_provide_the_max_redirect_value_to_the_base_class = () =>
			((FailoverFactoryStub)factory).MaxRedirects.ShouldEqual(42);

		It should_provide_each_endpoint_to_the_underlying_connection = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.Select(x => x.HostName)
				.SequenceEqual(Endpoints.Select(x => x.Host)).ShouldBeTrue();

		static readonly IList<Uri> Endpoints = new[]
		{
			new Uri("amqp://machine-a"), 
			new Uri("amqp://machine-b"),
			new Uri("amqp://machine-c"),
			new Uri("amqp://machine-d")
		};
		static int resolutionCount;
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_establishing_without_any_configured_endpoints : using_the_failover_connection_factory
	{
		Establish context = () =>
			factory = new FailoverFactoryStub();

		Because of = () =>
			factory.CreateConnection(42);

		It should_use_the_default_endpoint = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.Single().HostName.ShouldEqual("localhost");
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_establishing_a_connection_fails : using_the_failover_connection_factory
	{
		Establish context = () =>
			factory = new FailoverFactoryStub(false);

		Because of = () =>
			Try(() => factory.CreateConnection(42));

		It should_throw_an_exception = () =>
			thrown.ShouldBeOfType<BrokerUnreachableException>();
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_establishing_an_ssl_connection_using_a_certificate_file : using_the_failover_connection_factory
	{
		Establish context = () =>
		{
			factory = new FailoverFactoryStub();
			factory.AddEndpoint(Endpoint);
		};

		Because of = () =>
			factory.CreateConnection(0);

		It should_indicate_that_ssl_is_enabled = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.Enabled.ShouldBeTrue();

		It should_attempt_to_load_the_certificate_from_the_File = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.CertPath.ShouldEqual("c:/mycert.cer");

		static readonly Uri Endpoint = new Uri("amqps://localhost/?cert-path=c:/mycert.cer", UriKind.Absolute);
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_establishing_an_ssl_connection_using_a_certificate_file_and_passphrase : using_the_failover_connection_factory
	{
		Establish context = () =>
		{
			factory = new FailoverFactoryStub();
			factory.AddEndpoint(Endpoint);
		};

		Because of = () =>
			factory.CreateConnection(0);

		It should_indicate_that_ssl_is_enabled = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.Enabled.ShouldBeTrue();

		It should_attempt_to_load_the_certificate_from_the_file_specified = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.CertPath.ShouldEqual("c:/mycert.cer");

		It should_use_the_passphrase_specified = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.CertPassphrase.ShouldEqual("1234");

		static readonly Uri Endpoint = new Uri("amqps://localhost/?cert-path=c:/mycert.cer&cert-passphrase=1234", UriKind.Absolute);
	}

	[Subject(typeof(FailoverRabbitConnectionFactory))]
	public class when_establishing_an_ssl_connection_using_a_certificate_thumbprint : using_the_failover_connection_factory
	{
		Establish context = () =>
		{
			certificates = new Mock<CertificateStore>();
			certificates
				.Setup(x => x.Resolve("my-cert-thumbprint", "My", "CurrentUser"))
				.Returns(new Mock<X509Certificate>().Object);

			factory = new FailoverFactoryStub(true, certificates.Object);
			factory.AddEndpoint(Endpoint);
		};

		Because of = () =>
			factory.CreateConnection(0);

		It should_indicate_that_ssl_is_enabled = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.Enabled.ShouldBeTrue();

		It should_attempt_to_load_the_certificate_from_the_file_specified = () =>
			((FailoverFactoryStub)factory).ConnectedEndpoints.First().Ssl.Certs.Count.ShouldEqual(1);

		static readonly Uri Endpoint = new Uri("amqps://localhost/?cert-id=my-cert-thumbprint", UriKind.Absolute);
		static Mock<CertificateStore> certificates;
	}

	public abstract class using_the_failover_connection_factory
	{
		Establish context = () =>
			factory = new FailoverRabbitConnectionFactory();

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected static readonly Uri DefaultEndpoint = new Uri("amqp://guest:guest@localhost:5672/");
		protected static FailoverRabbitConnectionFactory factory;
		protected static Exception thrown;
	}

	internal class FailoverFactoryStub : FailoverRabbitConnectionFactory
	{
		public int MaxRedirects { get; private set; }
		public IList<AmqpTcpEndpoint> ConnectedEndpoints { get; private set; }
		private bool CanConnect { get; set; }

		protected override IConnection CreateConnection(
			int maxRedirects,
			IDictionary connectionAttempts,
			IDictionary connectionErrors,
			params AmqpTcpEndpoint[] endpoints)
		{
			this.MaxRedirects = maxRedirects;
			this.ConnectedEndpoints = endpoints;
			return this.CanConnect ? new Mock<IConnection>().Object : null;
		}

		public FailoverFactoryStub(bool canConnect = true, CertificateStore certificates = null)
			: base(certificates)
		{
			this.CanConnect = canConnect;
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169