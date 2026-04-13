using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskLine.ViewModels;

/// <summary>
/// 所有 ViewModel 的抽象基类。
/// 封装 <see cref="INotifyPropertyChanged"/> 的通用实现，供子类继承复用。
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    #region INotifyPropertyChanged

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发 <see cref="PropertyChanged"/> 事件，通知 UI 指定属性已变更。
    /// </summary>
    /// <param name="name">已变更的属性名称，由编译器自动填充。</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// 若新值与当前值不同，则更新字段并触发属性变更通知。
    /// </summary>
    /// <typeparam name="T">字段的类型。</typeparam>
    /// <param name="field">对私有字段的引用。</param>
    /// <param name="value">要设置的新值。</param>
    /// <param name="name">属性名称，由编译器自动填充。</param>
    /// <returns>若值发生了变化则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    #endregion
}
