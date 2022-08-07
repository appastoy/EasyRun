namespace EasyRun.Resolvers
{
    public interface IObjectResolver
    {
        T Resolve<T>();
        bool TryResolve<T>(out T value);
        void Inject<T>(T value);
    }
}
