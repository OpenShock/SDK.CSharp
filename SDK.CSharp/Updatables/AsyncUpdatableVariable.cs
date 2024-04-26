using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp.Updatables;

public sealed class AsyncUpdatableVariable<T>(T internalValue) : IAsyncUpdatable<T>
{
    public T Value
    {
        get => internalValue;
        set
        {
            if (internalValue!.Equals(value)) return;
            internalValue = value;
            Task.Run(() => OnValueChanged?.Raise(value));
        }
    }
    
    public event Func<T, Task>? OnValueChanged;
    
    public void UpdateWithoutNotify(T newValue)
    {
        internalValue = newValue;
    }
}