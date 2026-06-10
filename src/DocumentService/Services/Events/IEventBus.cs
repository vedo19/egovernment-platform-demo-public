namespace DocumentService.Services.Events;

public interface IEventBus
{
    void Publish(string routingKey, object payload);
}
