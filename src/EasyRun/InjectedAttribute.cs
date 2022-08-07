using System;

namespace EasyRun
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class InjectedAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectedRequiredAttribute : InjectedAttribute
    {

    }
}
