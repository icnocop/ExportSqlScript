// <copyright file="FlagCommandLineOptionAttribute.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A boolean command line option
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class FlagCommandLineOptionAttribute : NamedCommandLineOptionAttribute
    {
        /// <summary>
        /// Value if the option is set
        /// </summary>
        private readonly bool setValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlagCommandLineOptionAttribute"/> class.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="setValue">Value to apply if option is set.</param>
        public FlagCommandLineOptionAttribute(string keyName, bool setValue)
            : base(keyName)
        {
            this.setValue = setValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlagCommandLineOptionAttribute"/> class. Sets member to true if set.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        public FlagCommandLineOptionAttribute(string keyName)
            : this(keyName, true)
        {
        }

        /// <summary>
        /// Gets regular expression text to match the option
        /// </summary>
        /// <param name="optionParser">The option parser to get configuration from.</param>
        /// <returns></returns>
        public override string GetRegularExpression(OptionParser optionParser)
        {
            // Option Delimiter
            StringBuilder optionDelimiterBuilder = new StringBuilder();
            optionDelimiterBuilder.Append("(?<" + OptionParser.PrefixCaptureName + ">[");
            optionDelimiterBuilder.Append(optionParser.optionDelimiters);
            optionDelimiterBuilder.Append("])");
            string optionDelimiterPattern = optionDelimiterBuilder.ToString();

            StringBuilder optionPattern = new StringBuilder();
            optionPattern.Append("(^"); // Line Start
            optionPattern.Append(optionDelimiterPattern); // Option Delimeter
            optionPattern.Append("(?<" + OptionParser.OptionNameCaptureName + ">" + Regex.Escape(this.keyName) + ")");

            // Capture matched substring
            optionPattern.Append(")");

            return optionPattern.ToString();
        }

        /// <summary>
        /// Sets a object's member value using a regular expression match
        /// </summary>
        /// <param name="instance">The object instance to update.</param>
        /// <param name="memberInfo">The member to update.</param>
        /// <param name="match">The regular express match to use for the value.</param>
        internal override void SetMemberValue(object instance, MemberInfo memberInfo, Match match)
        {
            Util.SetMemberValue(instance, memberInfo, this.setValue);
        }
    }
}
