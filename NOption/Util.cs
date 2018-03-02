// <copyright file="Util.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Generic utility class
    /// </summary>
    internal class Util
    {
        internal static Type ValueType(MemberInfo memberInfo)
        {
            Type type;
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                type = fieldInfo.FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                type = propertyInfo.PropertyType;
            }
            else
            {
                throw new Exception("Unknown MemberInfo");
            }

            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            return type;
        }

        internal static object CastType(Type type, object value)
        {
            if (value is string)
            {
                if (type.IsEnum)
                {
                    bool ignoreCase = true;
                    object enumValue = null;
                    try
                    {
                        enumValue = Enum.Parse(type, value.ToString(), ignoreCase);
                    }
                    catch (ArgumentException e)
                    {
                        // Value didn't match. Attempt to find a distinct partial match
                        string[] names = Enum.GetNames(type);
                        foreach (string s in names)
                        {
                            if (s.StartsWith(value.ToString()))
                            {
                                if (enumValue == null)
                                {
                                    enumValue = Enum.Parse(type, s, ignoreCase);
                                }
                                else
                                {
                                    throw new Exception("Indistinct enumerated type value");
                                }
                            }
                        }

                        if (enumValue == null)
                        {
                            throw new Exception("Option value didn't match enumerated type", e);
                        }
                    }

                    return enumValue;
                }

                // Check if the destination type has a Parse(String) method. If so, call it.
                MemberInfo[] parseMembers =
                    type.FindMembers(
                        MemberTypes.All,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                        Type.FilterName,
                        "Parse");
                if (parseMembers != null)
                {
                    foreach (MethodInfo info in parseMembers as MemberInfo[])
                    {
                        ParameterInfo[] p = info.GetParameters();
                        if (p.Length == 1 && p[0].ParameterType == typeof(string))
                        {
                            return info.Invoke(null, BindingFlags.Static, null, new object[] { value }, null);
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Sets an object's member (field or property) to a value.
        /// </summary>
        /// <param name="instance">The instance on which to set the member</param>
        /// <param name="memberInfo">memberInfo class representing the member to set.</param>
        /// <param name="value">The value to set the member to.</param>
        internal static void SetMemberValue(object instance, MemberInfo memberInfo, object value)
        {
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                BindingFlags bindingFlags = BindingFlags.SetField;
                if (fieldInfo.IsStatic)
                {
                    bindingFlags |= BindingFlags.Static;
                }

                fieldInfo.SetValue(instance, CastType(fieldInfo.FieldType, value), bindingFlags, null, null);
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                if (propertyInfo.CanWrite)
                {
                    MethodInfo methodInfo = propertyInfo.GetSetMethod(true);
                    BindingFlags bindingFlags = BindingFlags.SetProperty;
                    if (methodInfo.IsStatic)
                    {
                        bindingFlags |= BindingFlags.Static;
                    }

                    propertyInfo.SetValue(instance, CastType(propertyInfo.PropertyType, value), bindingFlags, null, null, null);
                }
                else
                {
                    throw new Exception("Property is read-only");
                }
            }
            else
            {
                throw new Exception("Unexpected memberInfo type");
            }
        }
    }
}
