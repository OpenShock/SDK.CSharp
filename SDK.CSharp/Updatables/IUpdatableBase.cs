namespace OpenShock.SDK.CSharp.Updatables;

public interface IUpdatableBase<out T>
{
    public T Value { get; }
}