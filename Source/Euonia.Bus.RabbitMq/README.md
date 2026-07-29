## RabbitMQ Transport

This project provides a RabbitMQ transport implementation for the Euonia.Bus library. 

It enables seamless integration with RabbitMQ message broker, allowing you to send and receive messages using RabbitMQ as the underlying transport mechanism.

### Features

- **Easy Integration**: Quickly set up RabbitMQ transport in your Euonia.Bus applications.
- **Reliable Messaging**: Leverage RabbitMQ's robust messaging capabilities for reliable message delivery.
- **Asynchronous Communication**: Support for asynchronous message processing to improve application responsiveness.
- **Configurable**: Flexible configuration options to tailor the transport to your application's needs.
- **Cross-Platform**: Compatible with multiple .NET versions, ensuring broad usability.
- **Open Source**: Built on top of open-source libraries, promoting transparency and community contributions.

### Getting Started

To get started with the RabbitMQ transport in Euonia.Bus, follow these steps:
1. **Install the Package**: Add the Euonia.Bus.RabbitMq package to your project via NuGet.
   ```bash
   dotnet add package Euonia.Bus.RabbitMq
   ```
2. **Configure the Transport**: In your application's configuration file (e.g., appsettings.json), add the RabbitMQ transport settings under the `EuoniaBus` section:
```json
{
  "EuoniaBus": {
    "RabbitMq": {
      "Enabled": true,
      "Name": "RabbitMq",
      "Connection": "amqp://user:password@host:port",
      "ExchangeNamePrefix": "$nerosoft.euonia.exchange",
      "QueueNamePrefix": "$nerosoft.euonia.queue",
      "RoutingKey": "*",
      "Persistent": true,
      "AutoAck": true,
      "Mandatory": true,
      "MaxFailureRetries": 3,
      "SubscriptionId": "my-subscription"
    }
  }
}
```
3. **Add Module Dependency**: Add the RabbitMQ transport module dependency in your service module:
```csharp
[DependsOn(typeof(RabbitMqBusModule))]
public class YourServiceModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure your services here
    }
}
```
4. **Add Incoming/Outgoing Strategy**: Implement the necessary message handling strategies for incoming and outgoing messages.
```csharp
services.AddEuoniaBus(config =>
{
    config.SetStrategy("RabbitMq", builder =>
    {
        builder.Add(new AttributeTransportStrategy(["RabbitMq"]));
        builder.Add<DistributedMessageTransportStrategy>();
        builder.EvaluateOutgoing(e => true);
        builder.EvaluateIncoming(e => true);
    });
});
```
5. **Run Your Application**: Start your application, and it will now use RabbitMQ as the transport for Euonia.Bus.