// <copyright file="ArgumentCommandLineOptionAttribute.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ArgumentCommandLineOptionAttribute : CommandLineOptionAttribute
    {
        public ArgumentCommandLineOptionAttribute(int index)
        {
            this.index = index;
        }

        private readonly int index = 0;

        public int Index
        {
            get { return this.index; }
        }
    }
}
