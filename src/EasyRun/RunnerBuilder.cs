﻿using EasyRun.Builders;
using EasyRun.Descriptors;
using EasyRun.Registrations;
using EasyRun.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyRun
{
    public sealed class RunnerBuilder : IDependencyTypeRegistration
    {
        readonly Dictionary<Type, List<IObjectDescriptor>> _objectDescriptorsByTypeMap =
            new Dictionary<Type, List<IObjectDescriptor>>();

        readonly ObjectResolver _parentResolver;
        ITypeDescriptor _lastTypeDescriptor;

        internal RunnerBuilder(ObjectResolver parentResolver) => _parentResolver = parentResolver;

        public RunnerBuilder Register<T>(LifeTime lifeTime = LifeTime.Scoped)
        {
            return Register<T, T>(lifeTime);
        }

        public RunnerBuilder Register<TInterface, T>(LifeTime lifeTime = LifeTime.Scoped) where T : TInterface
        {
            CheckTypeAlreadyRegistered(typeof(T));
            return AddDescriptor(typeof(TInterface), _lastTypeDescriptor = new TypeDescriptor<TInterface, T>(lifeTime));
        }

        public RunnerBuilder Register<T>(T instance)
        {
            _lastTypeDescriptor = null;
            return AddDescriptor(typeof(T), new InstanceDescriptor<T>(typeof(T), instance));
        }

        public RunnerBuilder RegisterFactory<T>(Func<IObjectResolver, T> factory, LifeTime lifeTime = LifeTime.Scoped)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            _lastTypeDescriptor = null;
            return AddDescriptor(typeof(T), new FactoryDescriptor<T>(typeof(T), lifeTime, factory));
        }

        public RunnerBuilder TryRegister<T>(out bool isRegistered, LifeTime lifeTime = LifeTime.Scoped)
        {
            return TryRegister<T, T>(out isRegistered, lifeTime);
        }

        public RunnerBuilder TryRegister<TInterface, T>(out bool isRegistered, LifeTime lifeTime = LifeTime.Scoped) where T : TInterface
        {
            if (isRegistered = !IsRegistered(typeof(TInterface)))
            {
                return Register<TInterface, T>(lifeTime);
            }
            else
            {
                _lastTypeDescriptor = null;
                return this;
            }
        }

        public RunnerBuilder TryRegister<T>(T instance, out bool isRegistered)
        {
            return (isRegistered = !IsRegistered(typeof(T))) ? Register(instance) : this;
        }

        public RunnerBuilder TryRegisterFactory<T>(Func<IObjectResolver, T> factory, out bool isRegistered, LifeTime lifeTime = LifeTime.Scoped)
        {
            return (isRegistered = !IsRegistered(typeof(T))) ? RegisterFactory(factory, lifeTime) : this;
        }

        public RunnerBuilder WithParam<T>(string name, T value)
        {
            if (_lastTypeDescriptor == null)
                throw new InvalidOperationException("You should call RegisterType() method before call WithParam() method.");
            _lastTypeDescriptor.AddParam(name, value);
            return this;
        }

        public RunnerBuilder WithParamFactory<T>(string name, Func<T> factory)
        {
            if (_lastTypeDescriptor == null)
                throw new InvalidOperationException("You should call RegisterType() method before call WithParamFactory() method.");
            _lastTypeDescriptor.AddParamFactory(name, factory);
            return this;
        }

        public Runner Build()
        {
            return new Runner(
                new ObjectResolver(
                    _objectDescriptorsByTypeMap.ToDictionary(
                        kv => kv.Key,
                        kv => (IReadOnlyList<IObjectBuilder>)kv.Value.Select(v => v.CreateBuilder()).ToArray()),
                    _parentResolver)
                .Prebuild());
        }

        void CheckTypeAlreadyRegistered(Type type)
        {
            if (_objectDescriptorsByTypeMap.TryGetValue(type, out var list) &&
                list.Any(item => item.Type == type))
                throw new ArgumentException($"type already registered used RegisterType<T>. ({type.Name})");
        }

        RunnerBuilder AddDescriptor(Type type, IObjectDescriptor descriptor)
        {
            if (!_objectDescriptorsByTypeMap.TryGetValue(type, out var list))
            {
                list = new List<IObjectDescriptor>();
                _objectDescriptorsByTypeMap.Add(type, list);
            }
            list.Add(descriptor);
            return this;
        }

        bool IsRegistered(Type type) => _objectDescriptorsByTypeMap.ContainsKey(type);

        IDependencyTypeRegistration IDependencyTypeRegistration.WithParam<T>(string name, T value)
            => WithParam(name, value);

        IDependencyTypeRegistration IDependencyTypeRegistration.WithParamFactory<T>(string name, Func<T> value)
            => WithParamFactory(name, value);

        IDependencyTypeRegistration IDependencyRegistration.Register<T>(LifeTime lifeTime)
            => Register<T>(lifeTime);

        IDependencyTypeRegistration IDependencyRegistration.Register<TInterface, T>(LifeTime lifeTime)
            => Register<TInterface, T>(lifeTime);

        IDependencyRegistration IDependencyRegistration.Register<T>(T instance)
            => Register(instance);

        IDependencyRegistration IDependencyRegistration.RegisterFactory<T>(Func<IObjectResolver, T> factory, LifeTime lifeTime)
            => RegisterFactory(factory, lifeTime);

        IDependencyTypeRegistration IDependencyRegistration.TryRegister<T>(out bool isRegistered, LifeTime lifeTime)
            => TryRegister<T>(out isRegistered, lifeTime);

        IDependencyTypeRegistration IDependencyRegistration.TryRegister<TInterface, T>(out bool isRegistered, LifeTime lifeTime)
            => TryRegister<TInterface, T>(out isRegistered, lifeTime);

        IDependencyRegistration IDependencyRegistration.TryRegister<T>(T instance, out bool isRegistered)
            => TryRegister(instance, out isRegistered);

        IDependencyRegistration IDependencyRegistration.TryRegisterFactory<T>(Func<IObjectResolver, T> factory, out bool isRegistered, LifeTime lifeTime)
            => TryRegisterFactory(factory, out isRegistered, lifeTime);
    }
}
