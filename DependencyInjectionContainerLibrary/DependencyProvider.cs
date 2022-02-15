using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DependencyInjectionContainerLibrary.Exceptions;

namespace DependencyInjectionContainerLibrary
{
    public class DependencyProvider
    {
        private DependencyConfiguration _configuration;

        public List<Type> listInstances;

        public DependencyProvider(DependencyConfiguration configuration)
        {
            _configuration = configuration;
            listInstances = new List<Type>();
           
        }
        
        public Object Resolve<TType>() where TType : class
        {
            Type type = typeof(TType);
            if (typeof(IEnumerable).IsAssignableFrom(type))
            { 
                Type genericType = type.GetGenericArguments()[0];
                if (_configuration.HasType(genericType))
                {
                    List<Object> list = new List<object>();
                    foreach (var implementation in _configuration.GetAllImplementations(genericType))
                    {
                        list.Add(Resolve(implementation));
                    }

                    return list.AsEnumerable();
                }
            }
            
            if (_configuration.HasType(type))
            {
                Implementation implementation = _configuration.GetFirstImplementation(type);
                object obj = Resolve(implementation);
                
                return obj as TType;
            }

            if (type.IsGenericType)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                Type genericArgument = type.GetGenericArguments()[0];
                if (_configuration.HasType(genericTypeDefinition) && _configuration.HasType(genericArgument))
                {
                    return ResolveOpenGeneric(_configuration.GetFirstImplementation(genericTypeDefinition).Type,
                        genericArgument) as TType;
                }
            }
            
            throw new UnsupportedTypeException("Unsupported type: " + type.FullName);
        }

        private Object CreateObject(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors();

            foreach (var constructorInfo in constructors)
            {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (parameters.All(param => _configuration.HasType(param.ParameterType)))
                {
                    Object value;
                    
                        value = constructorInfo.Invoke(parameters.Select(param => Resolve(_configuration.GetFirstImplementation(param.ParameterType))).ToArray());
                        
                    return value;
                }
            }

            throw new NoSuitableConstructorException("No suitable constructor for type: " + type.FullName);
        }

        private object ResolveOpenGeneric(Type baseType, Type genericArgumentType)
        {
            return CreateObject(baseType.MakeGenericType(genericArgumentType));
        }

        private object Resolve(Implementation implementation)
        {
            Type type = implementation.Type;
            
            if (implementation.IsSingleton && implementation.Value != null)
            {
                return implementation.Value;
            }
            
            listInstances.Add(type);
            
            object value = CreateObject(type);

            if (implementation.IsSingleton)
            {
                implementation.Value = value;
            }

            return value;
        }
    }
}