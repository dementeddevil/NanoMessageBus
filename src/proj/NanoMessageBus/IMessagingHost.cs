﻿namespace NanoMessageBus
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading.Tasks;
    /// <summary>
    /// Represents the primary, high-level interface for working with sending and receiving messages.
    /// </summary>
    /// <remarks>
    /// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IMessagingHost : IDisposable
	{
		/// <summary>
		/// Creates all channel groups and the initializes each of them and returns the first one.
		/// </summary>
		/// <exception cref="ConfigurationErrorsException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns>The first or primary channel group initialized.</returns>
		IChannelGroup Initialize();

		/// <summary>
		/// Begins streaming any available inbound messages to the callback provided.
		/// </summary>
		/// <param name="callback">The callback to which any received messages should be dispatched.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		void BeginReceive(Func<IDeliveryContext, Task> callback);

		/// <summary>
		/// Obtains a reference to the channel group for the key specified.
		/// </summary>
		/// <param name="channelGroup">The key of the channel group.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns>A reference to the outbound-based method of the desired channel group.</returns>
		IChannelGroup this[string channelGroup] { get; }
	}
}