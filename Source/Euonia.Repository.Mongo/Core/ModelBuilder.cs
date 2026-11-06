using MongoDB.Bson.Serialization;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 模型构建器。
/// </summary>
public class ModelBuilder
{
    /// <summary>
    /// 获取或设置模型配置完成时的回调，用于将类型与配置登记到模型配置容器。
    /// </summary>
    internal Action<Type, ModelProfile> OnConfigure { get; set; } = null!;

    /// <summary>
    /// 为指定模型类型设置配置操作。
    /// </summary>
    /// <param name="buildAction">用于配置指定模型类型的委托。</param>
    /// <typeparam name="T">要配置的模型类型。</typeparam>
    /// <returns>当前构建器实例，以支持链式调用。</returns>
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
                var memberMap = map.MapIdProperty(profile.KeyName).SetSerializer(new ObjectIdSerializer(type!));
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