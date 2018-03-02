/* Copyright 2007 Ivan Hamilton
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NOption
{
    /// <summary>
    /// Generic utility class
    /// </summary>
    internal class Util
    {
        internal static Type ValueType(MemberInfo memberInfo)
        {
            Type type;
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                type = fieldInfo.FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                type = propertyInfo.PropertyType;
            } 
            else
                throw new Exception("Unknown MemberInfo");
            if (type.IsArray)
                type = type.GetElementType();
            return type;
        }

        internal static object CastType(Type type, Object value)
        {
            if (value is String)
            {
                if (type.IsEnum)
                {
                    bool ignoreCase = true;
                    Object enumValue = null;
                    try
                    {
                        enumValue = Enum.Parse(type, value.ToString(), ignoreCase);
                    }
                    catch (ArgumentException e)
                    {
                        //Value didn't match. Attempt to find a distinct partial match
                        string[] names = Enum.GetNames(type);
                        foreach (string s in names)
                            if (s.StartsWith(value.ToString()))
                                if (enumValue == null) enumValue = Enum.Parse(type, s, ignoreCase);
                                else throw new Exception("Indistinct enumerated type value");
                        if (enumValue == null) throw new Exception("Option value didn't match enumerated type", e);
                    }
                    return enumValue;
                }

                //Check if the destination type has a Parse(String) method. If so, call it.
                MemberInfo[] ParseMembers =
                    type.FindMembers(MemberTypes.All,
                                     BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                                     Type.FilterName, "Parse");
                if (ParseMembers != null)
                    foreach (MethodInfo info in ParseMembers as MemberInfo[])
                    {
                        ParameterInfo[] p = info.GetParameters();
                        if (p.Length == 1 && p[0].ParameterType == typeof(String)) return info.Invoke(null, BindingFlags.Static, null, new object[] { value }, null);
                    }
            }
            return value;
        }

        /// <summary>
        /// Sets an object's member (field or property) to a value.
        /// </summary>
        /// <param name="instance">The instance on which to set the member</param>
        /// <param name="memberInfo">memberInfo class representing the member to set.</param>
        /// <param name="value">The value to set the member to.</param>
        internal static void SetMemberValue(object instance, MemberInfo memberInfo, object value)
        {
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo) memberInfo;
                BindingFlags bindingFlags = BindingFlags.SetField;
                if (fieldInfo.IsStatic)
                    bindingFlags |= BindingFlags.Static;
                fieldInfo.SetValue(instance, CastType(fieldInfo.FieldType, value), bindingFlags, null, null);
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo) memberInfo;
                if (propertyInfo.CanWrite)
                {
                    MethodInfo methodInfo = propertyInfo.GetSetMethod(true);
                    BindingFlags bindingFlags = BindingFlags.SetProperty;
                    if (methodInfo.IsStatic)
                        bindingFlags |= BindingFlags.Static;
                    propertyInfo.SetValue(instance, CastType(propertyInfo.PropertyType, value), bindingFlags, null, null, null);
                }
                else
                    throw new Exception("Property is read-only");
            }
            else throw new Exception("Unexpected memberInfo type");
        }
    }

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

    /// <summary>
    /// Marks a member as requiring a matching option to set it
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredCommandLineOptionAttribute : CommandLineOptionAttribute
    {
    }

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
        internal abstract void SetMemberValue(Object instance, MemberInfo memberInfo, Match match);
    }

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
            String optionDelimiterPattern = optionDelimiterBuilder.ToString();

            StringBuilder optionPattern = new StringBuilder();
            optionPattern.Append("(^"); //Line Start
            optionPattern.Append(optionDelimiterPattern); //Option Delimeter
            optionPattern.Append("(?<" + OptionParser.OptionNameCaptureName + ">" + Regex.Escape(keyName) + ")");
            //Capture matched substring 
            optionPattern.Append(")");

            return optionPattern.ToString();
        }

        /// <summary>
        /// Sets a object's member value using a regular expression match
        /// </summary>
        /// <param name="instance">The object instance to update.</param>
        /// <param name="memberInfo">The member to update.</param>
        /// <param name="match">The regular express match to use for the value.</param>
        internal override void SetMemberValue(Object instance, MemberInfo memberInfo, Match match)
        {
            Util.SetMemberValue(instance, memberInfo, setValue);
        }
    }

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
            String optionDelimiterPattern = optionDelimiterBuilder.ToString();

            StringBuilder optionPattern = new StringBuilder();

            optionPattern.Append("(^");
            optionPattern.Append(optionDelimiterPattern);

            optionPattern.Append("(?<" + OptionParser.OptionNameCaptureName + ">" + Regex.Escape(keyName) + ")");

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
        internal override void SetMemberValue(Object instance, MemberInfo memberInfo, Match match)
        {
            string value = match.Groups[OptionParser.OptionValueCaptureName].Value;
            if (value[0] == '"') value = value.Trim('"');
            Util.SetMemberValue(instance, memberInfo, value);
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ArgumentCommandLineOptionAttribute : CommandLineOptionAttribute
    {
        //private argumentCommandLineOptionAttribute()
        //{
        //}

        public ArgumentCommandLineOptionAttribute(int index)
        {
            this.index = index;
        }

        private readonly int index=0;

        public int Index
        {
            get { return index; }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ArgumentsCommandLineOptionAttribute : CommandLineOptionAttribute
    {
    }

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

    /// <summary>
    /// A grouping of NamedCommandLineOptionAttribute, memberInfo & Match 
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


    /// <summary>
    /// Represents the results of an attempt to parse a command line for a configuration class.
    /// </summary>
    public class ParseForTypeResults
    {
        internal readonly Type type;
        internal readonly List<OptionMemberValue> optionMemberValues;
        internal readonly List<Match> unmatched;
        internal readonly MemberInfo argumentsMemberInfo;
        internal readonly List<ArgumentMember> argumentMembers;

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
        public void Apply(Object instance)
        {
            if (!instance.GetType().Equals(type))
                throw new Exception("Attempting to apply to incorrect type");
            foreach (OptionMemberValue optionMemberValue in optionMemberValues)
                optionMemberValue.namedCommandLineOptionAttribute.SetMemberValue(instance, optionMemberValue.memberInfo,
                                                                                 optionMemberValue.match);

            List<Match> arguements = new List<Match>();
            arguements.AddRange(unmatched);
            foreach (ArgumentMember argumentMember in argumentMembers)
            {
                int index = argumentMember.argumentCommandLineOptionAttribute.Index;
                if (index < 0)
                    index = unmatched.Count + index + 1;
                if (index < 1 || index > unmatched.Count)
                    //Arguement index out of range
                    continue;
                Util.SetMemberValue(instance, argumentMember.memberInfo, unmatched[index - 1].Value);
                arguements.Remove(unmatched[index - 1]);
            }

            //Check that unmatched will go into CommandLineArguments
            if (arguements.Count > 0 && argumentsMemberInfo == null)
                throw new Exception("Arguements with nowhere to go");

            if (arguements.Count > 0)
            {
                Type t = Util.ValueType(argumentsMemberInfo); //. MakeArrayType();
                Array arguementArray = Array.CreateInstance(t, arguements.Count);
                for (int i = 0; i < arguements.Count; i++)
                    arguementArray.SetValue(Util.CastType(t, arguements[i].Value),i);
                Util.SetMemberValue(instance, argumentsMemberInfo, arguementArray);
            }
            return;
        }
    }


    /// <summary>
    /// Parses command line options and applies them to objects.
    /// </summary>
    public class OptionParser
    {
        public Char[] optionDelimiters = new Char[] {'-', '/'};
        public Char[] assignSymbols = new Char[] {'=', ':'};

        public const string PrefixCaptureName = "pcn";
        public const string SwitchCaptureName = "scn";
        public const string OptionNameCaptureName = "ancn";
        public const string OptionValueCaptureName = "avcn";


        public ParseForTypeResults[] ParseForTypes(params Type[] types)
        {
            return ParseForTypes(Environment.CommandLine, types);
        }

        public ParseForTypeResults[] ParseForTypes(string commandLineComplete, params Type[] types)
        {
            List<ParseForTypeResults> results = new List<ParseForTypeResults>();
            foreach (Type type in types)
            {
                ParseForTypeResults result = ParseForType(type);
                if (result != null)
                    results.Add(result);
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
            //Get the full commandline (executable & argument)
            return ParseForType(Environment.CommandLine, type);
        }

        /// <summary>
        /// Parses the command line for a particular type
        /// </summary>
        /// <param name="type">The type to parse against.</param>
        /// <param name="commandLineComplete">The command line to parse.</param>
        /// <returns>A ParseForTypeResults object that contains appliable command line options</returns>
        public ParseForTypeResults ParseForType(string commandLineComplete, Type type)
        {

            //Match executable portion of command line
            Regex commandLineExecutableRegex = new Regex("(^[^ \"]+)|(\"[^\"]+\")");
            Match commandLineExecutableMatch = commandLineExecutableRegex.Match(commandLineComplete);

            //Check executable part recognition
            if (!commandLineExecutableMatch.Success || commandLineExecutableMatch.Index != 0)
                throw new Exception("Unable to recognise executable command line part");

            //Get command line parts
            //string commandLineExecutable = commandLineComplete.Substring(0, commandLineExecutableMatch.Length);
            string commandLineArguments = commandLineComplete.Substring(commandLineExecutableMatch.Length);

            //Regular expression option for 
            RegexOptions regexOptions = RegexOptions.ExplicitCapture;

            //Get the members of the config type
            MemberInfo[] memberInfos = type.FindMembers(
                NamedCommandLineOptionAttribute.memberTypes,
                NamedCommandLineOptionAttribute.bindingFlags,
                Type.FilterName, "*");

            //Construct member/regexp dictionary
            Dictionary<NamedCommandLineOptionAttribute, MemberInfo> memberAttributes =
                new Dictionary<NamedCommandLineOptionAttribute, MemberInfo>();
            List<MemberInfo> requiredCommandLineOptionMembers = new List<MemberInfo>();
            MemberInfo argumentsMemberInfo = null;
            List<ArgumentMember> argumentMembers= new List<ArgumentMember>();

            //Walk thru members to find those marked with named option attribute
            foreach (MemberInfo memberInfo in memberInfos)
            {
                //Add named options to memberAttributes dictionary
                NamedCommandLineOptionAttribute[] namedCommandLineOptionAttributes =
                    Attribute.GetCustomAttributes(memberInfo, typeof (NamedCommandLineOptionAttribute)) as
                    NamedCommandLineOptionAttribute[];
                if (namedCommandLineOptionAttributes != null && namedCommandLineOptionAttributes.Length != 0)
                    foreach (
                        NamedCommandLineOptionAttribute namedCommandLineOptionAttribute in
                            namedCommandLineOptionAttributes)
                        memberAttributes.Add(namedCommandLineOptionAttribute, memberInfo);
                //Add required members to requiredCommandLineOptionMembers 
                RequiredCommandLineOptionAttribute requiredCommandLineOptionAttribute =
                    Attribute.GetCustomAttribute(memberInfo, typeof (RequiredCommandLineOptionAttribute)) as
                    RequiredCommandLineOptionAttribute;
                if (requiredCommandLineOptionAttribute != null)
                    requiredCommandLineOptionMembers.Add(memberInfo);
                //Find default arguments property
                ArgumentsCommandLineOptionAttribute argumentsCommandLineOptionAttribute =
                    Attribute.GetCustomAttribute(memberInfo, typeof(ArgumentsCommandLineOptionAttribute)) as
                    ArgumentsCommandLineOptionAttribute;
                if (argumentsCommandLineOptionAttribute != null)
                {
                    if (argumentsMemberInfo != null)
                        throw new Exception("More than one arguments member");
                    argumentsMemberInfo = memberInfo;
                }
                //Add set arguements
                ArgumentCommandLineOptionAttribute argumentCommandLineOptionAttribute =
                    Attribute.GetCustomAttribute(memberInfo, typeof(ArgumentCommandLineOptionAttribute)) as
                    ArgumentCommandLineOptionAttribute;
                if (argumentCommandLineOptionAttribute != null)
                    argumentMembers.Add(new ArgumentMember(argumentCommandLineOptionAttribute, memberInfo));
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
                    //Use cached Regex if possible
                    if (memberAttributeRegexCache.ContainsKey(memberAttribute))
                        memberAttributeRegex = memberAttributeRegexCache[memberAttribute];
                    else
                    {
                        memberAttributeRegex = new Regex(memberAttribute.Key.GetRegularExpression(this), regexOptions);
                        memberAttributeRegexCache.Add(memberAttribute, memberAttributeRegex);
                    }

                    Match match = memberAttributeRegex.Match(commandLineArguments);
                    if (match.Success)
                    {
                        //Save the Attribute, Member & match for later application
                        optionMemberValues.Add(new OptionMemberValue(memberAttribute.Key, memberAttribute.Value, match));

                        //Remove required attribute from required list
                        requiredCommandLineOptionMembers.Remove(memberAttribute.Value);

                        //Consume the argument
                        commandLineArguments = commandLineArguments.Substring(match.Index + match.Length).TrimStart();
                        processedNamedArgument = true;
                    }
                }
                if (!processedNamedArgument)
                {
                    //Un-recognised argument
                    Match match = commandLineExecutableRegex.Match(commandLineArguments);
                    if (match.Success)
                    {
                        unmatched.Add(match);
                        //Consume the argument
                        commandLineArguments = commandLineArguments.Substring(match.Index + match.Length).TrimStart();
                    }
                    else
                    {
                        //Can't work out what to consume
                        throw new Exception("Can't work out what to consume");
                    }
                }
            }

            //Process unmatched arguements
            List<Match> arguements=new List<Match>();
            arguements.AddRange(unmatched);
            foreach (ArgumentMember argumentMember in argumentMembers)
            {
                int index = argumentMember.argumentCommandLineOptionAttribute.Index;
                if (index<0)
                    index = unmatched.Count + index + 1;
                if (index<1 || index > unmatched.Count)
                    //Arguement out of index
                    continue;
                requiredCommandLineOptionMembers.Remove(argumentMember.memberInfo);
                arguements.Remove(unmatched[index - 1]);
            }

            //Check for default unmatched argument bucket
            if (argumentsMemberInfo == null)
            {
                if (arguements.Count > 0)
                    //throw new Exception("Arguements with nowhere to go");
                    return null;
            }
            else
            {
                requiredCommandLineOptionMembers.Remove(argumentsMemberInfo);
            }

            //Check if we're setting all required members
            if (requiredCommandLineOptionMembers.Count != 0)
                return null;

            //Check for duplicate set options
            List<MemberInfo> members=new List<MemberInfo>();
            foreach (OptionMemberValue optionMemberValue in optionMemberValues)
            {
                if (members.Contains(optionMemberValue.memberInfo)) return null;
                members.Add(optionMemberValue.memberInfo);
            }

            //All looks good, create config object
                return new ParseForTypeResults(
                    type,
                    optionMemberValues,
                    unmatched,
                    argumentsMemberInfo, 
                    argumentMembers);
        }
    }
}
