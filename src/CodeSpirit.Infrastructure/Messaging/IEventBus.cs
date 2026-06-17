namespace CodeSpirit.Infrastructure.Messaging;

public interface IEventBus
{
    Task PublishAsync<T>(T message, string routingKey = "") where T : class;

    void Subscribe<T>(string queueName, Func<T, Task> handler, string routingKey = "") where T : class;

    void Unsubscribe(string queueName);
}
