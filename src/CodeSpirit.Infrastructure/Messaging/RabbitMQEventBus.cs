using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CodeSpirit.Infrastructure.Messaging;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, SubscriptionInfo> _subscriptions = new();
    private IConnection? _connection;
    private IChannel? _channel;
    private volatile bool _disposed;

    public RabbitMQEventBus(IOptions<RabbitMQOptions> options, ILogger<RabbitMQEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, string routingKey = "") where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQEventBus));

        var eventName = typeof(T).Name;
        var effectiveRoutingKey = string.IsNullOrEmpty(routingKey) ? eventName : routingKey;

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var channel = await EnsureChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: effectiveRoutingKey,
            body: body);

        _logger.LogDebug("Published {EventName} with routing key {RoutingKey}", eventName, effectiveRoutingKey);
    }

    public void Subscribe<T>(string queueName, Func<T, Task> handler, string routingKey = "") where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQEventBus));

        var eventName = typeof(T).Name;
        var effectiveRoutingKey = string.IsNullOrEmpty(routingKey) ? eventName : routingKey;

        var subscription = new SubscriptionInfo(
            QueueName: queueName,
            EventType: typeof(T),
            RoutingKey: effectiveRoutingKey,
            Handler: async (rawMessage) =>
            {
                var message = JsonSerializer.Deserialize<T>(rawMessage);
                if (message is not null)
                {
                    await handler(message);
                }
            });

        if (!_subscriptions.TryAdd(queueName, subscription))
        {
            _logger.LogWarning("Subscription for queue {QueueName} already exists, replacing", queueName);
            _subscriptions[queueName] = subscription;
        }

        StartConsumer(subscription);
    }

    public void Unsubscribe(string queueName)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQEventBus));

        if (_subscriptions.TryRemove(queueName, out var subscription))
        {
            subscription.CancellationTokenSource.Cancel();
            _logger.LogInformation("Unsubscribed queue {QueueName}", queueName);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var (_, subscription) in _subscriptions)
        {
            subscription.CancellationTokenSource.Cancel();
            subscription.CancellationTokenSource.Dispose();
        }

        _subscriptions.Clear();

        _channel?.Dispose();
        _connection?.Dispose();
        _connectionLock.Dispose();

        _logger.LogInformation("RabbitMQEventBus disposed");
    }

    private async Task<IChannel> EnsureChannelAsync()
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _connectionLock.WaitAsync();
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            await ConnectAsync();

            return _channel!;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ConnectAsync()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = await factory.CreateConnectionAsync();

        _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
        _connection.RecoverySucceededAsync += OnRecoverySucceededAsync;

        _channel = await _connection.CreateChannelAsync();

        _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _options.HostName, _options.Port);
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText} (code: {ReplyCode})", e.ReplyText, e.ReplyCode);
        return Task.CompletedTask;
    }

    private async Task OnRecoverySucceededAsync(object sender, AsyncEventArgs e)
    {
        _logger.LogInformation("RabbitMQ connection recovered, re-subscribing consumers");

        foreach (var (_, subscription) in _subscriptions)
        {
            await StartConsumerOnChannelAsync(_channel!, subscription);
        }
    }

    private void StartConsumer(SubscriptionInfo subscription)
    {
        if (_channel is { IsOpen: true })
        {
            StartConsumerOnChannelAsync(_channel, subscription).GetAwaiter().GetResult();
            return;
        }

        EnsureChannelAsync().ContinueWith(async task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                var channel = task.Result;
                await StartConsumerOnChannelAsync(channel, subscription);
            }
            else
            {
                _logger.LogError(task.Exception, "Failed to connect for consumer {QueueName}", subscription.QueueName);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task StartConsumerOnChannelAsync(IChannel channel, SubscriptionInfo subscription)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await channel.QueueDeclareAsync(
            queue: subscription.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: subscription.QueueName,
            exchange: _options.ExchangeName,
            routingKey: subscription.RoutingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageText = Encoding.UTF8.GetString(body);

            try
            {
                await subscription.Handler(messageText);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message on queue {QueueName}", subscription.QueueName);
                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        subscription.ConsumerTag = await channel.BasicConsumeAsync(
            queue: subscription.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: subscription.CancellationTokenSource.Token);

        _logger.LogInformation("Started consuming queue {QueueName} with routing {RoutingKey}", subscription.QueueName, subscription.RoutingKey);
    }

    private sealed record SubscriptionInfo(
        string QueueName,
        Type EventType,
        string RoutingKey,
        Func<string, Task> Handler)
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new();
        public string? ConsumerTag { get; set; }
    }
}
