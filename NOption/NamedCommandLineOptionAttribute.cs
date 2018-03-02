// <copyright file="NamedCommandLineOptionAttribute.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Base class for named command line options
    /// </summary>
    public abstract class NamedCommandLineOptionAttribute : CommandLineOptionAttribute
    {
        /// <summary>
        /// The command line option name
        /// </summary>
        protected string keyName;

        public NamedCommandLineOptionAttribute(string keyName)
        {
            this.keyName = keyName;
        }

        /// <summary>
        /// Gets regular expression text to match the option
        /// </summary>
        /// <param name="optionParser">The option parser to get configuration from.</param>
        /// <returns></returns>
        public virtual string GetRegularExpression(OptionParser optionParser)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets a object's member value using a regular expression match
        /// </summary>
        /// <param name="instance">The object instance to update.</param>
        /// <param name="memberInfo">The member to update.</param>
        /// <param name="match">The regular express match to use for the value.</param>
        internal abstract void SetMemberValue(object instance, MemberInfo memberInfo, Match match);
    }
}
