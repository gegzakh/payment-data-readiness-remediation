namespace PDR.BuildingBlocks.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>When false the service runs without RabbitMQ (unit tests, local minimal runs).</summary>
    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = "localhost";

    public ushort Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    /// <summary>Prefix for queue/exchange names, e.g. <c>pdr</c>.</summary>
    public string Prefix { get; set; } = "pdr";

    public int PrefetchCount { get; set; } = 16;

    public int RetryCount { get; set; } = 3;

    public int RetryIntervalSeconds { get; set; } = 5;

    public int OutboxPollIntervalSeconds { get; set; } = 5;

    public int OutboxBatchSize { get; set; } = 100;

    public int OutboxMaxAttempts { get; set; } = 10;
}
