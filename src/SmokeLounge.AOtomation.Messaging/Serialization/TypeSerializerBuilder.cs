// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeSerializerBuilder.cs" company="SmokeLounge">
//   Copyright � 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TypeSerializerBuilder type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class TypeSerializerBuilder
    {
        #region Fields

        private readonly Lazy<PropertyMetaData[]> propertyMetas;

        private readonly Func<Type, ISerializer> serializerResolver;

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public TypeSerializerBuilder(Type type, Func<Type, ISerializer> serializerResolver)
        {
            this.type = type;
            this.serializerResolver = serializerResolver;
            this.propertyMetas = new Lazy<PropertyMetaData[]>(this.InitializePropertyMetas);
        }

        #endregion

        #region Public Methods and Operators

        public Expression BuildDeserializer(
            ParameterExpression streamReaderExpression, ParameterExpression serializationContextExpression)
        {
            var deserializedObject = Expression.Variable(this.type, "serializerVar");

            var createInstance = Expression.Assign(deserializedObject, Expression.New(this.type));
            var deserializationExpressions = new List<Expression> { createInstance };

            var propertyDeserializers = this.CreatePropertyDeserializers(
                streamReaderExpression, serializationContextExpression, deserializedObject);
            deserializationExpressions.AddRange(propertyDeserializers);

            Expression returnValue = deserializedObject;
            if (ReflectionHelper.IsStruct(this.type))
            {
                returnValue = Expression.Convert(returnValue, typeof(object));
            }

            var returnTarget = Expression.Label(typeof(object));
            var returnExpression = Expression.Return(returnTarget, returnValue, typeof(object));
            var returnLabel = Expression.Label(returnTarget, returnValue);

            deserializationExpressions.Add(returnExpression);
            deserializationExpressions.Add(returnLabel);

            var deserialization = Expression.Block(new[] { deserializedObject }, deserializationExpressions);
            var deserializationLambda =
                Expression.Lambda<Func<StreamReader, SerializationContext, object>>(
                    deserialization, streamReaderExpression, serializationContextExpression);
            return deserializationLambda;
        }

        public Expression BuildSerializer(
            ParameterExpression streamWriterExpression, ParameterExpression serializationContextExpression)
        {
            // if (type == typeof(CreateCharacterMessage) || type == typeof(CharacterListMessage))
            // {
            //     System.Diagnostics.Debugger.Break();
            // }
            var objectParam = Expression.Parameter(typeof(object), "object");
            var objectToSerialize = Expression.Variable(this.type, "serializerVar");

            var unboxObject = Expression.Assign(objectToSerialize, Expression.Convert(objectParam, this.type));
            var serializationExpressions = new List<Expression> { unboxObject };

            var propertySerializers = this.CreatePropertySerializers(
                streamWriterExpression, serializationContextExpression, objectToSerialize);
            serializationExpressions.AddRange(propertySerializers);

            var serialization = Expression.Block(new[] { objectToSerialize }, serializationExpressions);
            var serializationLambda =
                Expression.Lambda<Action<StreamWriter, SerializationContext, object>>(
                    serialization, streamWriterExpression, serializationContextExpression, objectParam);
            return serializationLambda;
        }

        #endregion

        #region Methods

        private Expression CreatePropertyDeserializer(
            ParameterExpression streamReaderExpression, 
            ParameterExpression serializationContextExpression, 
            PropertyMetaData propertyMetaData, 
            ParameterExpression deserializedObject)
        {
            Expression propertyExpression = Expression.Property(deserializedObject, propertyMetaData.Property);

            if (propertyMetaData.UsesFlagsAttributes.Any())
            {
                var serializeMethodInfo =
                    ReflectionHelper.GetMethodInfo<SerializationContext, Func<StreamReader, PropertyMetaData, object>>(
                        o => o.Deserialize);
                var args = new Expression[]
                               {
                                   streamReaderExpression, Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                               };
                var callSerializeExpression = Expression.Call(serializationContextExpression, serializeMethodInfo, args);
                return Expression.Assign(
                    propertyExpression, Expression.Convert(callSerializeExpression, propertyMetaData.Type));
            }

            var propertySerializer = this.serializerResolver(propertyMetaData.Type);
            var deserializerExpression = propertySerializer.DeserializerExpression(
                streamReaderExpression, serializationContextExpression, propertyExpression, propertyMetaData);
            return deserializerExpression;
        }

        private IEnumerable<Expression> CreatePropertyDeserializers(
            ParameterExpression streamReaderExpression, 
            ParameterExpression serializationContextExpression, 
            ParameterExpression deserializedObject)
        {
            foreach (var propertyMeta in this.propertyMetas.Value)
            {
                var deserializerExpression = this.CreatePropertyDeserializer(
                    streamReaderExpression, serializationContextExpression, propertyMeta, deserializedObject);
                yield return deserializerExpression;

                if (propertyMeta.FlagsAttribute == null)
                {
                    continue;
                }

                Expression propertyExpression = Expression.Property(deserializedObject, propertyMeta.Property);
                var setFlagValueMethodInfo =
                    ReflectionHelper.GetMethodInfo<SerializationContext, Action<string, int>>(o => o.SetFlagValue);
                var setFlagExpression = Expression.Call(
                    serializationContextExpression, 
                    setFlagValueMethodInfo, 
                    Expression.Constant(propertyMeta.FlagsAttribute.Flag, typeof(string)), 
                    Expression.Convert(propertyExpression, typeof(int)));
                yield return setFlagExpression;
            }
        }

        private Expression CreatePropertySerializer(
            ParameterExpression streamWriterExpression, 
            ParameterExpression serializationContextExpression, 
            PropertyMetaData propertyMetaData, 
            ParameterExpression objectToSerialize)
        {
            Expression propertyExpression = Expression.Property(objectToSerialize, propertyMetaData.Property);
            if (ReflectionHelper.IsStruct(propertyMetaData.Type))
            {
                propertyExpression = Expression.Convert(propertyExpression, typeof(object));
            }

            if (propertyMetaData.UsesFlagsAttributes.Any())
            {
                var serializeMethodInfo =
                    ReflectionHelper.GetMethodInfo<SerializationContext, Action<StreamWriter, object, PropertyMetaData>>
                        (o => o.Serialize);
                var args = new Expression[]
                               {
                                   streamWriterExpression, Expression.Convert(propertyExpression, typeof(object)), 
                                   Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                               };
                var callSerializeExpression = Expression.Call(serializationContextExpression, serializeMethodInfo, args);
                return callSerializeExpression;
            }

            var propertySerializer = this.serializerResolver(propertyMetaData.Type);
            var serializerExpression = propertySerializer.SerializerExpression(
                streamWriterExpression, serializationContextExpression, propertyExpression, propertyMetaData);

            return serializerExpression;
        }

        private IEnumerable<Expression> CreatePropertySerializers(
            ParameterExpression streamWriterExpression, 
            ParameterExpression serializationContextExpression, 
            ParameterExpression objectToSerialize)
        {
            foreach (var propertyMeta in this.propertyMetas.Value)
            {
                if (propertyMeta.FlagsAttribute != null)
                {
                    Expression propertyExpression = Expression.Property(objectToSerialize, propertyMeta.Property);
                    var setFlagValueMethodInfo =
                        ReflectionHelper.GetMethodInfo<SerializationContext, Action<string, int>>(o => o.SetFlagValue);
                    var setFlagExpression = Expression.Call(
                        serializationContextExpression, 
                        setFlagValueMethodInfo, 
                        Expression.Constant(propertyMeta.FlagsAttribute.Flag, typeof(string)), 
                        Expression.Convert(propertyExpression, typeof(int)));
                    yield return setFlagExpression;
                }

                var serializerExpression = this.CreatePropertySerializer(
                    streamWriterExpression, serializationContextExpression, propertyMeta, objectToSerialize);
                yield return serializerExpression;
            }
        }

        private PropertyMetaData[] InitializePropertyMetas()
        {
            // if (this.type == typeof(CreateCharacterMessage))
            // {
            //     System.Diagnostics.Debugger.Break();
            // }
            var baseType = this.type;
            var stack = new Stack<Type>();
            do
            {
                stack.Push(baseType);
                baseType = baseType.BaseType;
            }
            while (baseType != null);

            var list = new List<PropertyMetaData>();

            while (stack.Count > 0)
            {
                var t = stack.Pop();
                var p = from property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        let memberAttribute =
                            property.GetCustomAttributes(typeof(AoMemberAttribute), false)
                                    .Cast<AoMemberAttribute>()
                                    .FirstOrDefault()
                        where property.CanWrite && memberAttribute != null && property.DeclaringType == t
                        orderby memberAttribute.Order ascending
                        let flagsAttribute =
                            property.GetCustomAttributes(typeof(AoFlagsAttribute), false)
                                    .Cast<AoFlagsAttribute>()
                                    .FirstOrDefault()
                        let usesFlagsAttribute =
                            property.GetCustomAttributes(typeof(AoUsesFlagsAttribute), false)
                                    .Cast<AoUsesFlagsAttribute>()
                                    .ToArray()
                        select new PropertyMetaData(property, memberAttribute, flagsAttribute, usesFlagsAttribute);
                list.AddRange(p);
            }

            return list.ToArray();
        }

        #endregion
    }
}