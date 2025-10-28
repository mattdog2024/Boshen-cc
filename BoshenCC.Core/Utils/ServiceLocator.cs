using System;
using System.Collections.Generic;
using System.Linq;

namespace BoshenCC.Core.Utils
{
    /// <summary>
    /// ������������ö��
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// ˲̬��ÿ�����󶼴�����ʵ��
        /// </summary>
        Transient,
        /// <summary>
        /// ����������Ӧ�ó�������������ֻ��һ��ʵ��
        /// </summary>
        Singleton,
        /// <summary>
        /// ���������ض����������ǵ���
        /// </summary>
        Scoped
    }

    /// <summary>
    /// �����ṩ�߽ӿ�
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// ��ȡ����
        /// </summary>
        T GetService<T>();
        /// <summary>
        /// ��ȡ����
        /// </summary>
        object GetService(Type serviceType);
    }

    /// <summary>
    /// ��ǿ�ķ���λ��ʵ�֣�֧������ע��
    /// </summary>
    public class ServiceLocator : IServiceProvider
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        /// <summary>
        /// ע��˲̬����
        /// </summary>
        public static void RegisterTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            // ��ʵ�֣���ʱʹ��ԭ�е�ע�᷽ʽ
            // ����������չΪ����������ע������
        }

        /// <summary>
        /// ע�ᵥ������
        /// </summary>
        public static void RegisterSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            // ��ʵ�֣���ʱʹ��ԭ�е�ע�᷽ʽ
            // ����������չΪ����������ע������
        }

        /// <summary>
        /// ע�ᵥ��ʵ��
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
        /// �����Է�����ע�������Ϊ������
        /// </summary>
        public static void Register<T>(T service)
        {
            RegisterSingleton(service);
        }

        /// <summary>
        /// ��ȡ����
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
        /// ��ȡ����
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
        /// IServiceProvider ʵ��
        /// </summary>
        T IServiceProvider.GetService<T>()
        {
            return GetService<T>();
        }

        /// <summary>
        /// IServiceProvider ʵ��
        /// </summary>
        object IServiceProvider.GetService(Type serviceType)
        {
            return GetService(serviceType);
        }

        /// <summary>
        /// �������Ƿ���ע��
        /// </summary>
        public static bool IsRegistered<T>()
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// �������Ƿ���ע��
        /// </summary>
        public static bool IsRegistered(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }

        /// <summary>
        /// ���������ע��ķ���
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }

        /// <summary>
        /// ��ȡ��ע��ķ�������
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
