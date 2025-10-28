using System;
using System.Collections.Generic;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// 简单的服务定位器实现
    /// </summary>
    public class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">服务实例</param>
        public static void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已注册</returns>
        public static bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 清除所有已注册的服务
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}