﻿using Cauldron.Core;
using Cauldron.Core.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Cauldron.Activator
{
    using Cauldron.Core.Diagnostics;

    /// <summary>
    /// Provides methods for creating and destroying object instances
    /// </summary>
    public sealed class Factory
    {
        private static readonly string iFactoryExtensionName = typeof(IFactoryResolver).FullName;
        private static Dictionary<string, IFactoryTypeInfo[]> components;
        private static IFactoryTypeInfo[] factoryInfoTypes;
        private static IFactoryResolver[] factoryResolvers;
        private static ConcurrentDictionary<string, FactoryInstancedObject> instances = new ConcurrentDictionary<string, FactoryInstancedObject>();

        static Factory()
        {
            factoryInfoTypes = Assemblies.CauldronObjects
                .Select(x => x as IFactoryCache)
                .Where(x => x != null)
                .SelectMany(x => x.GetComponents())
                .ToArray();

            InitializeFactory(factoryInfoTypes);

            Assemblies.LoadedAssemblyChanged += (s, e) =>
            {
                if (e.Cauldron == null)
                    return;

                var factoryCache = e.Cauldron as IFactoryCache;

                if (factoryCache == null)
                    return;

                factoryInfoTypes = factoryInfoTypes.Concat(factoryCache.GetComponents());
                InitializeFactory(factoryInfoTypes);
            };
        }

        /// <summary>
        /// Occures if an object was created
        /// </summary>
        public static event EventHandler<FactoryObjectCreatedEventArgs> ObjectCreated;

        /// <summary>
        /// Gets or sets a value that indicates if the <see cref="Factory"/> is allowed to raise an
        /// exception or not.
        /// </summary>
        public static bool CanRaiseExceptions { get; set; } = true;

        /// <summary>
        /// Gets a collection types that is known to the <see cref="Factory"/>
        /// </summary>
        public static IEnumerable<IFactoryTypeInfo> RegisteredTypes { get { return components.Values.SelectMany(x => x); } }

        /// <summary>
        /// Adds a new <see cref="Type"/> to list of known types. Should only be used for unit-tests
        /// </summary>
        /// <threadsafety static="false" instance="false"/>
        /// <param name="contractName">The name that identifies the type</param>
        /// <param name="creationPolicy">The creation policy of the type as defined by <see cref="FactoryCreationPolicy"/></param>
        /// <param name="type">The type to be added</param>
        /// <param name="createInstance">
        /// An action that is called by the factory to create the object
        /// </param>
        public static IFactoryTypeInfo AddType(string contractName, FactoryCreationPolicy creationPolicy, Type type, Func<object[], object> createInstance)
        {
            var factoryTypeInfo = new FactoryTypeInfoInternal(contractName, creationPolicy, type, createInstance);

            if (components.ContainsKey(contractName))
            {
                var content = components[contractName];
                components[contractName] = content.Concat(factoryTypeInfo);
                return factoryTypeInfo;
            }

            components.Add(contractName, new IFactoryTypeInfo[] { factoryTypeInfo });
            return factoryTypeInfo;
        }

        /// <summary>
        /// Creates an instance of the specified type depending on the <see cref="ComponentAttribute"/>
        /// </summary>
        /// <typeparam name="T">The Type that contract name derives from</typeparam>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <typeparamref name="T"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="AmbiguousMatchException">
        /// There is more than one implementation with contractname <typeparamref name="T"/> found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see
        /// cref="IDisposableObject"/> interface.
        /// </exception>
        public static T Create<T>(params object[] parameters) where T : class => GetInstance(typeof(T).FullName, parameters) as T;

        /// <summary>
        /// Creates an instance of the specified type depending on the <see cref="ComponentAttribute"/>
        /// </summary>
        /// <param name="contractName">The name that identifies the type</param>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="ArgumentNullException">
        /// The parameter <paramref name="contractName"/> is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The parameter <paramref name="contractName"/> is an empty string
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <paramref name="contractName"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="AmbiguousMatchException">
        /// There is more than one implementation with contractname <paramref name="contractName"/> found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see
        /// cref="IDisposableObject"/> interface.
        /// </exception>
        public static object Create(string contractName, params object[] parameters)
        {
            if (contractName == null)
                throw new ArgumentNullException(nameof(contractName));

            if (contractName.Length == 0)
                throw new ArgumentException("The parameter is an empty string", nameof(contractName));

            return GetInstance(contractName, parameters);
        }

        /// <summary>
        /// Creates an instance of the specified type depending on the <see cref="ComponentAttribute"/>
        /// </summary>
        /// <param name="contractType">The Type that contract name derives from</param>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="ArgumentNullException">
        /// The parameter <paramref name="contractType"/> is null
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <paramref name="contractType"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="AmbiguousMatchException">
        /// There is more than one implementation with contractname <paramref name="contractType"/> found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see
        /// cref="IDisposableObject"/> interface.
        /// </exception>
        public static object Create(Type contractType, params object[] parameters)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            return GetInstance(contractType.FullName, parameters);
        }

        /// <summary>
        /// Creates an instance of the specified type depending on the <see cref="ComponentAttribute"/>.
        /// If multiple implementations are available, the <see cref="Factory"/> will prefer the implementation with the highest priority.
        /// </summary>
        /// <typeparam name="T">The Type that contract name derives from</typeparam>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <typeparamref name="T"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see cref="IDisposableObject"/> interface.
        /// </exception>
        public static T CreateFirst<T>(params object[] parameters) where T : class => CreateFirst(typeof(T).FullName, parameters) as T;

        /// <summary>
        /// Creates an instance of the specified type depending on the <see cref="ComponentAttribute"/>.
        /// If multiple implementations are available, the <see cref="Factory"/> will prefer the implementation with the highest priority.
        /// </summary>
        /// <param name="contractType">The Type that contract name derives from</param>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="ArgumentNullException">
        /// The parameter <paramref name="contractType"/> is null
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <paramref name="contractType"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see cref="IDisposableObject"/> interface.
        /// </exception>
        public static object CreateFirst(Type contractType, params object[] parameters)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            return CreateFirst(contractType.FullName, parameters);
        }

        /// <summary>
        /// Creates an instance of the specified type depending on the <see cref="ComponentAttribute"/>.
        /// If multiple implementations are available, the <see cref="Factory"/> will prefer the implementation with the highest priority.
        /// </summary>
        /// <param name="contractName">The name that identifies the type</param>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="ArgumentNullException">
        /// The parameter <paramref name="contractName"/> is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The parameter <paramref name="contractName"/> is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <paramref name="contractName"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see cref="IDisposableObject"/> interface.
        /// </exception>
        public static object CreateFirst(string contractName, params object[] parameters)
        {
            if (contractName == null)
                throw new ArgumentNullException(nameof(contractName));

            if (contractName == "")
                throw new ArgumentException("contractName cannot be empty");

            if (components.TryGetValue(contractName, out IFactoryTypeInfo[] factoryInfos))
            {
                if (factoryInfos.Length == 1)
                    return GetInstance(factoryInfos[0], parameters);

                if (factoryInfos.Length != 0)
                    return GetInstance(factoryInfos.MaxBy(x => x.Priority), parameters);
            }

            if (CanRaiseExceptions)
                throw new KeyNotFoundException("The contractName '" + contractName + "' was not found.");

            Debug.WriteLine($"ERROR: The contractName '" + contractName + "' was not found.");
            return null;
        }

        /// <summary>
        /// Creates an instance of the specified type using the constructor that best matches the
        /// specified parameters. This method is similar to <see
        /// cref="ExtensionsReflection.CreateInstance(Type, object[])"/>, but this takes the types
        /// defined with <see cref="ComponentAttribute"/> into account. This also executes the
        /// factory extensions ( <see cref="IFactoryResolver"/>).
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <param name="args">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null</exception>
        /// <exception cref="NotImplementedException">
        /// Implementation of <paramref name="type"/> not found
        /// </exception>
        public static object CreateInstance(Type type, params object[] args)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            IFactoryTypeInfo[] factoryTypes;

            if (components.TryGetValue(type.FullName, out factoryTypes))
            {
                if (factoryTypes.Length == 1)
                    return factoryTypes[0].CreateInstance(args);

                if (factoryTypes.Length == 0)
                    return type.CreateInstance(args);

                var resolved = ResolveAmbiguousMatch(factoryTypes, type.FullName);
                return resolved.CreateInstance(args);
            }

            return type.CreateInstance(args);
        }

        /// <summary>
        /// Creates instances of the specified type depending on the <see cref="ComponentAttribute"/>
        /// </summary>
        /// <param name="contractName">The name that identifies the type</param>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A collection of the newly created objects.</returns>
        /// <exception cref="ArgumentNullException">
        /// The parameter <paramref name="contractName"/> is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The parameter <paramref name="contractName"/> is an empty string
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <paramref name="contractName"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see
        /// cref="IDisposableObject"/> interface.
        /// </exception>
        public static IEnumerable CreateMany(string contractName, params object[] parameters)
        {
            if (contractName == null)
                throw new ArgumentNullException(nameof(contractName));

            if (contractName.Length == 0)
                throw new ArgumentException("The parameter is an empty string", nameof(contractName));

            return GetInstances(contractName, parameters);
        }

        /// <summary>
        /// Creates instances of the specified type depending on the <see cref="ComponentAttribute"/>
        /// </summary>
        /// <param name="contractType">The Type that contract name derives from</param>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A collection of the newly created objects.</returns>
        /// <exception cref="ArgumentNullException">
        /// The parameter <paramref name="contractType"/> is null
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <paramref name="contractType"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see
        /// cref="IDisposableObject"/> interface.
        /// </exception>
        public static IEnumerable CreateMany(Type contractType, params object[] parameters)
        {
            if (contractType == null)
                throw new ArgumentNullException(nameof(contractType));

            return GetInstances(contractType.FullName, parameters);
        }

        /// <summary>
        /// Creates instances of the specified type depending on the <see cref="ComponentAttribute"/>
        /// </summary>
        /// <typeparam name="T">The Type that contract name derives from</typeparam>
        /// <param name="parameters">
        /// An array of arguments that match in number, order, and type the parameters of the
        /// constructor to invoke. If args is an empty array or null, the constructor that takes no
        /// parameters (the default constructor) is invoked.
        /// </param>
        /// <returns>A collection of the newly created objects.</returns>
        /// <exception cref="KeyNotFoundException">
        /// The contract described by <typeparamref name="T"/> was not found
        /// </exception>
        /// <exception cref="Exception">Unknown <see cref="FactoryCreationPolicy"/></exception>
        /// <exception cref="NotSupportedException">
        /// An <see cref="object"/> with <see cref="FactoryCreationPolicy.Singleton"/> with an
        /// implemented <see cref="IDisposable"/> must also implement the <see
        /// cref="IDisposableObject"/> interface.
        /// </exception>
        public static IEnumerable<T> CreateMany<T>(params object[] parameters) => GetInstances(typeof(T).FullName, parameters).Cast<T>();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <typeparam name="T">The Type that the contract name derives from</typeparam>
        public static void Destroy<T>() => Destroy(typeof(T));

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="contractType">The Type that the contract name derives from</param>
        public static void Destroy(Type contractType) => Destroy(contractType.FullName);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="contractName">The name that identifies the type</param>
        public static void Destroy(string contractName)
        {
            if (!components.ContainsKey(contractName))
                return;

            var content = components[contractName];

            for (int i = 0; i < content.Length; i++)
            {
                var key = contractName + content[i].Type.FullName;
                FactoryInstancedObject instance;

                if (instances.ContainsKey(key) && instances.TryRemove(key, out instance))
                    instance?.Dispose();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public static void Destroy()
        {
            var oldInstances = instances.ToArray();
            instances.Clear();

            for (int i = 0; i < oldInstances.Length; i++)
                oldInstances[i].TryDispose();
        }

        /// <summary>
        /// Determines whether a contract exist
        /// </summary>
        /// <param name="contractName">The name that identifies the type</param>
        /// <returns>True if the contract exists, otherwise false</returns>
        public static bool HasContract(string contractName) => components.ContainsKey(contractName);

        /// <summary>
        /// Determines whether a contract exist
        /// </summary>
        /// <param name="contractType">The Type that contract name derives from</param>
        /// <returns>True if the contract exists, otherwise false</returns>
        public static bool HasContract(Type contractType) => components.ContainsKey(contractType.FullName);

        /// <summary>
        /// Determines whether a contract exist
        /// </summary>
        /// <typeparam name="T">The Type that contract name derives from</typeparam>
        /// <returns>True if the contract exists, otherwise false</returns>
        public static bool HasContract<T>() => components.ContainsKey(typeof(T).FullName);

        /// <exclude/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void OnObjectCreation(object @object, IFactoryTypeInfo factoryTypeInfo) => ObjectCreated?.Invoke(null, new FactoryObjectCreatedEventArgs(@object, factoryTypeInfo));

        /// <summary>
        /// Removes a <see cref="Type"/> from the list of known types.
        /// ATTENTION: Should only be used in unit-tests.
        /// </summary>
        /// <param name="contractName">The name that identifies the type</param>
        /// <param name="type">The type to be removed</param>
        /// <threadsafety static="false" instance="false"/>
        public static void RemoveType(string contractName, Type type)
        {
            if (!components.ContainsKey(contractName))
                return;

            var content = components[contractName].ToArray() /* ToArray should insure that we are dealing with a new array */.Where(x => x.Type != type);
            components[contractName] = content.ToArray();
        }

        private static object CreateInstance(IFactoryTypeInfo factoryType, params object[] args)
        {
            if (factoryType == null)
                throw new ArgumentNullException(nameof(factoryType));

            return factoryType.CreateInstance(args);
        }

        private static object GetInstance(IFactoryTypeInfo factoryTypeInfo, object[] parameters)
        {
            if (factoryTypeInfo.CreationPolicy == FactoryCreationPolicy.Instanced)
                return CreateInstance(factoryTypeInfo, parameters);
            else if (factoryTypeInfo.CreationPolicy == FactoryCreationPolicy.Singleton)
            {
                if (instances.TryGetValue(factoryTypeInfo.ContractName + factoryTypeInfo.Type.FullName, out FactoryInstancedObject existingInstance))
                    return existingInstance.Item;
                else
                {
                    // Create the instance and return the object
                    var newInstance = CreateInstance(factoryTypeInfo, parameters);
                    var key = factoryTypeInfo.ContractName + factoryTypeInfo.Type.FullName;

                    // every singleton that implements the idisposable interface has also to
                    // implement the IDisposableObject interface this is because we want to know if
                    // an instance was disposed (somehow)
                    if (newInstance is IDisposable disposable)
                    {
                        var disposableObject = newInstance as IDisposableObject;
                        if (disposableObject == null)
                            throw new NotSupportedException("An object with creation policy 'Singleton' with an implemented 'IDisposable' must also implement the 'IDisposableObject' interface.");

                        disposableObject.Disposed += (s, e) =>
                        {
                            if (instances.ContainsKey(key) && instances.TryRemove(key, out FactoryInstancedObject thisInstance))
                                thisInstance?.Dispose();
                        };
                    }

                    instances.TryAdd(key, new FactoryInstancedObject { FactoryTypeInfo = factoryTypeInfo, Item = newInstance });
                    return newInstance;
                }
            }
            else
                throw new Exception("Unknown creation policy");
        }

        private static object GetInstance(string contractName, object[] parameters)
        {
            IFactoryTypeInfo[] factoryInfos;

            if (components.TryGetValue(contractName, out factoryInfos))
            {
                if (factoryInfos.Length == 1)
                    return GetInstance(factoryInfos[0], parameters);

                if (factoryInfos.Length == 0)
                {
                    if (CanRaiseExceptions)
                        throw new NotImplementedException("The contractName '" + contractName + "' was not found.");
                    else
                        Debug.WriteLine($"ERROR: The contractName '{contractName}' was not found.");
                }

                return GetInstance(ResolveAmbiguousMatch(factoryInfos, contractName), parameters);
            }
            else
            {
                try
                {
                    // Try to find out the type
                    var realType = Assemblies.GetTypeFromName(contractName);

                    if (realType == null)
                    {
                        if (CanRaiseExceptions)
                            throw new NotImplementedException("The contractName '" + contractName + "' was not found.");
                        else
                            Debug.WriteLine($"ERROR: The contractName '{contractName}' was not found.");

                        return null;
                    }

                    return realType.CreateInstance(parameters);
                }
                catch (Exception e)
                {
                    if (CanRaiseExceptions)
                        throw;
                    else
                        Debug.WriteLine(e.Message);

                    return null;
                }
            }
        }

        private static IEnumerable GetInstances(string contractName, object[] parameters)
        {
            IFactoryTypeInfo[] factoryInfos;

            if (components.TryGetValue(contractName, out factoryInfos))
            {
                if (factoryInfos.Length == 1)
                    return new object[] { GetInstance(factoryInfos[0], parameters) };

                if (factoryInfos.Length == 0)
                    return new object[0];

                return factoryInfos.OrderByDescending(x => x.Priority).Select(x => GetInstance(x, parameters)).ToArray();
            }

            if (CanRaiseExceptions)
                throw new NotImplementedException("The contractName '" + contractName + "' was not found.");

            Debug.WriteLine($"ERROR: The contractName '" + contractName + "' was not found.");
            return new object[0];
        }

        private static void InitializeFactory(IFactoryTypeInfo[] factoryInfoTypes)
        {
            // Get all known components
            components = factoryInfoTypes
                .GroupBy(x => x.ContractName)
                .ToDictionary(x => x.Key, x => x.ToArray());

            // Get all factory extensions
            factoryResolvers = factoryInfoTypes
                .Where(x => x.ContractName.GetHashCode() == iFactoryExtensionName.GetHashCode() && x.ContractName == iFactoryExtensionName).Select(x => x.CreateInstance() as IFactoryResolver)
                .ToArray();

            if (components.Count == 0)
                Debug.WriteLine($"ERROR: Unable to find any components. Please check if FodyWeavers.xml has an entry for Cauldron.Interception");
        }

        private static IFactoryTypeInfo ResolveAmbiguousMatch(string contractName) => ResolveAmbiguousMatch(components[contractName], contractName);

        private static IFactoryTypeInfo ResolveAmbiguousMatch(IFactoryTypeInfo[] factoryInfos, string contractName)
        {
            for (int i = 0; i < factoryResolvers.Length; i++)
            {
                var selectedType = factoryResolvers[i].SelectAmbiguousMatch(factoryInfos, contractName);

                if (selectedType == null)
                    continue;

                return selectedType;
            }

            throw new AmbiguousMatchException("There is more than one implementation with contractname '" + contractName + "' found.");
        }

        private class FactoryInstancedObject : IDisposable
        {
            private volatile bool disposed = false;

            ~FactoryInstancedObject()
            {
                this.Dispose(false);
            }

            public IFactoryTypeInfo FactoryTypeInfo { get; set; }

            /// <summary>
            /// Gets a value indicating if the object has been disposed or not
            /// </summary>
            public bool IsDisposed { get { return this.disposed; } }

            public object Item { get; set; }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <param name="disposing">true if managed resources requires disposing</param>
            [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
            protected void Dispose(bool disposing)
            {
                // Check to see if Dispose has already been called.
                if (!this.disposed)
                {
                    if (disposing)
                        this.Item.TryDispose();

                    disposed = true;
                }
            }
        }
    }
}