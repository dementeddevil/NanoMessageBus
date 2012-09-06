#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NanoMessageBus.Channels
{
	using System;
	using System.Security.Cryptography.X509Certificates;
	using Machine.Specifications;

	[Subject(typeof(CertificateStore))]
	public class when_resolving_a_certificate : using_a_certificate_store
	{
		Because of = () =>
			resolved = store.Resolve(KnownFingerprint, "Root");

		It should_return_the_desired_certificate = () =>
			resolved.Subject.ShouldEqual("CN=Microsoft Root Authority, OU=Microsoft Corporation, OU=Copyright (c) 1997 Microsoft Corp.");
	}

	[Subject(typeof(CertificateStore))]
	public class when_resolving_a_certificate_with_lowercase_and_spaced_hex_characters : using_a_certificate_store
	{
		Because of = () =>
			resolved = store.Resolve(KnownFingerprint, "Root");

		It should_return_the_desired_certificate = () =>
			resolved.Subject.ShouldEqual("CN=Microsoft Root Authority, OU=Microsoft Corporation, OU=Copyright (c) 1997 Microsoft Corp.");
	}

	[Subject(typeof(CertificateStore))]
	public class when_resolving_a_certificate_without_a_fingerprint : using_a_certificate_store
	{
		Because of = () =>
			resolved = store.Resolve(null);

		It should_not_return_a_certificate = () =>
			resolved.ShouldBeNull();
	}

	[Subject(typeof(CertificateStore))]
	public class when_resolving_a_certificate_without_a_nonexistent_fingerprint : using_a_certificate_store
	{
		Because of = () =>
			resolved = store.Resolve("Bad fingerprint");

		It should_not_return_a_certificate = () =>
			resolved.ShouldBeNull();
	}

	[Subject(typeof(CertificateStore))]
	public class when_resolving_a_certificate_with_a_nonexistent_store_location : using_a_certificate_store
	{
		Because of = () =>
			resolved = store.Resolve(KnownFingerprint, "My", "Bad Location");

		It should_not_return_a_certificate = () =>
			resolved.ShouldBeNull();
	}

	[Subject(typeof(CertificateStore))]
	public class when_resolving_a_certificate_with_a_nonexistent_store_name : using_a_certificate_store
	{
		Because of = () =>
			resolved = store.Resolve(KnownFingerprint, "Bad Store Name");

		It should_not_return_a_certificate = () =>
			resolved.ShouldBeNull();
	}

	public abstract class using_a_certificate_store
	{
		Establish context = () =>
		{
			store = new CertificateStore();
			resolved = null;
			thrown = null;
		};

		protected static void Try(Action callback)
		{
			thrown = Catch.Exception(callback);
		}

		protected const string KnownFingerprint = "A43489159A520F0D93D032CCAF37E7FE20A8B419";
		protected static CertificateStore store;
		protected static X509Certificate resolved;
		protected static Exception thrown;
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169