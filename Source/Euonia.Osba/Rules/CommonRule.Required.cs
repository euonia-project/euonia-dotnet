namespace Nerosoft.Euonia.Osba;

public partial class CommonRule
{
    /// <summary>
    /// 提供属性必填验证。
    /// </summary>
    public class Required : CommonRuleBase
    {
        /// <summary>
        /// 初始化 <see cref="Required"/> 的新实例。
        /// </summary>
        /// <param name="property">要检查的属性。</param>
        /// <param name="message">规则的错误消息。</param>
        public Required(IPropertyInfo property, string message)
            : base(property, message)
        { }

        /// <summary>
        /// 初始化 <see cref="Required"/> 的新实例。
        /// </summary>
        /// <param name="property">要检查的属性。</param>
        /// <param name="messageFactory">生成规则消息的委托。</param>
        public Required(IPropertyInfo property, Func<string> messageFactory)
            : base(property, messageFactory)
        {
        }

        /// <inheritdoc />
        public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
        {
            if (context.Target is IBusinessObject target)
            {
                var value = target.ReadProperty(Property);
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    var message = string.Format(MessageFactory(), Property.FriendlyName);
                    context.AddErrorResult(message);
                }
            }

            await Task.CompletedTask;
        }
    }
}
