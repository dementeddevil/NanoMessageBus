using System;
using System.Threading.Tasks;
using Autofac;

namespace NanoMessageBus
{
    public class AutofacChannelMessageHandler : IMessageHandler<ChannelMessage>
    {
        private readonly IMessageHandler<ChannelMessage> _handler;

        public AutofacChannelMessageHandler(
            IHandlerContext context, IRoutingTable table, IMessageHandler<ChannelMessage> inner = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (table == null)
                throw new ArgumentNullException(nameof(table));

            _handler = inner ?? new DefaultChannelMessageHandler(context, table);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(context).ExternallyOwned(); // single instance for this and descendent scopes
            builder.Update(context.CurrentResolver.As<ILifetimeScope>().ComponentRegistry);
        }

        public virtual Task HandleAsync(ChannelMessage message)
        {
            return _handler.HandleAsync(message);
        }
    }
}