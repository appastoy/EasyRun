namespace EasyRun.Resolvers
{
    public interface IObjectResolver
    {
        T Resolve<T>();
        bool TryResolve<T>(out T value);
        IObjectResolver Inject<T>(T value);
    }
}
