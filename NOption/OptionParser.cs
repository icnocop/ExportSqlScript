// <copyright file="OptionParser.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
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
    /// Parses command line options and applies them to objects.
    /// </summary>
    public class OptionParser
    {
        public char[] optionDelimiters = new char[] { '-', '/' };
        public char[] assignSymbols = new char[] { '=', ':' };

        public const string PrefixCaptureName = "pcn";
        public const string SwitchCaptureName = "scn";
        public const string OptionNameCaptureName = "ancn";
        public const string OptionValueCaptureName = "avcn";

        public ParseForTypeResults[] ParseForTypes(params Type[] types)
        {
            return this.ParseForTypes(Environment.CommandLine, types);
        }

        public ParseForTypeResults[] ParseForTypes(string commandLineComplete, params Type[] types)
        {
            List<ParseForTypeResults> results = new List<ParseForTypeResults>();
            foreach (Type type in types)
            {
                ParseForTypeResults result = this.ParseForType(type);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            ParseForTypeResults[] resultArray = new ParseForTypeResults[results.Count];
            results.CopyTo(resultArray);
            return resultArray;
        }

        /// <summary>
        /// Parses the command line for a particular type
        /// </summary>
        /// <param name="type">The type to parse against.</param>
        /// <returns>A ParseForTypeResults object that contains appliable command line options</returns>
        public ParseForTypeResults ParseForType(Type type)
        {
            // Get the full commandline (executable & argument)
            return this.ParseForType(Environment.CommandLine, type);
        }

        /// <summary>
        /// Parses the command line for a particular type
        /// </summary>
        /// <param name="type">The type to parse against.</param>
        /// <param name="commandLineComplete">The command line to parse.</param>
        /// <returns>A ParseForTypeResults object that contains appliable command line options</returns>
        public ParseForTypeResults ParseForType(string commandLineComplete, Type type)
        {
            // Match executable portion of command line
            Regex commandLineExecutableRegex = new Regex("(^[^ \"]+)|(\"[^\"]+\")");
            Match commandLineExecutableMatch = commandLineExecutableRegex.Match(commandLineComplete);

            // Check executable part recognition
            if (!commandLineExecutableMatch.Success || commandLineExecutableMatch.Index != 0)
            {
                throw new Exception("Unable to recognise executable command line part");
            }

            // Get command line parts
            // string commandLineExecutable = commandLineComplete.Substring(0, commandLineExecutableMatch.Length);
            string commandLineArguments = commandLineComplete.Substring(commandLineExecutableMatch.Length);

            // Regular expression option for
            RegexOptions regexOptions = RegexOptions.ExplicitCapture;

            // Get the members of the config type
            MemberInfo[] memberInfos = type.FindMembers(
                NamedCommandLineOptionAttribute.memberTypes,
                NamedCommandLineOptionAttribute.bindingFlags,
                Type.FilterName,
                "*");

            // Construct member/regexp dictionary
            Dictionary<NamedCommandLineOptionAttribute, MemberInfo> memberAttributes =
                new Dictionary<NamedCommandLineOptionAttribute, MemberInfo>();
            List<MemberInfo> requiredCommandLineOptionMembers = new List<MemberInfo>();
            MemberInfo argumentsMemberInfo = null;
            List<ArgumentMember> argumentMembers = new List<ArgumentMember>();

            // Walk thru members to find those marked with named option attribute
            foreach (MemberInfo memberInfo in memberInfos)
            {
                // Add named options to memberAttributes dictionary
                NamedCommandLineOptionAttribute[] namedCommandLineOptionAttributes =
                    Attribute.GetCustomAttributes(memberInfo, typeof(NamedCommandLineOptionAttribute)) as
                    NamedCommandLineOptionAttribute[];
                if (namedCommandLineOptionAttributes != null && namedCommandLineOptionAttributes.Length != 0)
                {
                    foreach (
                        NamedCommandLineOptionAttribute namedCommandLineOptionAttribute in
                            namedCommandLineOptionAttributes)
                    {
                        memberAttributes.Add(namedCommandLineOptionAttribute, memberInfo);
                    }
                }

                // Add required members to requiredCommandLineOptionMembers
                RequiredCommandLineOptionAttribute requiredCommandLineOptionAttribute =
                    Attribute.GetCustomAttribute(memberInfo, typeof(RequiredCommandLineOptionAttribute)) as
                    RequiredCommandLineOptionAttribute;
                if (requiredCommandLineOptionAttribute != null)
                {
                    requiredCommandLineOptionMembers.Add(memberInfo);
                }

                // Find default arguments property
                ArgumentsCommandLineOptionAttribute argumentsCommandLineOptionAttribute =
                    Attribute.GetCustomAttribute(memberInfo, typeof(ArgumentsCommandLineOptionAttribute)) as
                    ArgumentsCommandLineOptionAttribute;
                if (argumentsCommandLineOptionAttribute != null)
                {
                    if (argumentsMemberInfo != null)
                    {
                        throw new Exception("More than one arguments member");
                    }

                    argumentsMemberInfo = memberInfo;
                }

                // Add set arguements
                ArgumentCommandLineOptionAttribute argumentCommandLineOptionAttribute =
                    Attribute.GetCustomAttribute(memberInfo, typeof(ArgumentCommandLineOptionAttribute)) as
                    ArgumentCommandLineOptionAttribute;
                if (argumentCommandLineOptionAttribute != null)
                {
                    argumentMembers.Add(new ArgumentMember(argumentCommandLineOptionAttribute, memberInfo));
                }
            }

            List<OptionMemberValue> optionMemberValues = new List<OptionMemberValue>();
            List<Match> unmatched = new List<Match>();

            Dictionary<KeyValuePair<NamedCommandLineOptionAttribute, MemberInfo>, Regex> memberAttributeRegexCache =
                new Dictionary<KeyValuePair<NamedCommandLineOptionAttribute, MemberInfo>, Regex>();

            while (commandLineArguments.Length > 0)
            {
                commandLineArguments = commandLineArguments.TrimStart();

                bool processedNamedArgument = false;
                foreach (KeyValuePair<NamedCommandLineOptionAttribute, MemberInfo> memberAttribute in memberAttributes)
                {
                    Regex memberAttributeRegex;

                    // Use cached Regex if possible
                    if (memberAttributeRegexCache.ContainsKey(memberAttribute))
                    {
                        memberAttributeRegex = memberAttributeRegexCache[memberAttribute];
                    }
                    else
                    {
                        memberAttributeRegex = new Regex(memberAttribute.Key.GetRegularExpression(this), regexOptions);
                        memberAttributeRegexCache.Add(memberAttribute, memberAttributeRegex);
                    }

                    Match match = memberAttributeRegex.Match(commandLineArguments);
                    if (match.Success)
                    {
                        // Save the Attribute, Member & match for later application
                        optionMemberValues.Add(new OptionMemberValue(memberAttribute.Key, memberAttribute.Value, match));

                        // Remove required attribute from required list
                        requiredCommandLineOptionMembers.Remove(memberAttribute.Value);

                        // Consume the argument
                        commandLineArguments = commandLineArguments.Substring(match.Index + match.Length).TrimStart();
                        processedNamedArgument = true;
                    }
                }

                if (!processedNamedArgument)
                {
                    // Un-recognised argument
                    Match match = commandLineExecutableRegex.Match(commandLineArguments);
                    if (match.Success)
                    {
                        unmatched.Add(match);

                        // Consume the argument
                        commandLineArguments = commandLineArguments.Substring(match.Index + match.Length).TrimStart();
                    }
                    else
                    {
                        // Can't work out what to consume
                        throw new Exception("Can't work out what to consume");
                    }
                }
            }

            // Process unmatched arguements
            List<Match> arguements = new List<Match>();
            arguements.AddRange(unmatched);
            foreach (ArgumentMember argumentMember in argumentMembers)
            {
                int index = argumentMember.argumentCommandLineOptionAttribute.Index;
                if (index < 0)
                {
                    index = unmatched.Count + index + 1;
                }

                if (index < 1 || index > unmatched.Count)
                {
                    // Arguement out of index
                    continue;
                }

                requiredCommandLineOptionMembers.Remove(argumentMember.memberInfo);
                arguements.Remove(unmatched[index - 1]);
            }

            // Check for default unmatched argument bucket
            if (argumentsMemberInfo == null)
            {
                if (arguements.Count > 0)
                {
                    // throw new Exception("Arguements with nowhere to go");
                    return null;
                }
            }
            else
            {
                requiredCommandLineOptionMembers.Remove(argumentsMemberInfo);
            }

            // Check if we're setting all required members
            if (requiredCommandLineOptionMembers.Count != 0)
            {
                return null;
            }

            // Check for duplicate set options
            List<MemberInfo> members = new List<MemberInfo>();
            foreach (OptionMemberValue optionMemberValue in optionMemberValues)
            {
                if (members.Contains(optionMemberValue.memberInfo))
                {
                    return null;
                }

                members.Add(optionMemberValue.memberInfo);
            }

            // All looks good, create config object
                return new ParseForTypeResults(
                    type,
                    optionMemberValues,
                    unmatched,
                    argumentsMemberInfo,
                    argumentMembers);
        }
    }
}
