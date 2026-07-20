namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 表示一个特定的状态。
/// </summary>
/// <seealso cref="StatefulMutex"/>
public class State
{
    private readonly int _stateNum;

    /// <summary>
    /// 创建一个新状态。
    /// </summary>
    public State()
        : this(0)
    {
    }

    private State(int stateNum)
    {
        _stateNum = stateNum;
    }

    /// <summary>
    /// 创建一个跟随当前状态之后的新状态。
    /// </summary>
    /// <returns>表示下一状态的新 <see cref="State"/> 实例。</returns>
    public State GetNextState()
    {
        return new State(_stateNum + 1);
    }

    /// <summary>
    /// 通过状态编号比较状态。
    /// </summary>
    /// <param name="obj">要比较的对象。</param>
    /// <returns>如果它们具有相同的状态编号则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public override bool Equals(object obj)
    {
        var otherState = obj as State;
        return otherState?._stateNum == _stateNum;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _stateNum.GetHashCode();
    }
}
