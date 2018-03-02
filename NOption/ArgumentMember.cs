// <copyright file="ArgumentMember.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System.Reflection;

    internal class ArgumentMember
    {
        internal readonly ArgumentCommandLineOptionAttribute argumentCommandLineOptionAttribute;
        internal readonly MemberInfo memberInfo;

        public ArgumentMember(ArgumentCommandLineOptionAttribute argumentCommandLineOptionAttribute, MemberInfo memberInfo)
        {
            this.argumentCommandLineOptionAttribute = argumentCommandLineOptionAttribute;
            this.memberInfo = memberInfo;
        }
    }
}
