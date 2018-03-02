// <copyright file="RequiredCommandLineOptionAttribute.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;

    /// <summary>
    /// Marks a member as requiring a matching option to set it
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredCommandLineOptionAttribute : CommandLineOptionAttribute
    {
    }
}
