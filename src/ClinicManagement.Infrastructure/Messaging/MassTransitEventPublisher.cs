using ClinicManagement.Application.Common.Interfaces;
using MassTransit;

namespace ClinicManagement.Infrastructure.Messaging;

public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        => await _publishEndpoint.Publish(@event, cancellationToken);
}
