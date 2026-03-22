using RabbitMQ.Client;

namespace MIBO.ActionService.RetryPolicy;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string ConnectionString { get; set; } = string.Empty;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExecuteExchange { get; set; } = "integration.execute";
    public string RetryExchange { get; set; } = "integration.retry";
    public string DeadLetterExchange { get; set; } = "integration.deadletter";
    public string ExecuteQueue { get; set; } = "integration.execute";
    public string RetryQueue { get; set; } = "integration.retry";
    public string DeadLetterQueue { get; set; } = "integration.deadletter";
    public bool UseSsl { get; set; }

    public ConnectionFactory CreateConnectionFactory()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return new ConnectionFactory
            {
                Uri = new Uri(ConnectionString, UriKind.Absolute),
                DispatchConsumersAsync = true,
            };
        }

        return new ConnectionFactory
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            VirtualHost = VirtualHost,
            DispatchConsumersAsync = true,
            Ssl = new SslOption
            {
                Enabled = UseSsl,
            },
        };
    }
}
