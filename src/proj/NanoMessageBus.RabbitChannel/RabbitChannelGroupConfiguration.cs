﻿namespace NanoMessageBus.RabbitChannel
{
	using System;
	using RabbitMQ.Client;

	public class RabbitChannelGroupConfiguration : IChannelConfiguration
	{
		public virtual RabbitChannelGroupConfiguration InitializeConnection(IConnection connection)
		{
			return this;
		}
		public virtual RabbitChannelGroupConfiguration ConfigureChannel(IModel channel)
		{
			return this;
		}

		public virtual string ChannelGroup
		{
			get { return null; }
		}
		public virtual string InputQueue
		{
			get { return null; }
		}
		public virtual string PrivateExchange
		{
			get { return null; }
		}
		public virtual bool DispatchOnly
		{
			get { return false; }
		}
		public virtual int MinThreads
		{
			get { return 0; }
		}
		public virtual int MaxThreads
		{
			get { return 0; }
		}
		public virtual TimeSpan ReceiveTimeout
		{
			get { return TimeSpan.Zero; }
		}
		public virtual RabbitTransactionType TransactionType
		{
			get { return RabbitTransactionType.None; }
		}
		public virtual int ChannelBuffer
		{
			get { return 0; }
		}
	}
}