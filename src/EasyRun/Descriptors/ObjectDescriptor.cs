using EasyRun.Builders;
using EasyRun.Registrations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EasyRun.Descriptors
{
    internal interface IObjectDescriptor
    {
        Type Type { get; }
        LifeTime LifeTime { get; }
        IObjectBuilder CreateBuilder();
    }
}
