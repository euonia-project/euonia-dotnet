using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The model builder.
/// </summary>
public class ModelBuilder
{
    internal Action<Type, ModelProfile> OnConfigure { get; set; }

    /// <summary>
    /// Sets the configure action for specified model type.
    /// </summary>
    /// <param name="buildAction"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ModelBuilder For<T>(Action<ModelProfile<T>> buildAction)
        where T : class
    {
        var profile = new ModelProfile<T>();
        buildAction(profile);
        OnConfigure(typeof(T), profile);

        var mapAction = profile.MapAction ?? DefaultMapAction;
        BsonClassMap.RegisterClassMap(mapAction);
        return this;

        void DefaultMapAction(BsonClassMap<T> map)
        {
            if (profile.AutoMapProperty)
            {
                map.AutoMap();
            }

            map.SetIgnoreExtraElements(true);
            if (!string.IsNullOrEmpty(profile.KeyName))
            {
                var type = profile.KeyType ?? typeof(T).GetProperty(profile.KeyName)?.PropertyType;
                var memberMap = map.MapIdProperty(profile.KeyName).SetSerializer(new ObjectIdSerializer(type));
                if (profile.IdGenerator != null)
                {
                    memberMap.SetIdGenerator(profile.IdGenerator);
                }
            }

            foreach (var (name, property) in profile.Properties)
            {
                var member = map.MapProperty(name);
                if (!string.IsNullOrEmpty(property.ElementName))
                {
                    member.SetElementName(property.ElementName);
                }
                if (property.IsRequired.HasValue)
                {
                    member.SetIsRequired(property.IsRequired.Value);
                }
                if (property.ValueGenerator != null)
                {
                    member.SetDefaultValue(property.ValueGenerator.Generate);
                }
            }
        }
    }
}