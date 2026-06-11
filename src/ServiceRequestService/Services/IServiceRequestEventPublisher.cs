using ServiceRequestService.Models;

namespace ServiceRequestService.Services;

public interface IServiceRequestEventPublisher
{
    Task PublishSubmittedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    Task PublishAssignedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    Task PublishDocumentsRequestedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    Task PublishDocumentsRejectedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    Task PublishApprovedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
    Task PublishRejectedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default);
}
