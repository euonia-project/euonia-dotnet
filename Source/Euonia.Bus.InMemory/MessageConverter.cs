using Nerosoft.Euonia.Reflection;
using Newtonsoft.Json;

namespace Nerosoft.Euonia.Bus.InMemory;

internal class MessageConverter
{
    public static object? Convert(object source, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        if (targetType == source.GetType())
        {
            return source;
        }

        if (targetType == typeof(object))
        {
            return source;
        }

        if (targetType == typeof(Guid))
        {
            return TypeHelper.CoerceValue<Guid>(source.GetType(), source);
        }

        if (targetType.IsAssignableTo(typeof(IConvertible)))
        {
            return TypeHelper.CoerceValue(targetType, source.GetType(), source);
        }

        if (source is string content)
        {
            return JsonConvert.DeserializeObject(content, targetType);
        }

        return null;
    }
}