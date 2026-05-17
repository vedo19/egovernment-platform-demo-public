using ServiceRequestService.Models;

namespace ServiceRequestService.Services;

public interface IServiceRequestEventPublisher
{
    Task PublishCreatedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
}
