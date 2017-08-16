﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Castle.MicroKernel.Registration;
using MiniAbp.DataAccess;
using MiniAbp.Domain;
using MiniAbp.Domain.Entitys;

namespace MiniAbp.Extension
{
    public static class ObjectExtension
    {
        public static TDestination MapTo<TDestination>(this object source) where TDestination : class, new()
        {
            var targetInstance = new TDestination();
            if (IsEnumerableType(source.GetType()))
            {
                MapToGenericEnumerable(source, targetInstance);
            }
            else
            {
                MapToSimpleObject(source, targetInstance);
            }
            return targetInstance;
        }
        /// <summary>
        /// Execute a mapping from the source object to the existing destination object
        /// There must be a mapping between objects before calling this method.
        /// 
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam><typeparam name="TDestination">Destination type</typeparam><param name="source">Source object</param><param name="destination">Destination object</param>
        /// <returns/>
        public static TDestination MapTo<TSource, TDestination>(this TSource source, TDestination destination) where TDestination : class, new()
        {
            if (IsEnumerableType(typeof(TSource)))
            {
                throw new ArgumentException("This method can't support class which inherit IEnumberable`1");
            }
            if(destination == null) destination = new TDestination();
            MapToSimpleObject(source, destination);
            return destination;
        }

        /// <summary>
        /// Does class implement IEnumerable
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsEnumerableType(Type type)
        {
            var hasInterface = type.GetInterface("IEnumerable`1");
            return hasInterface != null;
        }

        /// <summary>
        /// Map to generic type which inherit IEnumerable
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        private static void MapToGenericEnumerable<TSource, TDestination>(this TSource source, TDestination destination)
            where TDestination : class, new()
        {
            var sourceType = source.GetType();
            var destType = destination.GetType();
            var sourceHasInterface = sourceType.GetInterface("IEnumerable`1");
            var targetHasInterface = destType.GetInterface("IEnumerable`1");
            if (sourceHasInterface != null && targetHasInterface != null && sourceType.IsGenericType)
            {
                int count = Convert.ToInt32(sourceType.GetProperty("Count").GetValue(source, null));
                for (int i = 0; i < count; i++)
                {
                    var itemValue = sourceType.GetProperty("Item").GetValue(source, new object[] {i});
                    var destGenericType = destType.GenericTypeArguments[0];
                    var destInstance = destGenericType.Assembly.CreateInstance(destGenericType.FullName);
                    MapToSimpleObject(itemValue, destInstance);
                    var methodAdd = destType.GetMethod("Add");
                    methodAdd.Invoke(destination, new object[] {destInstance});
                }
            }
        }
        /// <summary>
        /// Map to simple object
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private static void MapToSimpleObject<TSource, TDestination>(this TSource source, TDestination destination)
            where TDestination : class, new()
        {
            var sourceType = source.GetType();
            var sourceProps = sourceType.GetProperties();
            var targetType = destination.GetType();
            var targetProps = targetType.GetProperties();
            foreach (var p in sourceProps)
            {
                var targetValueType = targetProps.FirstOrDefault(r => r.Name == p.Name);
                if (targetValueType != null)
                {
                    var valueType = sourceType.GetProperty(p.Name);
                    var value = valueType.GetValue(source);
                    if(value == null)
                        continue;
                    var preSetValue = value;
                    //only support simple object type not support collection
                    if (!valueType.PropertyType.IsValueType && valueType.PropertyType != typeof (String))
                    {
                        var targetPropType = targetValueType.PropertyType;
                        var targetPropInstance = targetPropType.Assembly.CreateInstance(targetPropType.FullName);
                        if (IsEnumerableType(source.GetType()))
                        {
                            MapToGenericEnumerable(source, targetPropInstance);
                        }
                        else
                        {
                            MapToSimpleObject(source, targetPropInstance);
                        }
                        preSetValue = targetPropInstance;
                    }
                    targetValueType.SetValue(destination, preSetValue);
                }
            }
        }

        /// <summary>
        /// 输出文件
        /// </summary>
        /// <param name="service"></param>
        /// <param name="strem"></param>
        /// <param name="downloadName"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static FileStreamOutput File(this ApplicationService service, Stream strem, string downloadName, string contentType)
        {
            return new FileStreamOutput(strem, downloadName, contentType);
        }

        /// <summary>
        /// 输出文件
        /// </summary>
        /// <param name="service"></param>
        /// <param name="filePath"></param>
        /// <param name="downloadName"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static FilePathOutput File(this ApplicationService service, string filePath, string downloadName, string contentType)
        {
            return new FilePathOutput(filePath, downloadName, contentType);
        }
    }
}
