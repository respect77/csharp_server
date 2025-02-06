using Common;
using Microsoft.Extensions.ObjectPool;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace TcpServer.Common
{
    public class CustomObjectPool<T> where T : class, new()
    {
        private readonly static Lazy<CustomObjectPool<T>> _instance = new(() => new CustomObjectPool<T>());
        private readonly DefaultObjectPoolProvider _provider = new();
        private readonly ObjectPool<T> _pool;

        private CustomObjectPool()
        {
            _pool = _provider.Create<T>();
        }
        public static CustomObjectPool<T> Instance => _instance.Value;

        public static T Get() => Instance._pool.Get();

        public void Dispose(T obj)
        {
            _pool.Return(obj);
        }
    }
    public static class CustomObjectPool
    {
        public static void Dispose<T>(T obj) where T : class, new() => CustomObjectPool<T>.Instance.Dispose(obj);
        public static T Get<T>(T? _) where T : class, new() => CustomObjectPool<T>.Get();
    }

    class CachedObjectMemberInfo
    {
        public List<MemberInfo> Lists { get; set; } = new();
        public List<MemberInfo> Dics { get; set; } = new();
        public List<MemberInfo> HashSets { get; set; } = new();
        public List<MemberInfo> Objects { get; set; } = new();
        public CachedObjectMemberInfo()
        { }
    }

    enum CachedMemberTypeEnum
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

        private ConcurrentDictionary<string, CachedObjectMemberInfo> _cachedObjectMemberGroup = new();
        public static ObjectPoolDisposeHelper Instance => _instance.Value;
        private ObjectPoolDisposeHelper()
        {
        }
        CachedMemberTypeEnum GetType(Type type)
        {
            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition();

                if (genericType == typeof(List<>))
                {
                    return CachedMemberTypeEnum.List;
                }
                else if (genericType == typeof(Dictionary<,>))
                {
                    return CachedMemberTypeEnum.Dic;
                }
                else if (genericType == typeof(HashSet<>))
                {
                    return CachedMemberTypeEnum.HashSet;
                }
                else
                {
                    LogManager.Instance.Debug($"Unhandled generic type: {genericType} {type.Name}");
                    return CachedMemberTypeEnum.None;
                }
            }
            else
            {
                if (type != typeof(string) && !type.IsValueType)
                {
                    return CachedMemberTypeEnum.Object;
                }
                else
                {
                    return CachedMemberTypeEnum.None;
                }
            }
        }
        public void DisposeClass<T>(T? obj, bool from_obj = false) where T : class, new()
        {
            if (obj == null)
            {
                return;
            }

            Type type = obj.GetType();

            if (!_cachedObjectMemberGroup.TryGetValue(type.Name, out var member_group))
            {
                member_group = new();

                var MemberAddExec = (MemberInfo member, Type member_type) =>
                {
                    var type = GetType(member_type);
                    switch (type)
                    {
                        case CachedMemberTypeEnum.List:
                            member_group.Lists.Add(member);
                            break;
                        case CachedMemberTypeEnum.Dic:
                            member_group.Dics.Add(member);
                            break;
                        case CachedMemberTypeEnum.HashSet:
                            member_group.HashSets.Add(member);
                            break;
                        case CachedMemberTypeEnum.Object:
                            member_group.Objects.Add(member);
                            break;
                    }
                };

                foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.Name.Contains("k__BackingField"))
                    {
                        continue;
                    }
                    MemberAddExec(field, field.FieldType);
                }
                foreach (var property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    MemberAddExec(property, property.PropertyType);
                }

                _cachedObjectMemberGroup.TryAdd(type.Name, member_group);
            }

            var GetValue = (MemberInfo member) =>
            {
                return member switch
                {
                    PropertyInfo property => property.GetValue(obj),
                    FieldInfo field => field.GetValue(obj),
                    _ => null,
                };
            };

            foreach (var member in member_group.Lists)
            {
                DisposeList(GetValue(member), member.Name);
            }

            foreach (var member in member_group.Dics)
            {
                DisposeDictionary(GetValue(member), member.Name);
            }

            foreach (var member in member_group.HashSets)
            {
                DisposeSet(GetValue(member), member.Name);
            }

            foreach (var member in member_group.Objects)
            {
                var value = GetValue(member);
                DisposeClass(value);
            }

            if (!from_obj)
            {
                CustomObjectPool.Dispose(obj);
            }
        }

        private void DisposeList(object? value, string name)
        {
            if (value == null)
            {
                return;
            }
            if (value is IList list_value)
            {
                var member_type = CachedMemberTypeEnum.None;
                foreach (var list_v in list_value)
                {
                    if (!DisposeLoopExec(list_v, name, ref member_type))
                    {
                        break;
                    }
                }
                list_value.Clear();
            }
        }

        private void DisposeDictionary(object? value, string name)
        {
            if (value == null)
            {
                return;
            }
            if (value is IDictionary dic_value)
            {
                var property_type = CachedMemberTypeEnum.None;
                foreach (var dic_v in dic_value.Values)
                {
                    if (!DisposeLoopExec(dic_v, name, ref property_type))
                    {
                        break;
                    }
                }
                dic_value.Clear();
            }
        }

        private void DisposeSet(dynamic? value, string name)
        {
            if (value == null)
            {
                return;
            }
            var property_type = CachedMemberTypeEnum.None;
            foreach (var set_v in value)
            {
                if (!DisposeLoopExec(set_v, name, ref property_type))
                {
                    break;
                }
            }
            value.Clear();
        }

        private bool DisposeLoopExec(object? value, string from_name, ref CachedMemberTypeEnum member_type)
        {
            if (value == null)
            {
                return true;
            }

            Type type = value.GetType();
            string name = $"{from_name} {type.Name}";

            if (member_type == CachedMemberTypeEnum.None)
            {
                //member_type = GetPropertyType(value, name);
                member_type = GetType(type);
            }

            switch (member_type)
            {
                case CachedMemberTypeEnum.List:
                    DisposeList(value, name);
                    break;
                case CachedMemberTypeEnum.Dic:
                    DisposeDictionary(value, name);
                    break;
                case CachedMemberTypeEnum.HashSet:
                    DisposeSet(value, name);
                    break;
                case CachedMemberTypeEnum.Object:
                    DisposeClass(value);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
