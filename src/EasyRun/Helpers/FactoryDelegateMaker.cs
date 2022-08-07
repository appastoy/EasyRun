using EasyRun.Descriptors;
using EasyRun.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EasyRun.Helpers
{
    internal delegate TInterface FactoryDelegate<TInterface>(ObjectResolver resolver, IReadOnlyDictionary<string, IParameterDescriptor> paramDescMap);

    internal static class FactoryDelegateMaker
    {
        internal static readonly MethodInfo s_resolveParameter;
        internal static readonly MethodInfo s_tryResolveParameter;
        static readonly Dictionary<Type, Delegate> s_delegateCache;

        static FactoryDelegateMaker()
        {
            s_resolveParameter = typeof(FactoryDelegateMaker)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .First(m => m.IsGenericMethod && m.Name == nameof(ResolveParameter));
            s_tryResolveParameter = typeof(FactoryDelegateMaker)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .First(m => m.IsGenericMethod && m.Name == nameof(TryResolveParameter));
            s_delegateCache = new Dictionary<Type, Delegate>();
        }
        
        public static FactoryDelegate<TInterface> Make<TInterface>(ITypeDescriptor typeDescriptor)
        {
            if (!s_delegateCache.TryGetValue(typeDescriptor.Type, out var compiledDelegate))
            {
                if (typeDescriptor.Parameters.Count == 0)
                {
                    compiledDelegate = new FactoryDelegate<TInterface>((_, __) => (TInterface)Activator.CreateInstance(typeDescriptor.Type));
                }
                else
                {
                    compiledDelegate = CreateDelegate<TInterface>(typeDescriptor);       
                }
                s_delegateCache.Add(typeDescriptor.Type, compiledDelegate);
            }
            return (FactoryDelegate<TInterface>)compiledDelegate;
        }

        private static FactoryDelegate<TInterface> CreateDelegate<TInterface>(ITypeDescriptor typeDescriptor)
        {
            var resolverParam = Expression.Parameter(typeof(ObjectResolver));
            var paramDescMapParam = Expression.Parameter(typeof(IReadOnlyDictionary<string, IParameterDescriptor>));
            var body = CreateBody(typeDescriptor, resolverParam, paramDescMapParam);
            return Expression.Lambda<FactoryDelegate<TInterface>>(
                                body,
                                Enumerable.Repeat(resolverParam, 1))
                                .Compile();
        }

        static Expression CreateBody(
            ITypeDescriptor typeDescriptor,
            ParameterExpression resolverParam
            , ParameterExpression paramDescMapParam)
        {
            return typeDescriptor.Parameters.All(p => !p.HasDefaultValue) ?
                CreateBodyNoOptionalParameter(typeDescriptor, resolverParam, paramDescMapParam) :
                CreateBodyWithOptionalParameter(typeDescriptor, resolverParam, paramDescMapParam);
        }

        static Expression CreateBodyNoOptionalParameter(ITypeDescriptor typeDescriptor, ParameterExpression resolverParam, ParameterExpression paramDescMapParam)
        {
            return Expression.New(typeDescriptor.Constructor, typeDescriptor.Parameters
                .Select(p => ResolveParameter(resolverParam, paramDescMapParam, p)));
        }

        static Expression ResolveParameter(
            ParameterExpression resolverParam,
            ParameterExpression paramDescMapParam,
            ParameterInfo param)
        {
            return Expression.Call(s_resolveParameter.MakeGenericMethod(param.ParameterType),
                resolverParam,
                paramDescMapParam,
                Expression.Constant(param, typeof(ParameterInfo)));
        }
        static Expression CreateBodyWithOptionalParameter(ITypeDescriptor typeDescriptor, ParameterExpression resolverParam, ParameterExpression paramDescMapParam)
        {
            var localVariables = CreateLocalVariables(typeDescriptor.Parameters);
            var ensureOptionalParameterExpressions =
                EnsureOptionalParameterExpressions(typeDescriptor, localVariables, resolverParam, paramDescMapParam);
            var newExpression = CreateNewExpression(typeDescriptor, localVariables, resolverParam, paramDescMapParam);
            return Expression.Block(localVariables, ensureOptionalParameterExpressions.Concat(
                Enumerable.Repeat(newExpression, 1)));
        }

        static IEnumerable<ParameterExpression> CreateLocalVariables(IEnumerable<ParameterInfo> parameters)
        {
            return parameters
                .Where(p => p.HasDefaultValue)
                .Select(p => Expression.Variable(p.ParameterType, p.Name));
        }

        static IEnumerable<Expression> EnsureOptionalParameterExpressions(
            ITypeDescriptor typeDescriptor,
            IEnumerable<ParameterExpression> localVariables,
            ParameterExpression resolverParam
            , ParameterExpression paramDescMapParam)
        {
            return typeDescriptor.Parameters
                .Where(p => p.HasDefaultValue)
                .Select(p =>
                {
                    var localVariable = localVariables.First(v => v.Name == p.Name);
                    return TryResolveParameter(resolverParam, paramDescMapParam, p, localVariable);
                });
        }      

        private static Expression TryResolveParameter(
            ParameterExpression resolverParam,
            ParameterExpression paramDescMapParam,
            ParameterInfo param,
            ParameterExpression localVariable)
        {
            return Expression.IfThen(
                Expression.Not(Expression.Call(s_tryResolveParameter.MakeGenericMethod(param.ParameterType),
                    resolverParam,
                    paramDescMapParam,
                    Expression.Constant(param.Name, typeof(string)),
                    localVariable)),
                Expression.Assign(localVariable, Expression.Constant(param.DefaultValue, param.ParameterType)));
        }

        static NewExpression CreateNewExpression(
            ITypeDescriptor typeDescriptor,
            IEnumerable<ParameterExpression> localVariables,
            ParameterExpression resolverParam,
            ParameterExpression paramDescMapParam)
        {
            var arguments = typeDescriptor.Parameters
                .Where(p => !p.HasDefaultValue)
                .Select(p => ResolveParameter(resolverParam, paramDescMapParam, p))
                .Concat(localVariables);

            return Expression.New(typeDescriptor.Constructor, arguments, typeDescriptor.Parameters.Select(p => p.Member));
        }

        static T ResolveParameter<T>(ObjectResolver resolver, IReadOnlyDictionary<string, IParameterDescriptor> paramDescMap, ParameterInfo paramInfo)
        {
            return paramDescMap.Count > 0 &&
                paramDescMap.TryGetValue(paramInfo.Name, out var paramDesc) &&
                paramDesc is IParameterDescriptor<T> valueParam ?
                    valueParam.Value :
                    resolver.Resolve<T>();
        }

        static bool TryResolveParameter<T>(
                ObjectResolver resolver
                , IReadOnlyDictionary<string, IParameterDescriptor> paramDescMap
                , string paramName
                , out T result)
        {
            if (paramDescMap.Count > 0 &&
                paramDescMap.TryGetValue(paramName, out var paramDesc) &&
                paramDesc is IParameterDescriptor<T> valueParam)
            {
                result = valueParam.Value;
                return true;
            }
            return resolver.TryResolve(out result);
        }
    }
}
