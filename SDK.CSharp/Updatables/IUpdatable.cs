namespace OpenShock.SDK.CSharp.Updatables;

public interface IUpdatable<out T> : IUpdatableBase<T>
{
    public event Action<T>? OnValueChanged;
}