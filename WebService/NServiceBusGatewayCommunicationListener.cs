namespace WebService
{
    using System;
    using System.Fabric;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using NServiceBus;
    using NServiceBus.Features;
    using Owin;

    internal class NServiceBusGatewayCommunicationListener : ICommunicationListener
    {
        private readonly ServiceEventSource eventSource;
        private readonly ServiceContext serviceContext;
        private readonly string endpointName;
        private readonly string appRoot;

        private string publishAddress;
        private string listeningAddress;
        private IEndpointInstance endpoint;

        public NServiceBusGatewayCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, ServiceEventSource eventSource, string endpointName)
            : this(startup, serviceContext, eventSource, endpointName, null)
        {
        }

        public NServiceBusGatewayCommunicationListener(Action<IAppBuilder> startup, ServiceContext serviceContext, ServiceEventSource eventSource, string endpointName, string appRoot)
        {
            if (startup == null)
            {
                throw new ArgumentNullException(nameof(startup));
            }

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            this.serviceContext = serviceContext;
            this.endpointName = endpointName;
            this.eventSource = eventSource;
            this.appRoot = appRoot;
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint(this.endpointName);
            var protocol = serviceEndpoint.Protocol;
            int port = serviceEndpoint.Port;

            if (!(this.serviceContext is StatelessServiceContext))
            {
                throw new InvalidOperationException("Should only be used with Stateless services.");
            }

            this.listeningAddress = string.Format(
                CultureInfo.InvariantCulture,
                "{0}://+:{1}/{2}",
                protocol,
                port,
                string.IsNullOrWhiteSpace(this.appRoot)
                    ? string.Empty
                    : this.appRoot.TrimEnd('/') + '/');

            this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            try
            {
                this.eventSource.Message("Starting endpoint");

                var endpointConfiguration = new EndpointConfiguration("webservice");
                endpointConfiguration.EnableFeature<Gateway>();
                endpointConfiguration.UsePersistence<InMemoryPersistence>();
                endpointConfiguration.SendFailedMessagesTo("error");

                endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

                this.eventSource.Message("Started");

                return this.publishAddress;
            }
            catch (Exception ex)
            {
                this.eventSource.Message("Failed to start an endpoint. {0}", ex.ToString());

                await this.StopEndpointAsync().ConfigureAwait(false);

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.eventSource.Message("Closing endpoint");

            return this.StopEndpointAsync();
        }

        public void Abort()
        {
            this.eventSource.Message("Aborting endpoint");

            this.StopEndpointAsync().GetAwaiter().GetResult();
        }

        private Task StopEndpointAsync()
        {
            if (this.endpoint != null)
            {
                return this.endpoint.Stop();
            }

            return Task.CompletedTask;
        }
    }
}