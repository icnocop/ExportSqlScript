// <copyright file="CommandLineOptionAttribute.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Base class for command line option attributes
    /// </summary>
    public abstract class CommandLineOptionAttribute : Attribute
    {
        internal const BindingFlags bindingFlags =
            BindingFlags.DeclaredOnly
            | BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Static
            | BindingFlags.FlattenHierarchy;

        internal const MemberTypes memberTypes =
            MemberTypes.Field
            | MemberTypes.Property;
    }
}
