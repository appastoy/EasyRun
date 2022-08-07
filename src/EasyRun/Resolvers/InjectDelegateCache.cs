using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EasyRun.Resolvers
{


    internal static class InjectDelegateCache
    {
        internal delegate void InjectDelegate<T>(ObjectResolver resolver, T value);

        static readonly MethodInfo s_resolve;
        static readonly MethodInfo s_tryResolve;
        static readonly Dictionary<Type, Delegate> s_delegateCacheMap = new Dictionary<Type, Delegate>();

        static InjectDelegateCache()
        {
            s_resolve = typeof(ObjectResolver)
                .GetMethods()
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(ObjectResolver.Resolve));
            s_tryResolve = typeof(ObjectResolver)
                .GetMethods()
                .FirstOrDefault(m => m.IsGenericMethod && m.Name == nameof(ObjectResolver.TryResolve));
        }


        public static InjectDelegate<T> Acquire<T>()
        {
            if (!s_delegateCacheMap.TryGetValue(typeof(T), out var cachedDelegate))
            {
                cachedDelegate = CreateDelegate<T>();
                s_delegateCacheMap.Add(typeof(T), cachedDelegate);
            }
            return (InjectDelegate<T>)cachedDelegate;
        }

        static InjectDelegate<T> CreateDelegate<T>()
        {
            var resolverParam = Expression.Parameter(typeof(ObjectResolver));
            var valueParam = Expression.Parameter(typeof(T));
            return Expression.Lambda<InjectDelegate<T>>(
                    CreateBody(resolverParam, valueParam, typeof(T)), 
                    resolverParam, 
                    valueParam)
                .Compile();
        }

        static Expression CreateBody(
            Expression resolverParam
            , Expression valueParam
            , Type type)
        {
            return Expression.Block(
                CreateInjectFieldExpressions(resolverParam, valueParam, GetInjectedFields(type))
                .Concat(CreateInjectPropertyExpressions(resolverParam, valueParam, GetInjectedProperties(type)))
                .Concat(CreateInjectMethodExpressions(resolverParam, valueParam, GetInjectedMethods(type))));
        }

        static IEnumerable<FieldInfo> GetInjectedFields(Type type)
        {
            return type.EnumerateWithBaseTypes()
                       .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                       .Where(f => !f.IsLiteral
                                && !f.IsInitOnly
                                && f.GetCustomAttribute<InjectedAttribute>() != null);
        }

        static IEnumerable<PropertyInfo> GetInjectedProperties(Type type)
        {
            return type.EnumerateWithBaseTypes()
                       .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                       .Where(p => p.CanWrite
                                && !p.SetMethod.IsAbstract
                                && p.GetIndexParameters().Length == 0
                                && p.GetCustomAttribute<InjectedAttribute>(true) != null
                                && (!p.SetMethod.IsVirtual
                                    || p.SetMethod.IsPrivate
                                    || p.SetMethod.DeclaringType == p.DeclaringType.GetProperty(p.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).DeclaringType));
        }

        static IEnumerable<MethodInfo> GetInjectedMethods(Type type)
        {
            return type.EnumerateWithBaseTypes()
           .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
           .Where(m => !m.IsConstructor
                    && !m.IsSpecialName
                    && !m.IsAbstract
                    && m.GetParameters().Length > 0
                    && m.GetCustomAttribute<InjectedAttribute>(true) != null
                    && (!m.IsVirtual
                        || m.IsPrivate
                        || m.DeclaringType == m.DeclaringType.GetMethod(m.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).DeclaringType));
        }

        static IEnumerable<Expression> CreateInjectFieldExpressions(
            Expression resolverParam
            , Expression valueParam
            , IEnumerable<FieldInfo> fields)
        {
            return fields.Select<FieldInfo, Expression>(f =>
                {
                    if (f.GetCustomAttribute<InjectedRequiredAttribute>() != null)
                        return Expression.Assign(
                            Expression.Field(valueParam, f),
                            Expression.Call(resolverParam, s_resolve.MakeGenericMethod(f.FieldType)));
                    var localVar = Expression.Variable(f.FieldType);
                    var tryResolve = Expression.IfThen(
                        Expression.Call(
                            resolverParam,
                            s_tryResolve.MakeGenericMethod(f.FieldType))
                        , Expression.Assign(Expression.Field(valueParam, f), localVar));
                    return Expression.Block(Enumerable.Repeat(localVar, 1), Enumerable.Repeat(tryResolve, 1));
                });
                
        }

        static IEnumerable<Expression> CreateInjectPropertyExpressions(
            Expression resolverParam
            , Expression valueParam
            , IEnumerable<PropertyInfo> properties)
        {
            return properties.Select<PropertyInfo, Expression>(p =>
            {
                if (p.GetCustomAttribute<InjectedRequiredAttribute>() != null)
                    return Expression.Assign(
                        Expression.Property(valueParam, p),
                        Expression.Call(resolverParam, s_resolve.MakeGenericMethod(p.PropertyType)));
                var localVar = Expression.Variable(p.PropertyType);
                var tryResolve = Expression.IfThen(
                    Expression.Call(
                        resolverParam,
                        s_tryResolve.MakeGenericMethod(p.PropertyType))
                    , Expression.Assign(Expression.Property(valueParam, p), localVar));
                return Expression.Block(Enumerable.Repeat(localVar, 1), Enumerable.Repeat(tryResolve, 1));
            });
        }

        static IEnumerable<Expression> CreateInjectMethodExpressions(
            Expression resolverParam
            , Expression valueParam
            , IEnumerable<MethodInfo> methods)
        {
            return methods.Select(m => 
                Expression.Call(valueParam, m, CreateMethodParameterExpressions(resolverParam, m.GetParameters())));
        }

        private static IEnumerable<Expression> CreateMethodParameterExpressions(Expression resolverParam, ParameterInfo[] parameters)
        {
            var resolveEssentialParameterExpressions = ResolveEssentialParameters(resolverParam, parameters);
            var localVariables = parameters
                .Where(p => p.HasDefaultValue)
                .Select(p => Expression.Variable(p.ParameterType, p.Name));
            if (localVariables.Any())
            {
                var resolveOptionalParameterExpressions = ResolveOptionalParameters(
                    resolverParam
                    , localVariables
                    , parameters);
                return resolveEssentialParameterExpressions.Concat(resolveOptionalParameterExpressions);
            }
            else
            {
                return resolveEssentialParameterExpressions;
            }
        }

        static IEnumerable<Expression> ResolveEssentialParameters(
            Expression resolverParam
            , IEnumerable<ParameterInfo> paramInfos)
        {
            return paramInfos
                .Where(p => !p.HasDefaultValue)
                .Select(p => Expression.Call(resolverParam, s_resolve.MakeGenericMethod(p.ParameterType)));
        }

        static IEnumerable<Expression> ResolveOptionalParameters(
            Expression resolverParam
            , IEnumerable<ParameterExpression> localVariables
            , IEnumerable<ParameterInfo> paramInfos)
        {
            return paramInfos.Select(p =>
            {
                var localVar = localVariables.First(v => v.Name == p.Name);
                return Expression.IfThen(Expression.Not(Expression.Call(
                        resolverParam
                        , s_tryResolve.MakeGenericMethod(p.ParameterType)
                        , localVar))
                    , Expression.Assign(localVar, Expression.Constant(p.DefaultValue, p.ParameterType)));
            });
        }
    }
}
