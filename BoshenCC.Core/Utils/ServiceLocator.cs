using System;
using System.Collections.Generic;
using System.Linq;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 服务生命周期枚举
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// 瞬态，每次请求都创建新实例
        /// </summary>
        Transient,
        /// <summary>
        /// 单例，整个应用程序生命周期内只有一个实例
        /// </summary>
        Singleton,
        /// <summary>
        /// 作用域，在特定作用域内是单例
        /// </summary>
        Scoped
    }

    /// <summary>
    /// 服务提供者接口
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// 获取服务
        /// </summary>
        T GetService<T>();
        /// <summary>
        /// 获取服务
        /// </summary>
        object GetService(Type serviceType);
    }

    /// <summary>
    /// 增强的服务定位器实现，支持依赖注入
    /// </summary>
    public class ServiceLocator : IServiceProvider
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        public static void RegisterTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            // 简化实现，暂时使用原有的注册方式
            // 后续可以扩展为完整的依赖注入容器
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        public static void RegisterSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            // 简化实现，暂时使用原有的注册方式
            // 后续可以扩展为完整的依赖注入容器
        }

        /// <summary>
        /// 注册单例实例
        /// </summary>
        public static void RegisterSingleton<TService>(TService implementationInstance)
            where TService : class
        {
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            lock (_lock)
            {
                _services[typeof(TService)] = implementationInstance;
            }
        }

        /// <summary>
        /// 兼容性方法：注册服务（作为单例）
        /// </summary>
        public static void Register<T>(T service)
        {
            RegisterSingleton(service);
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static T GetService<T>()
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out object service))
                {
                    return (T)service;
                }
            }
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static object GetService(Type serviceType)
        {
            lock (_lock)
            {
                if (_services.TryGetValue(serviceType, out object service))
                {
                    return service;
                }
            }
            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }

        /// <summary>
        /// IServiceProvider 实现
        /// </summary>
        T IServiceProvider.GetService<T>()
        {
            return GetService<T>();
        }

        /// <summary>
        /// IServiceProvider 实现
        /// </summary>
        object IServiceProvider.GetService(Type serviceType)
        {
            return GetService(serviceType);
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public static bool IsRegistered<T>()
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public static bool IsRegistered(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }

        /// <summary>
        /// 清除所有已注册的服务
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }

        /// <summary>
        /// 获取已注册的服务类型
        /// </summary>
        public static IEnumerable<Type> GetRegisteredServiceTypes()
        {
            lock (_lock)
            {
                return _services.Keys.ToList();
            }
        }
    }
}
