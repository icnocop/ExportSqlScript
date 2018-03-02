// <copyright file="ParseForTypeResults.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
//   Licensed under the Apache License, Version 2.0 license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace NOption
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents the results of an attempt to parse a command line for a configuration class.
    /// </summary>
    public class ParseForTypeResults
    {
        private readonly Type type;
        private readonly List<OptionMemberValue> optionMemberValues;
        private readonly List<Match> unmatched;
        private readonly MemberInfo argumentsMemberInfo;
        private readonly List<ArgumentMember> argumentMembers;

        internal ParseForTypeResults(Type type, List<OptionMemberValue> optionMemberValues, List<Match> unmatched, MemberInfo argumentsMemberInfo, List<ArgumentMember> argumentMembers)
        {
            this.type = type;
            this.optionMemberValues = optionMemberValues;
            this.unmatched = unmatched;
            this.argumentsMemberInfo = argumentsMemberInfo;
            this.argumentMembers = argumentMembers;
        }

        /// <summary>
        /// Applies the command line options to a specified instance.
        /// </summary>
        /// <param name="instance">The obj.</param>
        public void Apply(object instance)
        {
            if (!instance.GetType().Equals(this.type))
            {
                throw new Exception("Attempting to apply to incorrect type");
            }

            foreach (OptionMemberValue optionMemberValue in this.optionMemberValues)
            {
                optionMemberValue.namedCommandLineOptionAttribute.SetMemberValue(
                    instance,
                    optionMemberValue.memberInfo,
                    optionMemberValue.match);
            }

            List<Match> arguements = new List<Match>();
            arguements.AddRange(this.unmatched);
            foreach (ArgumentMember argumentMember in this.argumentMembers)
            {
                int index = argumentMember.argumentCommandLineOptionAttribute.Index;
                if (index < 0)
                {
                    index = this.unmatched.Count + index + 1;
                }

                if (index < 1 || index > this.unmatched.Count)
                {
                    // Arguement index out of range
                    continue;
                }

                Util.SetMemberValue(instance, argumentMember.memberInfo, this.unmatched[index - 1].Value);
                arguements.Remove(this.unmatched[index - 1]);
            }

            // Check that unmatched will go into CommandLineArguments
            if (arguements.Count > 0 && this.argumentsMemberInfo == null)
            {
                throw new Exception("Arguements with nowhere to go");
            }

            if (arguements.Count > 0)
            {
                Type t = Util.ValueType(this.argumentsMemberInfo); // . MakeArrayType();
                Array arguementArray = Array.CreateInstance(t, arguements.Count);
                for (int i = 0; i < arguements.Count; i++)
                {
                    arguementArray.SetValue(Util.CastType(t, arguements[i].Value), i);
                }

                Util.SetMemberValue(instance, this.argumentsMemberInfo, arguementArray);
            }

            return;
        }
    }
}
