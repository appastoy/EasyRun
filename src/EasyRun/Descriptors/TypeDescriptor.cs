using EasyRun.Builders;
using EasyRun.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyRun.Descriptors
{
    public interface IObjectCreationOption
    {
        void AddParam<T>(string name, T value);
        void AddParamFactory<T>(string name, Func<T> factory);
    }

    internal interface ITypeDescriptor : IObjectDescriptor, IObjectCreationOption
    {
        ConstructorInfo Constructor { get; }
        IReadOnlyList<ParameterInfo> Parameters { get; }
    }

    internal sealed class TypeDescriptor<TInterface, T> : ITypeDescriptor
        where T : TInterface
    {
        static readonly ConstructorInfo s_constructor;
        static readonly ParameterInfo[] s_parameters;

        static TypeDescriptor()
        {
            s_constructor = typeof(T).GetConstructors().SingleOrDefault();
            if (s_constructor == null)
                throw new ArgumentException($"type should have only one constructor. ({typeof(T).Name})");
            s_parameters = s_constructor.GetParameters();
        }

        Dictionary<string, IParameterDescriptor> _paramDescMap;
        Dictionary<string, IParameterDescriptor> ParamDescMap
        {
            get => _paramDescMap ?? (_paramDescMap = new Dictionary<string, IParameterDescriptor>());
        }

        public Type Type => typeof(T);
        public LifeTime LifeTime { get; }
        public ConstructorInfo Constructor => s_constructor;
        public IReadOnlyList<ParameterInfo> Parameters => s_parameters;
        public TypeDescriptor(LifeTime lifeTime) => LifeTime = lifeTime;

        public void AddParam<TParam>(string name, TParam value)
        {
            ValidateParam<TParam>(name, nameof(RunnerBuilder.WithParam));
            ParamDescMap.Add(name, new ParameterValueDescriptor<TParam>(name, value));
        }

        public void AddParamFactory<TParam>(string name, Func<TParam> factory)
        {
            ValidateParam<TParam>(name, nameof(RunnerBuilder.WithParamFactory));
            ParamDescMap.Add(name, new ParameterFactoryDescriptor<TParam>(name, factory));
        }

        public IObjectBuilder CreateBuilder() => CreateBuilderInternal();

        internal IObjectBuilder<TInterface> CreateBuilderInternal()
        {
            var factory = FactoryDelegateMaker.Make<TInterface>(this);
            return LifeTime == LifeTime.Transient ?
                (IObjectBuilder<TInterface>)new TransientBuilder<TInterface>(factory, _paramDescMap) :
                new ScopedBuilder<TInterface>(factory, _paramDescMap);
        }

        void ValidateParam<TParam>(string name, string methodName)
        {
            if (_paramDescMap?.ContainsKey(name) ?? false)
                throw new ArgumentException($"\"{name}\" parameter of {Type.Name} constructor already added. You can register each parameter only once using WithParam().");
            var param = s_parameters.FirstOrDefault(p => p.Name == name);
            if (param == null)
                throw new ArgumentException($"\"{name}\" parameter is not exists in {Type.Name} constructor.");
            if (!param.ParameterType.IsAssignableFrom(typeof(TParam)))
                throw new ArgumentException($"\"{name}\" parameter of {Type.Name} constructor value type is mismatch. (parameterType: {param.ParameterType.Name}, {methodName}().valueType: {typeof(TParam).Name})");
        }
    }
}
