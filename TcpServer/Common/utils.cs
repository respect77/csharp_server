using Microsoft.Extensions.ObjectPool;
using System.Collections;

namespace TcpServer.Common
{
    public class CustomObjectPool<T> where T : class, new()
    {
        private readonly static Lazy<CustomObjectPool<T>> _instance = new(() => new CustomObjectPool<T>());

        private readonly DefaultObjectPoolProvider provider = new();
        private readonly ObjectPool<T> pool;

        private CustomObjectPool()
        {
            pool = provider.Create<T>();
        }
        public static CustomObjectPool<T> Instance => _instance.Value;

        public static T Get() => Instance.pool.Get();

        public void Dispose(T obj)
        {
            switch (obj)
            {
                case IDictionary dictionary:
                    dictionary.Clear();
                    break;
                case IList list:
                    list.Clear();
                    break;
            }
            pool.Return(obj);
        }
    }
    public static class CustomObjectPool
    {
        public static void Dispose<T>(T obj) where T : class, new() => CustomObjectPool<T>.Instance.Dispose(obj);
        public static T Get<T>(T? _) where T : class, new() => CustomObjectPool<T>.Get();
    }
}
