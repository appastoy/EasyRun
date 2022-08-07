using EasyRun.Resolvers;
using System;

namespace EasyRun.Registrations
{
    public interface IDependencyRegistration
    {
        IDependencyTypeRegistration RegisterType<T>(LifeTime lifeTime = LifeTime.Scoped);
        IDependencyTypeRegistration RegisterType<TInterface, T>(LifeTime lifeTime = LifeTime.Scoped) where T : TInterface;
        IDependencyRegistration Register<T>(T instance);
        IDependencyRegistration RegisterFactory<T>(Func<IObjectResolver, T> factory, LifeTime lifeTime = LifeTime.Scoped);
        IDependencyTypeRegistration TryRegisterType<T>(out bool isRegistered, LifeTime lifeTime = LifeTime.Scoped);
        IDependencyTypeRegistration TryRegisterType<TInterface, T>(out bool isRegistered, LifeTime lifeTime = LifeTime.Scoped) where T : TInterface;
        IDependencyRegistration TryRegister<T>(T instance, out bool isRegistered);
        IDependencyRegistration TryRegisterFactory<T>(Func<IObjectResolver, T> factory, out bool isRegistered, LifeTime lifeTime = LifeTime.Scoped);
    }

    public interface IDependencyTypeRegistration : IDependencyRegistration
    {
        IDependencyTypeRegistration WithParam<T>(string name, T value);
        IDependencyTypeRegistration WithParamFactory<T>(string name, Func<T> value);
    }
}
