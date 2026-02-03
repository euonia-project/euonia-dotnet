namespace Nerosoft.Euonia.Osba;

public partial class CommonRule
{
    /// <summary>
    /// Provides property required validation.
    /// </summary>
    public class Required : CommonRuleBase
    {
        /// <summary>
        /// Initialize a new instance of <see cref="Required"/>.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="message"></param>
        public Required(IPropertyInfo property, string message)
            : base(property, message)
        { }

        /// <summary>
        /// Initialize a new instance of <see cref="Required"/>.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="messageFactory"></param>
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
