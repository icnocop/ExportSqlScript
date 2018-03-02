// <copyright file="KeyValueCommandLineOptionAttribute.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
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
    /// A string command line option
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class KeyValueCommandLineOptionAttribute : NamedCommandLineOptionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueCommandLineOptionAttribute"/> class.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        public KeyValueCommandLineOptionAttribute(string keyName)
            : base(keyName)
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

            optionPattern.Append("(^");
            optionPattern.Append(optionDelimiterPattern);

            optionPattern.Append("(?<" + OptionParser.OptionNameCaptureName + ">" + Regex.Escape(this.keyName) + ")");

            optionPattern.Append("([");
            optionPattern.Append(optionParser.assignSymbols);
            optionPattern.Append("]");
            optionPattern.Append("(?<" + OptionParser.OptionValueCaptureName + ">(([^ \"]+)|(\"[^\"]+\"))))");

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
            string value = match.Groups[OptionParser.OptionValueCaptureName].Value;
            if (value[0] == '"')
            {
                value = value.Trim('"');
            }

            Util.SetMemberValue(instance, memberInfo, value);
        }
    }
}
