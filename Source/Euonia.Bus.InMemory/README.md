### In-Memory Transport
The In-Memory Transport is a lightweight transport mechanism designed for scenarios where messages need to be exchanged within the same process or application domain. It is ideal for testing, development, and scenarios where low-latency communication is required without the overhead of network protocols.

### Features

- **Lightweight**: Minimal overhead for message processing.
- **Fast**: Low-latency communication within the same application.
- **Easy to Use**: Simple setup and configuration.
- **Ideal for Testing**: Perfect for unit tests and local development.

### Getting Started

To get started with the In-Memory transport in Euonia.Bus, follow these steps:

1. **Add the In-Memory Transport Package**: Include the Euonia.Bus.InMemory package in your project.
   ```bash
   dotnet add package Euonia.Bus.InMemory
   ```

2. **Configure the In-Memory Transport**: In your application's configuration file (e.g., appsettings.json), add the In-Memory transport settings under the `EuoniaBus` section:
   ```json
   {
     "EuoniaBus": {
       "InMemory": {
         "Name": "InMemory"
       }
     }
   }
   ```

3. **Add Module Dependency**: Add the In-Memory transport module dependency in your service module:
   ```csharp
   [DependsOn(typeof(InMemoryBusModule))]
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
       config.SetStrategy("InMemory", builder =>
       {
           builder.Add(new AttributeTransportStrategy(["InMemory"]));
           builder.Add<LocalMessageTransportStrategy>();
           builder.EvaluateOutgoing(e => true);
           builder.EvaluateIncoming(e => true);
       });
   });
   ```

5. **Run Your Application**: Start your application, and it will now use In-Memory as the transport for Euonia.Bus.