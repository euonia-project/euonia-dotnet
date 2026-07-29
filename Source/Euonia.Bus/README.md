### Overview

Euonia.Bus is a .NET library designed to facilitate message-based communication within applications. It provides a robust and flexible framework for implementing messaging patterns, enabling developers to build scalable and maintainable systems.

### Features

- **Modular Architecture**: Easily extend and customize the messaging infrastructure.
- **Transport Agnostic**: Supports various transport mechanisms (e.g., RabbitMQ, In-Memory) for message delivery.
- **Message Routing**: Advanced routing capabilities to direct messages to the appropriate handlers.
- **Error Handling**: Built-in error handling and retry mechanisms for reliable message processing.
- **Open Source**: Actively developed and maintained as an open-source project.

### Getting Started

To get started with Euonia.Bus, follow these steps:

1. **Install the Package**: Add the Euonia.Bus package to your project via NuGet.
   ```bash
   dotnet add package Euonia.Bus
   ```

2. **Register Services**: In your application's, register the necessary services for the message bus.
   ```csharp
    services.AddEuoniaBus(config =>
    {
        // Configure the message conventions to determine the message types
    	config.SetConventions(builder =>
    	{
    		builder.Add<DefaultMessageConvention>();
    		builder.Add<AttributeMessageConvention>();
    		builder.Add<DomainMessageConvention>();
    	});
    	config.SetStrategy(typeof(InMemoryTransport), builder =>
    	{
    		builder.Add<LocalMessageTransportStrategy>();
    		//builder.Add(new AttributeTransportStrategy([typeof(InMemoryTransport)]));
    		builder.EvaluateIncoming(_ => true);
    		builder.EvaluateOutgoing(_ => true);
    	});
        // Configure identity provider
    	config.SetIdentityProvider(jwt => JwtIdentityAccessor.Resolve(jwt, configuration));
    	config.RegisterHandlers([assemblies]);
    });
   ```

3. **Configure the Bus**: In your application's configuration file (e.g., appsettings.json), add the necessary settings for the message bus.

4. **Add Module Dependency**: Add the required transport module dependency in your service module.

5. **Implement Message Handlers**: Create message handler classes to process incoming messages.

6. **Run Your Application**: Start your application, and it will now use Euonia.Bus for message-based communication.
