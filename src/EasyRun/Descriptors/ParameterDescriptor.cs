using System;

namespace EasyRun.Descriptors
{
    internal interface IParameterDescriptor
    {
        string Name { get; }
        Type ValueType { get; }
    }

    internal interface IParameterDescriptor<T> : IParameterDescriptor
    {
        T Value { get; }
    }

    internal sealed class ParameterValueDescriptor<T> : IParameterDescriptor<T>
    {
        public string Name { get; }
        public T Value { get; }
        public Type ValueType => typeof(T);

        public ParameterValueDescriptor(string name, T value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }
    }

    internal sealed class ParameterFactoryDescriptor<T> : IParameterDescriptor<T>
    {
        readonly Func<T> _factory;
        T _value;
        bool _hasValue;

        public string Name { get; }
        public T Value
        {
            get
            {
                if (!_hasValue)
                {
                    _value = _factory.Invoke();
                    _hasValue = true;
                }
                return _value;
            }
        }
        public Type ValueType => typeof(T);

        public ParameterFactoryDescriptor(string name, Func<T> factory)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _factory = factory;
        }
    }
}
