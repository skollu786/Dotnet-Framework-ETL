﻿namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
#if !NETSTANDARD2_0
    using System.Windows.Data;
#endif

    #endregion NameSpaces

    public static class ChoTypeDescriptor
    {
        #region Constants

        private static readonly TypeConverter[] EmptyTypeConverters = new TypeConverter[] { };
        private static readonly object[] EmptyParams = new object[] { };

        #endregion Constants

        #region Shared Data Members (Private)

        private static readonly object _typeMemberTypeConverterCacheLockObject = new object();
        private static readonly Dictionary<MemberInfo, object[]> _typeMemberTypeConverterCache = new Dictionary<MemberInfo, object[]>();
        private static readonly Dictionary<Type, object[]> _typeTypeConverterCache = new Dictionary<Type, object[]>();

        private static readonly Dictionary<MemberInfo, object[]> _typeMemberTypeConverterParamsCache = new Dictionary<MemberInfo, object[]>();
        private static readonly Dictionary<Type, object[]> _typeTypeConverterParamsCache = new Dictionary<Type, object[]>();
     
        private static readonly Dictionary<MemberInfo, object[]> _typeMemberGlobalTypeConverterParamsCache = new Dictionary<MemberInfo, object[]>();

        private static readonly object _disableImplicitOpsTypeLockObject = new object();
        private static readonly Dictionary<Type, bool> _disableImplicitOpsTypeCache = new Dictionary<Type, bool>();

        public static bool DoNotUseTypeConverterForTypes = false;
        public static bool DoNotDiscoverTypeConverterForTypes = false;
        public static bool DoNotDiscoverTypeConverterForTypesByBaseType = false;

        #endregion Shared Data Members (Private)

        #region Shared Members (Public)

        #region GetTypeConverters Overloads (Public)

        public static T GetTypeAttribute<T>(Type type)
            where T : Attribute
        {
            if (type == null)
                throw new NullReferenceException("type");
            return TypeDescriptor.GetAttributes(type).OfType<T>().FirstOrDefault();
        }

        public static IEnumerable<T> GetTypeAttributes<T>(Type type)
            where T : Attribute
        {
            if (type == null)
                throw new NullReferenceException("type");
            return TypeDescriptor.GetAttributes(type).OfType<T>();
        }

        public static AttributeCollection GetTypeAttributes(Type type)
        {
            if (type == null)
                throw new NullReferenceException("type");
            return TypeDescriptor.GetAttributes(type);
        }

        public static Attribute GetTypeAttribute(Type type, Type attributeType)
        {
            if (type == null)
                throw new NullReferenceException("type");
            if (attributeType == null)
                throw new NullReferenceException("interfaceType");
            foreach (Attribute attribute in TypeDescriptor.GetAttributes(type))
            {
                if (attributeType.IsAssignableFrom(attribute.GetType()))
                    return attribute;
            }

            return null;
        }

        public static IEnumerable<PropertyDescriptor> GetAllProperties(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            lock (_pdDictLock)
            {
                return _pdDict[type];
            }
        }

        private static readonly Dictionary<Type, PropertyDescriptor[]> _pdDict = new Dictionary<Type, PropertyDescriptor[]>();
        private static readonly object _pdDictLock = new object();
        private static void Init(Type type)
        {
            lock (_pdDictLock)
            {
                if (!_pdDict.ContainsKey(type))
                {
                    if (type.IsSimple()
                        || type == typeof(object)
                        || typeof(Type).IsAssignableFrom(type)
                        )
                        _pdDict.Add(type, new PropertyDescriptor[] { });
                    else
                        _pdDict.Add(type, GetBasePropertiesFirst(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd => !pd.Attributes.OfType<ChoIgnoreMemberAttribute>().Any()).ToArray());
                }
            }
        }

        private static PropertyDescriptorCollection GetBasePropertiesFirst(Type type)
        {
            var orderList = new List<Type>();
            var iteratingType = type;
            do
            {
                orderList.Insert(0, iteratingType);
                iteratingType = iteratingType.BaseType;
            } while (iteratingType != null);

            var props = TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>()
                .OrderBy(x => orderList.IndexOf(x.ComponentType))
                .ToArray();

            var map = props.GroupBy(p => p.GetType())
                .Select(g => new { g.Key, Values = g.ToArray() }).ToArray();
                
            return new PropertyDescriptorCollection(map.FirstOrDefault()?.Values);
        }

        public static IEnumerable<PropertyDescriptor> GetProperties<T>(Type type)
            where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            Init(type);

            lock (_pdDictLock)
            {
                return _pdDict[type].Where(pd => pd.Attributes.OfType<T>().Any());
            }
            //return _pdDict[type].Where(pd => pd.Attributes.OfType<T>().Any());
        }

        public static IEnumerable<PropertyDescriptor> GetProperties(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            Init(type);

            lock (_pdDictLock)
            {
                return _pdDict[type];
            }
            //return _pdDict[type];
        }

        public static PropertyDescriptor GetProperty<T>(Type type, string propName)
            where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");
            Init(type);

            lock (_pdDictLock)
            {
                return _pdDict[type].Where(pd =>
                pd.Name == propName && pd.Attributes.OfType<T>().Any()).FirstOrDefault();
            }

            //return _pdDict[type].Where(pd =>
            //    pd.Name == propName && pd.Attributes.OfType<T>().Any()).FirstOrDefault();
        }

        public static PropertyDescriptor GetProperty(Type type, string propName)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");
            Init(type);

            lock (_pdDictLock)
            {
                if (propName.Contains("."))
                {
                    PropertyDescriptor pd = null;
                    Type subType = type;
                    foreach (var mn in propName.SplitNTrim(".").Where(m => !m.IsNullOrWhiteSpace()))
                    {
                        pd = GetAllPropetiesForType(subType).Where(p => p.Name == mn).FirstOrDefault();
                        if (pd == null || subType == null)
                            break;
                        subType = pd.PropertyType.GetUnderlyingType().GetItemType();
                    }

                    return pd;
                }
                else
                    return GetAllPropetiesForType(type).Where(pd => pd.Name == propName).FirstOrDefault();
            }
            //return _pdDict[type].Where(pd =>
            //    pd.Name == propName).FirstOrDefault();
        }

        private static PropertyDescriptor[] GetAllPropetiesForType(Type type)
        {
            Init(type);
            return _pdDict[type];
        }
        public static PropertyDescriptor GetNestedProperty(Type recType, string pn)
        {
            if (pn.IsNullOrWhiteSpace()) return null;

            string[] pnTokens = pn.SplitNTrim(".");
            PropertyDescriptor pd = null;
            for (int index = 0; index < pnTokens.Length - 1; index++)
            {
                pd = ChoTypeDescriptor.GetProperty(recType, pnTokens[index]);
                if (pd == null) return null;

                recType = pd.PropertyType;
            }

            return ChoTypeDescriptor.GetProperty(recType, pnTokens.Last());
        }

        public static T GetPropetyAttribute<T>(PropertyDescriptor pd)
                   where T : Attribute
        {
            if (pd == null)
                return null;
            else
                return pd.Attributes.OfType<T>().FirstOrDefault();
        }

        public static IEnumerable<T> GetPropetyAttributes<T>(PropertyDescriptor pd)
                   where T : Attribute
        {
            if (pd == null)
                return new T[] { };
            else
            {
                return pd.Attributes.OfType<T>();
                //return pd.Attributes.OfType<Attribute>().Where(a => a.GetType() == typeof(T)).OfType<T>();
            }
        }

        public static T GetPropetyAttribute<T>(Type type, string propName)
                   where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");
            Init(type);

            lock (_pdDictLock)
            {
                PropertyDescriptor pd = _pdDict[type].Where(pd1 =>
                pd1.Name == propName && pd1.Attributes.OfType<T>().Any()).FirstOrDefault();
                if (pd == null)
                    return null;
                else
                    return GetPropetyAttribute<T>(pd);
            }
        }

        public static IEnumerable<T> GetPropetyAttributes<T>(Type type, string propName)
                   where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");
            Init(type);

            lock (_pdDictLock)
            {
                PropertyDescriptor pd = _pdDict[type].Where(pd1 =>
                pd1.Name == propName && pd1.Attributes.OfType<T>().Any()).FirstOrDefault();
                if (pd == null)
                    return Enumerable.Empty<T>();
                else
                    return GetPropetyAttributes<T>(pd);
            }
        }

        private static void RegisterConverters(MemberInfo memberInfo, TypeConverter[] typeConverters)
        {
            ChoGuard.ArgumentNotNull(memberInfo, "MemberInfo");

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (typeConverters == null)
                    typeConverters = EmptyTypeConverters;

                if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
                    _typeMemberTypeConverterCache[memberInfo] = typeConverters;
                else
                    _typeMemberTypeConverterCache.Add(memberInfo, typeConverters);
            }
        }

        public static void ClearConverters(MemberInfo memberInfo)
        {
            ChoGuard.ArgumentNotNull(memberInfo, "MemberInfo");

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
                    _typeMemberTypeConverterCache.Remove(memberInfo);
            }
        }

        #endregion GetTypeConverters Overloads (Public)

        #region GetTypeConverters Overloads (internal)

        public static object[] GetTypeConverterParams(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return EmptyParams;

            if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
            {
                if (_typeMemberTypeConverterCache[memberInfo] != EmptyTypeConverters)
                {
                    if (_typeMemberTypeConverterParamsCache.ContainsKey(memberInfo))
                        return _typeMemberTypeConverterParamsCache[memberInfo];
                }
            }

            if (_typeMemberGlobalTypeConverterParamsCache.ContainsKey(memberInfo))
                return _typeMemberGlobalTypeConverterParamsCache[memberInfo];

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (_typeMemberGlobalTypeConverterParamsCache.ContainsKey(memberInfo))
                    return _typeMemberGlobalTypeConverterParamsCache[memberInfo];

                foreach (var attribute in GetPropetyAttributes<ChoTypeConverterParamsAttribute>(memberInfo.ReflectedType, memberInfo.Name).Where(a => a.Parameters != null))
                {
                    _typeMemberGlobalTypeConverterParamsCache.Add(memberInfo, new object[] { attribute.ParametersObject });
                    return _typeMemberGlobalTypeConverterParamsCache[memberInfo];
                }
            }

            return EmptyParams;
        }

        public static object GetTypeConverter(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            object[] typeConverters = GetTypeConverters(memberInfo);
            if (typeConverters == null || typeConverters.Length == 0)
                return null;

            return typeConverters[0];
        }

        public static bool HasTypeConverters(MemberInfo memberInfo)
        {
            var conv = GetTypeConverters(memberInfo);
            if (conv == null)
                return false;

            return conv.Length > 0;
        }
        public static object[] GetTypeConverters(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            string mn = memberInfo.Name;
            object[] tcs = null;
            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (_typeMemberTypeConverterCache.TryGetValue(memberInfo, out tcs))
                {
                    if (tcs == EmptyTypeConverters)
                    {
                        Type memberType;
                        if (!ChoType.TryGetMemberType(memberInfo, out memberType) || (memberType == null /*|| memberType.IsSimple() */))
                            return null;
                        _typeTypeConverterCache.TryGetValue(memberType, out tcs);
                        return tcs;
                    }

                    return _typeMemberTypeConverterCache[memberInfo];
                }
                else
                {
                    if (!_typeMemberTypeConverterCache.ContainsKey(memberInfo))
                    {
                        Type typeConverterAttribute = typeof(ChoTypeConverterAttribute);

                        _typeMemberTypeConverterCache[memberInfo] = EmptyTypeConverters;
                        _typeMemberTypeConverterParamsCache[memberInfo] = EmptyParams;

                        int index = 0;
                        SortedList<int, object> queue = new SortedList<int, object>();
                        SortedList<int, object> paramsQueue = new SortedList<int, object>();
                        foreach (var attribute in GetPropetyAttributes<ChoTypeConverterAttribute>(memberInfo.ReflectedType, memberInfo.Name).Where(a => a.ConverterType != null))  //ChoType.GetMemberAttributesByBaseType(memberInfo, typeof(ChoTypeConverterAttribute)))
                        {
                            ChoTypeConverterAttribute converterAttribute = attribute;
                            if (converterAttribute != null)
                            {
                                if (converterAttribute.PriorityInternal == null)
                                {
                                    queue.Add(index, converterAttribute.CreateInstance());
                                    paramsQueue.Add(index, converterAttribute.ParametersObject);
                                    index++;
                                }
                                else
                                {
                                    queue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.CreateInstance());
                                    paramsQueue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.ParametersObject);
                                }
                            }
                        }

                        if (queue.Count == 0)
                        {
                            Type type = ChoType.GetMemberType(memberInfo);
                            if (ChoTypeConverter.Global.Contains(type))
                            {
                                _typeMemberTypeConverterCache[memberInfo] = (from a1 in ChoTypeConverter.Global.GetAll()
                                                                             where a1.Key == type
                                                                             select a1.Value).ToArray();
                            }
                        }

                        if (queue.Count > 0)
                        {
                            _typeMemberTypeConverterCache[memberInfo] = queue.Values.ToArray();
                            _typeMemberTypeConverterParamsCache[memberInfo] = paramsQueue.Values.ToArray();

                            return _typeMemberTypeConverterCache[memberInfo];
                        }

                        //if (queue.Count == 0 && !memberType.IsSimple())
                        //{
                        //    if (!_typeTypeConverterCache.ContainsKey(memberType))
                        //    {
                        //        Type[] types = ChoType.GetTypes(typeof(ChoTypeConverterAttribute)).Where(t => t.GetCustomAttribute<ChoTypeConverterAttribute>().ConverterType == memberType).ToArray();

                        //        if (types != null)
                        //        {
                        //            int index1 = 0;
                        //            SortedList<int, object> queue1 = new SortedList<int, object>();
                        //            SortedList<int, object[]> paramsQueue1 = new SortedList<int, object[]>();

                        //            foreach (Type t in types)
                        //            {
                        //                queue.Add(index1, Activator.CreateInstance(t));
                        //                index1++;
                        //            }
                        //            _typeTypeConverterCache.Add(memberType, queue.Values.ToArray());
                        //            return _typeTypeConverterCache[memberType];
                        //        }

                        //        TypeConverter converter = TypeDescriptor.GetConverter(memberType);
                        //        if (converter != null)
                        //            _typeTypeConverterCache.Add(memberType, new object[] { converter });
                        //        else
                        //            _typeTypeConverterCache.Add(memberType, EmptyTypeConverters);

                        //        _typeTypeConverterParamsCache.Add(memberType, EmptyParams);
                        //    }

                        //    return _typeTypeConverterCache[memberType];
                        //}
                    }

                    return _typeMemberTypeConverterCache.ContainsKey(memberInfo) ? _typeMemberTypeConverterCache[memberInfo] : EmptyTypeConverters;
                }
            }
        }

        public static object[] GetTypeConverters(PropertyDescriptor pd)
        {
            if (pd == null)
                return null;

            int index = 0;
            SortedList<int, object> queue = new SortedList<int, object>();
            SortedList<int, object> paramsQueue = new SortedList<int, object>();
            foreach (var attribute in GetPropetyAttributes<ChoTypeConverterAttribute>(pd).Where(a => a.ConverterType != null))
            {
                ChoTypeConverterAttribute converterAttribute = attribute;
                if (converterAttribute != null)
                {
                    if (converterAttribute.PriorityInternal == null)
                    {
                        queue.Add(index, converterAttribute.CreateInstance());
                        paramsQueue.Add(index, converterAttribute.ParametersObject);
                        index++;
                    }
                    else
                    {
                        queue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.CreateInstance());
                        paramsQueue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.ParametersObject);
                    }
                }
            }

            if (queue.Count == 0)
            {
                return GetTypeConvertersForType(pd.PropertyType);
            }

            if (queue.Count > 0)
                return queue.Values.ToArray();
            else
                return null;
        }
        public static object[] GetTypeConverterParams(PropertyDescriptor pd)
        {
            if (pd == null)
                return null;

            int index = 0;
            SortedList<int, object> queue = new SortedList<int, object>();
            SortedList<int, object> paramsQueue = new SortedList<int, object>();
            foreach (var attribute in GetPropetyAttributes<ChoTypeConverterAttribute>(pd).Where(a => a.ConverterType != null))
            {
                ChoTypeConverterAttribute converterAttribute = attribute;
                if (converterAttribute != null)
                {
                    if (converterAttribute.PriorityInternal == null)
                    {
                        queue.Add(index, converterAttribute.CreateInstance());
                        paramsQueue.Add(index, converterAttribute.ParametersObject);
                        index++;
                    }
                    else
                    {
                        queue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.CreateInstance());
                        paramsQueue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.ParametersObject);
                    }
                }
            }

            if (paramsQueue.Count > 0)
                return paramsQueue.Values.ToArray();
            else
                return null;
        }

        public static object[] GetTypeConvertersForType<T>()
        {
            return GetTypeConvertersForType(typeof(T));
        }

        public static object[] GetTypeConverterParamsForType(Type objType)
        {
            if (objType == null)
                return null;

            if (_typeMemberTypeConverterParamsCache.ContainsKey(objType))
                return _typeMemberTypeConverterParamsCache[objType];

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                var _ = GetTypeConvertersForType(objType);
                if (_typeMemberTypeConverterParamsCache.ContainsKey(objType))
                    return _typeMemberTypeConverterParamsCache[objType];
            }
            return EmptyParams;
        }

        public static object[] GetTypeConvertersForType(Type objType)
        {
            if (objType == null)
                return null;

            if (DoNotUseTypeConverterForTypes)
                return EmptyTypeConverters;

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (_typeTypeConverterCache.ContainsKey(objType))
                    return _typeTypeConverterCache[objType];
                else
                {
                    if (!DoNotDiscoverTypeConverterForTypesByBaseType)
                    {
                        var convs1 = (from a1 in _typeTypeConverterCache
                                      where a1.Key.IsAssignableFrom(objType)
                                      select a1.Value).FirstOrDefault();
                        if (!convs1.IsNullOrEmpty())
                        {
                            _typeTypeConverterCache[objType] = convs1;
                            return _typeTypeConverterCache[objType];
                        }
                    }

                    if (DoNotDiscoverTypeConverterForTypes)
                        return EmptyTypeConverters;

                    if (!_typeTypeConverterCache.ContainsKey(objType))
                    {
                        Type type = objType;
                        if (ChoTypeConverter.Global.Contains(type))
                        {
                            _typeTypeConverterCache[type] = (from a1 in ChoTypeConverter.Global.GetAll()
                                                             where a1.Key == type
                                                             select a1.Value).ToArray();
                            return _typeTypeConverterCache[type];
                        }
                        else if (!DoNotDiscoverTypeConverterForTypesByBaseType)
                        {
                            var convs = (from a1 in ChoTypeConverter.Global.GetAll()
                                         where a1.Key.IsAssignableFrom(type)
                                         select a1.Value).ToArray();
                            if (convs.Length > 0)
                            {
                                _typeTypeConverterCache[type] = convs;
                                return _typeTypeConverterCache[type];
                            }
                        }

                        Type[] types = ChoType.GetTypes(typeof(ChoTypeConverterAttribute)).Where(t => t.GetCustomAttribute<ChoTypeConverterAttribute>().ConverterType != null && t.GetCustomAttribute<ChoTypeConverterAttribute>().ConverterType.IsAssignableFrom(objType)).ToArray();

                        if (types != null)
                        {
                            int index1 = 0;
                            SortedList<int, object> queue1 = new SortedList<int, object>();
                            SortedList<int, object> paramsQueue1 = new SortedList<int, object>();
                            foreach (Type t in types)
                            {
                                var typeConvAttr = ChoType.GetAttribute<ChoTypeConverterAttribute>(t);

                                queue1.Add(index1, ChoActivator.CreateInstance(t));
                                if (typeConvAttr.ParametersObject != null)
                                    paramsQueue1.Add(index1, typeConvAttr.ParametersObject);
                                index1++;
                            }
                            _typeTypeConverterCache.Add(objType, queue1.Values.ToArray());
                            _typeTypeConverterParamsCache.Add(objType, paramsQueue1.Values.ToArray());
                            return _typeTypeConverterCache[objType];
                        }

                        TypeConverter converter = TypeDescriptor.GetConverter(objType);
                        if (converter != null)
                            _typeTypeConverterCache.Add(objType, new object[] { converter });
                        else
                            _typeTypeConverterCache.Add(objType, EmptyTypeConverters);

                        _typeTypeConverterParamsCache.Add(objType, EmptyParams);
                    }

                    return _typeTypeConverterCache[objType];
                }
            }
        }
        public static void ClearTypeConvertersForType<T>()
        {
            ClearTypeConvertersForType(typeof(T));
        }

        public static void ClearTypeConvertersForType(Type objType)
        {
            if (objType == null)
                return;
            if (!_typeTypeConverterCache.ContainsKey(objType))
                return;

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (!_typeTypeConverterCache.ContainsKey(objType))
                    return;

                _typeTypeConverterCache.Remove(objType);
            }
        }

        public static void RegisterTypeConvertersForType<T, TConv>()
        {
            RegisterTypeConvertersForType(typeof(T), new object[] { ChoActivator.CreateInstance<TConv>() });
        }

        public static void RegisterTypeConvertersForType(Type objType, object[] converters)
        {
            if (objType == null)
                return;
            if (converters == null)
                converters = new object[] { };

            object[] choConvs = new object[] { };
            object[] netConvs = new object[] { };

            choConvs = converters.OfType<IChoValueConverter>().ToArray();
#if !NETSTANDARD2_0
            netConvs = converters.OfType<IValueConverter>().ToArray();
#endif
            converters = ChoArray.Combine<object>(choConvs, netConvs);

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (!_typeTypeConverterCache.ContainsKey(objType))
                    _typeTypeConverterCache.Add(objType, converters);
                else
                    _typeTypeConverterCache[objType] = ChoArray.Combine<object>(_typeTypeConverterCache[objType], converters);
            }
        }

        public static void RegisterTypeConverter(Type type)
        {
#if !NETSTANDARD2_0
            if (typeof(IValueConverter).IsAssignableFrom(type))
            {
                var attr = ChoType.GetCustomAttribute<ChoSourceTypeAttribute>(type, true);
                if (attr == null || attr.Type == null) return;
                RegisterTypeConverterForTypeInternal(attr.Type, ChoActivator.CreateInstance(type));
                return;
            }
#endif
            if (typeof(IChoValueConverter).IsAssignableFrom(type))
            {
                var attr = ChoType.GetCustomAttribute<ChoSourceTypeAttribute>(type, true);
                if (attr == null || attr.Type == null) return;
                RegisterTypeConverterForTypeInternal(attr.Type, ChoActivator.CreateInstance(type));
            }
        }

        public static void RegisterTypeConverter<T>()
        {
            RegisterTypeConverter(typeof(T));
        }

#if !NETSTANDARD2_0
        public static void RegisterTypeConverterForType<T>(IValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(typeof(T), (object)converter);
        }
#endif

        public static void RegisterTypeConverterForType<T>(IChoValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(typeof(T), (object)converter);
        }
#if !NETSTANDARD2_0

        public static void RegisterTypeConverterForType(Type objType, IValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(objType, (object)converter);
        }
#endif
        public static void RegisterTypeConverterForType(Type objType, IChoValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(objType, (object)converter);
        }

        private static void RegisterTypeConverterForTypeInternal(Type objType, object converter)
        {
            if (objType == null)
                return;
            if (converter == null)
                return;

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (!_typeTypeConverterCache.ContainsKey(objType))
                    _typeTypeConverterCache.Add(objType, new object[] { converter });
                else
                    _typeTypeConverterCache[objType] = _typeTypeConverterCache[objType].Union(new object[] { converter }).ToArray();
            }
        }

        #endregion GetTypeConverter Overloads (internal)

        #region GetCustomSerializer Overloads

        public static object GetCustomSerializer(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            object[] typeConverters = GetCustomSerializers(memberInfo);
            if (typeConverters == null || typeConverters.Length == 0)
                return null;

            return typeConverters[0];
        }


        public static object[] GetCustomSerializers(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            object[] tcs = null;
            Type typeConverterAttribute = typeof(ChoCustomSerializerAttribute);

            int index = 0;
            SortedList<int, object> queue = new SortedList<int, object>();
            SortedList<int, object> paramsQueue = new SortedList<int, object>();
            foreach (Attribute attribute in GetPropetyAttributes<ChoCustomSerializerAttribute>(memberInfo.ReflectedType, memberInfo.Name))  //ChoType.GetMemberAttributesByBaseType(memberInfo, typeof(ChoCustomSerializerAttribute)))
            {
                ChoCustomSerializerAttribute converterAttribute = (ChoCustomSerializerAttribute)attribute;
                if (converterAttribute != null)
                {
                    if (converterAttribute.PriorityInternal == null)
                    {
                        queue.Add(index, converterAttribute.CreateInstance());
                        paramsQueue.Add(index, converterAttribute.ParametersObject);
                        index++;
                    }
                    else
                    {
                        queue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.CreateInstance());
                        paramsQueue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.ParametersObject);
                    }
                }
            }


            if (queue.Count > 0)
                return queue.Values.ToArray();

            return EmptyTypeConverters;
        }

        public static object GetCustomSerializerParams(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            object[] typeConverters = GetCustomSerializersParams(memberInfo);
            if (typeConverters == null || typeConverters.Length == 0)
                return null;

            return typeConverters[0];
        }


        public static object[] GetCustomSerializersParams(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            object[] tcs = null;
            Type typeConverterAttribute = typeof(ChoCustomSerializerAttribute);

            int index = 0;
            SortedList<int, object> queue = new SortedList<int, object>();
            SortedList<int, object> paramsQueue = new SortedList<int, object>();
            foreach (Attribute attribute in GetPropetyAttributes<ChoCustomSerializerAttribute>(memberInfo.ReflectedType, memberInfo.Name))  //ChoType.GetMemberAttributesByBaseType(memberInfo, typeof(ChoCustomSerializerAttribute)))
            {
                ChoCustomSerializerAttribute converterAttribute = (ChoCustomSerializerAttribute)attribute;
                if (converterAttribute != null)
                {
                    if (converterAttribute.PriorityInternal == null)
                    {
                        queue.Add(index, converterAttribute.CreateInstance());
                        paramsQueue.Add(index, converterAttribute.ParametersObject);
                        index++;
                    }
                    else
                    {
                        queue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.CreateInstance());
                        paramsQueue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.ParametersObject);
                    }
                }
            }


            if (queue.Count > 0)
                return paramsQueue.Values.ToArray();

            return EmptyParams;
        }

        #endregion

        public static bool IsTurnedOffImplicitOpsOnType(Type type)
        {
            if (type == null)
                return true;

            lock (_disableImplicitOpsTypeLockObject)
            {
                if (_disableImplicitOpsTypeCache.ContainsKey(type))
                    return _disableImplicitOpsTypeCache[type];

                if (type.IsValueType)
                    return true;
            }

            return true;
        }

        public static void TurnOffImplicitOpsOnType(Type type, bool flag)
        {
            if (type == null)
                return;

            lock (_disableImplicitOpsTypeLockObject)
            {
                if (_disableImplicitOpsTypeCache.ContainsKey(type))
                    _disableImplicitOpsTypeCache[type] = flag;
                else
                    _disableImplicitOpsTypeCache.Add(type, flag);
            }

        }

        public static void TurnOffImplicitOpsOnType<T>(bool flag)
        {
            Type type = typeof(T);
            TurnOffImplicitOpsOnType(type, flag);
        }

        #endregion Shared Members (Public)
    }
}
