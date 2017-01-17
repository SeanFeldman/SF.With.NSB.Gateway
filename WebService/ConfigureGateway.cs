using System;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;

public class ConfigureGateway : IProvideConfiguration<GatewayConfig>
{
    public GatewayConfig GetConfiguration()
    {
        return new GatewayConfig
        {
            TransactionTimeout = TimeSpan.FromMinutes(10),
            Channels = new ChannelCollection
            {
                new ChannelConfig
                {
                    Address = "http://+:12000/webservice/",
                    ChannelType = "http",
                    Default = true
                }
            }
        };
    }
}