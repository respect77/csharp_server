using Microsoft.Extensions.ObjectPool;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

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
            pool.Return(obj);
        }
    }
    public static class CustomObjectPool
    {
        public static void Dispose<T>(T obj) where T : class, new() => CustomObjectPool<T>.Instance.Dispose(obj);
        public static T Get<T>(T? _) where T : class, new() => CustomObjectPool<T>.Get();
    }

    class CachedObjectPropertyGroupInfo
    {
        public List<PropertyInfo> Lists { get; set; } = new();
        public List<PropertyInfo> Dics { get; set; } = new();
        public List<PropertyInfo> HashSets { get; set; } = new();
        public List<PropertyInfo> Objects { get; set; } = new();
        public CachedObjectPropertyGroupInfo()
        { }
    }

    enum CachedPropertyTypeEnum
    {
        None,
        List,
        Dic,
        HashSet,
        Object,
    }

    public class ObjectPoolDisposeHelper
    {
        private readonly static Lazy<ObjectPoolDisposeHelper> _instance = new(() => new ObjectPoolDisposeHelper());

        private readonly ConcurrentDictionary<string, CachedObjectPropertyGroupInfo> CachedObjectProperty = new();
        private readonly ConcurrentDictionary<string, CachedPropertyTypeEnum> CachedPropertyType = new();
        public static ObjectPoolDisposeHelper Instance => _instance.Value;
        private ObjectPoolDisposeHelper()
        {
        }
        public void DisposeClass<T>(T? obj, bool from_obj = false) where T : class, new()
        {
            if (obj == null)
            {
                return;
            }

            Type type = obj.GetType();

            if (!CachedObjectProperty.TryGetValue(type.Name, out var property_group))
            {
                property_group = new();
                foreach (var property in type.GetProperties())
                {
                    var value = property.GetValue(obj);

                    switch (value)
                    {
                        case var _ when value is IList:
                            property_group.Lists.Add(property);
                            break;
                        case var _ when value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>):
                            property_group.Dics.Add(property);
                            break;
                        case var _ when value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(HashSet<>):
                            property_group.HashSets.Add(property);
                            break;
                        case var _ when value != null && value.GetType() != typeof(string) && !value.GetType().IsValueType:
                            property_group.Objects.Add(property);
                            break;
                        default:
                            //LogManager.Instance.Debug($"{property.Name} Else");
                            break;
                    }
                }
                CachedObjectProperty.TryAdd(type.Name, property_group);
            }

            foreach (var property in property_group.Lists)
            {
                DisposeList(property.GetValue(obj), property.Name);
            }

            foreach (var property in property_group.Dics)
            {
                DisposeDictionary(property.GetValue(obj), property.Name);
            }

            foreach (var property in property_group.HashSets)
            {
                DisposeSet(property.GetValue(obj), property.Name);

            }

            foreach (var property in property_group.Objects)
            {
                var value = property.GetValue(obj);
                DisposeClass(value);
            }

            if (!from_obj)
            {
                CustomObjectPool.Dispose(obj);
            }
        }

        private void DisposeList(object? value, string name)
        {
            if (value is IList list_value)
            {
                CachedPropertyTypeEnum property_type = CachedPropertyTypeEnum.None;
                foreach (var list_v in list_value)
                {
                    if (!DisposeExec(list_v, name, ref property_type))
                    {
                        break;
                    }
                }
                list_value.Clear();
            }
        }

        private void DisposeDictionary(object? value, string name)
        {
            if (value is IDictionary dic_value)
            {
                CachedPropertyTypeEnum property_type = CachedPropertyTypeEnum.None;
                foreach (var dic_v in dic_value.Values)
                {
                    if (!DisposeExec(dic_v, name, ref property_type))
                    {
                        break;
                    }
                }
                dic_value.Clear();
            }
        }

        private void DisposeSet(dynamic? set_value, string name)
        {
            if (set_value != null)
            {
                CachedPropertyTypeEnum property_type = CachedPropertyTypeEnum.None;
                foreach (var set_v in set_value)
                {
                    if (!DisposeExec(set_v, name, ref property_type))
                    {
                        break;
                    }
                }
                set_value.Clear();
            }
        }

        private CachedPropertyTypeEnum GetPropertyType(object? value, string name)
        {
            if (!CachedPropertyType.TryGetValue(name, out var property_type))
            {
                switch (value)
                {
                    case var _ when value is IList:
                        property_type = CachedPropertyTypeEnum.List;
                        break;
                    case var _ when value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>):
                        property_type = CachedPropertyTypeEnum.Dic;
                        break;
                    case var _ when value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(HashSet<>):
                        property_type = CachedPropertyTypeEnum.HashSet;
                        break;
                    case var _ when value != null && value.GetType() != typeof(string) && !value.GetType().IsValueType:
                        property_type = CachedPropertyTypeEnum.Object;
                        break;
                    default:
                        property_type = CachedPropertyTypeEnum.None;
                        break;
                }
                CachedPropertyType.TryAdd(name, property_type);
            }
            return property_type;
        }

        private bool DisposeExec(object? value, string from_name, ref CachedPropertyTypeEnum property_type)
        {
            if (value == null)
            {
                return true;
            }

            Type type = value.GetType();
            string name = $"{from_name} {type.Name}";

            if (property_type == CachedPropertyTypeEnum.None)
            {
                property_type = GetPropertyType(value, name);
            }

            switch (property_type)
            {
                case CachedPropertyTypeEnum.List:
                    DisposeList(value, name);
                    break;
                case CachedPropertyTypeEnum.Dic:
                    DisposeDictionary(value, name);
                    break;
                case CachedPropertyTypeEnum.HashSet:
                    DisposeSet(value, name);

                    break;
                case CachedPropertyTypeEnum.Object:
                    DisposeClass(value);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
