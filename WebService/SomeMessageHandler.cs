using NServiceBus;

namespace WebService
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Logging;

    public class SomeMessageHandler : IHandleMessages<SomeMessage>
    {
        static ILog log = LogManager.GetLogger<SomeMessageHandler>();

        public Task Handle(SomeMessage message, IMessageHandlerContext context)
        {
            log.Info("received SomeMessage!");
            return Task.CompletedTask;
        }
    }
}

public class SomeMessage : IMessage
{
}
