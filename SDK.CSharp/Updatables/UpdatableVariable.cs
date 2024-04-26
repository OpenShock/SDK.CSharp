namespace OpenShock.SDK.CSharp.Updatables;

public sealed class UpdatableVariable<T>(T internalValue) : IUpdatable<T>
{
    public T Value
    {
        get => internalValue;
        set
        {
            if (internalValue!.Equals(value)) return;
            internalValue = value;
            OnValueChanged?.Invoke(value);
        }
    }
    
    public event Action<T>? OnValueChanged;
    
    public void UpdateWithoutNotify(T newValue)
    {
        internalValue = newValue;
    }
}