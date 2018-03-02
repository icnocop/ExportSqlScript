// <copyright file="OptionMemberValue.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A grouping of NamedCommandLineOptionAttribute, memberInfo and Match
    /// </summary>
    internal class OptionMemberValue
    {
        internal readonly NamedCommandLineOptionAttribute namedCommandLineOptionAttribute;
        internal readonly MemberInfo memberInfo;
        internal readonly Match match;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionMemberValue"/> class.
        /// </summary>
        /// <param name="namedCommandLineOptionAttribute">The named command line option attribute.</param>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="match">The regular expression match.</param>
        internal OptionMemberValue(NamedCommandLineOptionAttribute namedCommandLineOptionAttribute,
                                   MemberInfo memberInfo,
                                   Match match)
        {
            this.namedCommandLineOptionAttribute = namedCommandLineOptionAttribute;
            this.memberInfo = memberInfo;
            this.match = match;
        }
    }
}
