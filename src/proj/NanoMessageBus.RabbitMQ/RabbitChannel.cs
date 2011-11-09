////namespace NanoMessageBus.RabbitMQ
////{
////    using System;
////    using System.Collections.Generic;
////    using System.IO;
////    using System.Linq;
////    using System.Runtime.Serialization;
////    using Endpoints;
////    using Serialization;

////    public class RabbitChannel : ISendToEndpoints, IReceiveFromEndpoints
////    {
////        //// TODO: logging

////        public Uri EndpointAddress { get; private set; }

////        public void Send(EnvelopeMessage message, params Uri[] recipients)
////        {
////            recipients = recipients ?? new Uri[0];
////            if (recipients.Length == 0)
////                return;

////            var connector = this.connectorFactory(); // TODO: catch connection errors
////            var serializer = this.serializerFactory(string.Empty);

////            var pending = new RabbitMessage
////            {
////                MessageId = message.MessageId,
////                ProducerId = string.Empty, // TODO?
////                CorrelationId = string.Empty, // TODO?
////                ContentEncoding = string.Empty, // TODO
////                ContentType = serializer.ContentType,
////                Durable = message.Persistent,
////                Expiration = message.Expiration(),
////                MessageType = message.MessageType(),
////                ReplyTo = this.EndpointAddress.ToString(), // TODO
////                RoutingKey = message.RoutingKey(),
////                UserId = string.Empty, // TODO?
////                Headers = message.Headers,
////                Body = message.Serialize(serializer),
////            };

////            foreach (var address in recipients.Select(x => new RabbitAddress(x)))
////                connector.Send(pending, address);
////        }

////        public EnvelopeMessage Receive()
////        {
////            // TODO: catch connection errors
////            var connector = this.connectorFactory();
////            var message = connector.Receive(DefaultReceiveWait);

////            if (this.ForwardWhenExpired(message))
////                return null;

////            var logicalMessages = this.TryDeserialize(message);
////            if (logicalMessages == null)
////                return null; // message cannot be deserialized

////            // TODO: reply-to address and TTL
////            return new EnvelopeMessage(
////                message.MessageId,
////                null,
////                TimeSpan.MaxValue,
////                message.Durable,
////                message.Headers,
////                logicalMessages);
////        }
////        private ICollection<object> TryDeserialize(RabbitMessage message)
////        {
////            try
////            {
////                return (ICollection<object>)this.Deserialize(message);
////            }
////            catch (SerializationException e)
////            {
////                this.ForwardWhenPoison(message, e);
////            }
////            catch (InvalidCastException e)
////            {
////                this.ForwardWhenPoison(message, e);
////            }

////            return null;
////        }
////        private object Deserialize(RabbitMessage message)
////        {
////            var serializer = this.serializerFactory(message.ContentType);
////            using (var stream = new MemoryStream(message.Body))
////                return serializer.Deserialize(stream);
////        }

////        private bool ForwardWhenExpired(RabbitMessage message)
////        {
////            if (message.Expiration >= SystemTime.UtcNow)
////                return false;

////            // TODO: forward to dead letter exchange
////            return true;
////        }
////        private void ForwardWhenPoison(RabbitMessage message, Exception exception)
////        {
////            // TODO: forward to poison messager exchange
////        }

////        public RabbitChannel(
////            Uri localAddress,
////            Func<RabbitConnector> connectorFactory,
////            Func<string, ISerializer> serializerFactory)
////        {
////            this.EndpointAddress = localAddress;
////            this.connectorFactory = connectorFactory;
////            this.serializerFactory = serializerFactory;
////        }
////        ~RabbitChannel()
////        {
////            this.Dispose(false);
////        }

////        public void Dispose()
////        {
////            this.Dispose(true);
////            GC.SuppressFinalize(this);
////        }
////        protected virtual void Dispose(bool disposing)
////        {
////        }

////        private static readonly TimeSpan DefaultReceiveWait = TimeSpan.FromMilliseconds(500);
////        private readonly Func<RabbitConnector> connectorFactory;
////        private readonly Func<string, ISerializer> serializerFactory;
////    }
////}