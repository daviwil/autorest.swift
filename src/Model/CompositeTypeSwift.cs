﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AutoRest.Core.Model;
using AutoRest.Core.Utilities;
using AutoRest.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static AutoRest.Core.Utilities.DependencyInjection;

namespace AutoRest.Swift.Model
{
    /// <summary>
    /// Defines a synthesized composite type that wraps a primary type, array, or dictionary method response.
    /// </summary>
    public class CompositeTypeSwift : CompositeType, IVariableType
    {
        public static string TestNamespace { get; set; }

        private bool _wrapper;

        // True if the type is returned by a method
        public bool IsResponseType;

        // Name of the field containing the URL used to retrieve the next result set
        // (null or empty if the model is not paged).
        public string NextLink;

        public bool PreparerNeeded = false;

        public IEnumerable<CompositeType> DerivedTypes => CodeModel.ModelTypes.Where(t => t.DerivesFrom(this));

        public IEnumerable<CompositeType> SiblingTypes
        {
            get
            {
                var st = (BaseModelType as CompositeTypeSwift).DerivedTypes;
                if (BaseModelType.BaseModelType != null && BaseModelType.BaseModelType.IsPolymorphic)
                {
                    st = st.Union((BaseModelType as CompositeTypeSwift).SiblingTypes);
                }
                return st;
            }
        }

        public string InterfaceOutput
        {
            get
            {
                CompositeTypeSwift baseType = (CompositeTypeSwift)this.BaseModelType;
                if(baseType != null)
                {
                    return $"{this.Name}Protocol, {baseType.InterfaceOutput}";
                }

                return $"{this.Name}Protocol";
            }
        }

        public string BaseTypeInterfaceForProtocol
        {
            get
            {
                CompositeTypeSwift baseType = (CompositeTypeSwift)this.BaseModelType;
                if(baseType != null)
                {
                    return $"{baseType.Name}Protocol";
                }

                return $"Codable";
            }
        }

        public bool HasPolymorphicFields
        {
            get
            {
                return AllProperties.Any(p => 
                        // polymorphic composite
                        (p.ModelType is CompositeType && (p.ModelType as CompositeTypeSwift).IsPolymorphic) ||
                        // polymorphic array
                        (p.ModelType is SequenceType && (p.ModelType as ArrayTypeSwift).ElementType is CompositeType &&
                            ((p.ModelType as ArrayTypeSwift).ElementType as CompositeType).IsPolymorphic));
            }
        }

        public EnumType DiscriminatorEnum;

        public string DiscriminatorEnumValue => (DiscriminatorEnum as EnumTypeSwift).Constants.FirstOrDefault(c => c.Value.Equals(SerializedName)).Key;

        public CompositeTypeSwift()
        {

        }

        public CompositeTypeSwift(string name) : base(name)
        {

        }

        public CompositeTypeSwift(IModelType wrappedType)
        {
            // gosdk: Ensure the generated name does not collide with existing type names
            BaseType = wrappedType;

            IModelType elementType = GetElementType(wrappedType);

            if (elementType is PrimaryType)
            {
                var type = (elementType as PrimaryType).KnownPrimaryType;
                switch (type)
                {
                    case KnownPrimaryType.Object:
                        Name += "AnyObject";
                        break;

                    case KnownPrimaryType.Boolean:
                        Name += "Bool";
                        break;

                    case KnownPrimaryType.Double:
                        Name += "Double";
                        break;

                    case KnownPrimaryType.Int:
                        Name += "Int32";
                        break;

                    case KnownPrimaryType.Long:
                        Name += "Int64";
                        break;

                    default:
                        Name += type.ToString();
                        break;
                }
            }
            else
            {
                Name += elementType.Name;
            }

            // add the wrapped type as a property named Value
            var p = new PropertySwift();
            p.Name = "Value";
            p.SerializedName = "value";
            p.ModelType = wrappedType;
            Add(p);

            _wrapper = true;
        }

        /// <summary>
        /// If PolymorphicDiscriminator is set, makes sure we have a PolymorphicDiscriminator property.
        /// </summary>
        private void AddPolymorphicPropertyIfNecessary()
        {
            if (!string.IsNullOrEmpty(PolymorphicDiscriminator) && Properties.All(p => p.SerializedName != PolymorphicDiscriminator))
            {
                base.Add(New<Property>(new
                {
                    Name = CodeNamerSwift.Instance.GetPropertyName(PolymorphicDiscriminator),
                    SerializedName = PolymorphicDiscriminator,
                    ModelType = DiscriminatorEnum,
                }));
            }            
        }

        public string PolymorphicProperty
        {
            get
            {
                if (!string.IsNullOrEmpty(PolymorphicDiscriminator))
                {
                    return CodeNamerSwift.Instance.GetPropertyName(PolymorphicDiscriminator);
                }
                if (BaseModelType != null)
                {
                    return (BaseModelType as CompositeTypeSwift).PolymorphicProperty;
                }
                return null;
            }
        }

        public IEnumerable<PropertySwift> AllProperties
        {
            get
            {
                if (BaseModelType != null)
                {
                    return Properties.Cast<PropertySwift>().Concat((BaseModelType as CompositeTypeSwift).AllProperties);
                }
                return Properties.Cast<PropertySwift>();
            }
        }

        public override Property Add(Property item)
        {
            var property = base.Add(item) as PropertySwift;
            return property;
        }

        /// <summary>
        /// Add imports for composite types.
        /// </summary>
        /// <param name="imports"></param>
        public void AddImports(HashSet<string> imports)
        {
            Properties.ForEach(p => p.ModelType.AddImports(imports));
        }

        public bool IsPolymorphicResponse() {
            if (BaseIsPolymorphic && BaseModelType != null)
            {
                return (BaseModelType as CompositeTypeSwift).IsPolymorphicResponse();
            }
            return IsPolymorphic && IsResponseType;
        }

        public string FieldsAsString(bool forInterface = false)
        {
            AddPolymorphicPropertyIfNecessary();
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null && !forInterface)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).FieldsAsString(forInterface));
            }

            // Emit each property, except for named Enumerated types, as a pointer to the type
            foreach (var property in properties)
            {
                var modelType = property.ModelType;
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                if(modelType is PrimaryTypeSwift) {
                    ((PrimaryTypeSwift)modelType).IsRequired = property.IsRequired;
                }

                var modelDeclaration = modelType.Name;
                if (modelType is IVariableType)
                {
                    modelDeclaration = ((IVariableType)modelType).VariableTypeDeclaration(property.IsRequired);
                }

                var output = string.Empty;
                var propName = property.VariableName;
                var modifier = forInterface ? "" : "public";
                //TODO: need to handle flatten property case.
                output = string.Format("{2} var {0}: {1}", 
                    propName,
                    modelDeclaration,
                    modifier);
            
                if (forInterface)
                {
                    output += " { get set }\n";
                }else
                {
                    output += "\n";
                }

                indented.Append(output);
            }

            return indented.ToString();
        }

        public string FieldEnumValuesForCodable()
        {
            AddPolymorphicPropertyIfNecessary();
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).FieldEnumValuesForCodable());
            }

            // Emit each property, except for named Enumerated types, as a pointer to the type
            foreach (var property in properties)
            {
                var propName = property.VariableName;
                var serializeName = property.SerializedName;
                indented.Append($"case {propName} = \"{serializeName}\"\r\n");
            }

            return indented.ToString();
        }

        public string FieldEncodingString()
        {
            AddPolymorphicPropertyIfNecessary();
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).FieldEncodingString());
            }

            // Emit each property, except for named Enumerated types, as a pointer to the type
            foreach (var property in properties)
            {
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                var propName = property.VariableName;
                var modelType = property.ModelType;
                if(modelType is PrimaryTypeSwift) {
                    ((PrimaryTypeSwift)modelType).IsRequired = property.IsRequired;
                }

                if (modelType is IVariableType &&
                    !(modelType is EnumType) &&
                    !(modelType is DictionaryType) &&
                    !string.IsNullOrEmpty(((IVariableType)modelType).DecodeTypeDeclaration(property.IsRequired)))
                {
                    if (property.IsRequired)
                    {
                        indented.Append($"try container.encode({propName} as! {((IVariableType)modelType).DecodeTypeDeclaration(property.IsRequired)}, forKey: .{propName})\r\n");
                    }
                    else
                    {
                        indented.Append($"if self.{propName} != nil {{try container.encode({propName} as! {((IVariableType)modelType).DecodeTypeDeclaration(property.IsRequired)}, forKey: .{propName})}}\r\n");
                    }
                }
                else
                {
                    if (property.IsRequired)
                    {
                        indented.Append($"try container.encode({propName}, forKey: .{propName})\r\n");
                    }
                    else
                    {
                        indented.Append($"if self.{propName} != nil {{try container.encode({propName}, forKey: .{propName})}}\r\n");
                    }
                    
                }
            }

            return indented.ToString();
        }

        public string FieldDecodingString()
        {
            AddPolymorphicPropertyIfNecessary();
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).FieldDecodingString());
            }

            // Emit each property, except for named Enumerated types, as a pointer to the type
            foreach (var property in properties)
            {
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                var propName = property.VariableName;
                var modelType = property.ModelType;
                if(modelType is PrimaryTypeSwift) {
                    ((PrimaryTypeSwift)modelType).IsRequired = property.IsRequired;
                }

                var modelDeclaration = modelType.Name;
                if (modelType is IVariableType && 
                    !string.IsNullOrEmpty(((IVariableType)modelType).DecodeTypeDeclaration(property.IsRequired)))
                {
                    modelDeclaration = ((IVariableType)modelType).DecodeTypeDeclaration(property.IsRequired);
                }

                if (property.IsRequired)
                {
                    indented.Append($"{propName} = try container.decode({modelDeclaration}.self, forKey: .{propName})\r\n");
                }
                else
                {
                    indented.Append($"if container.contains(.{propName}) {{\r\n");
                    indented.Append($"    {propName} = try container.decode({modelDeclaration}.self, forKey: .{propName})\r\n");
                    indented.Append($"}}\r\n");
                }
            }

            return indented.ToString();
        }

        public string FieldsForTest()
        {
            AddPolymorphicPropertyIfNecessary();
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).FieldsForTest());
            }

            foreach (var property in properties)
            {
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                var propName = property.VariableName;
                var serializeName = property.SerializedName;
                indented.Append($"model.{propName} = nil\r\n");
            }

            return indented.ToString();
        }

        public string FieldsForValidation()
        {
            AddPolymorphicPropertyIfNecessary();
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).FieldsForValidation());
            }

            // Emit each property, except for named Enumerated types, as a pointer to the type
            foreach (var property in properties)
            {
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                var propName = SwiftNameHelper.convertToValidSwiftTypeName(property.Name.RawValue);
                var modelType = property.ModelType;
                var modelDeclaration = modelType.Name;
                var serializeName = SwiftNameHelper.convertToValidSwiftTypeName(property.SerializedName);
                if (modelType is IVariableType && 
                    !string.IsNullOrEmpty(((IVariableType)modelType).DecodeTypeDeclaration(property.IsRequired)))
                {
                }
                else
                {
                }
            }

            return indented.ToString();
        }

        public bool IsWrapperType => _wrapper;

        public IModelType BaseType { get; private set; }

        public IModelType GetElementType(IModelType type)
        {
            if (type is ArrayTypeSwift)
            {
                Name += "List";
                return GetElementType((type as SequenceType).ElementType);
            }
            else if (type is DictionaryTypeSwift)
            {
                Name += "Set";
                return GetElementType(((type as DictionaryTypeSwift).ValueType));
            }
            else
            {
                return type;
            }
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public bool IsRequired { get; set; }

        public string VariableTypeDeclaration(bool isRequired)
        {
            return SwiftNameHelper.getTypeName(this.Name + "Protocol", isRequired);
        }

        public string EncodeTypeDeclaration(bool isRequired)
        {
                return SwiftNameHelper.getTypeName(this.TypeName, isRequired);
        }

        public string DecodeTypeDeclaration(bool isRequired)
        {
            return SwiftNameHelper.getTypeName(this.TypeName, isRequired);
        }

        public string VariableName
        {
            get
            {
                return SwiftNameHelper.convertToVariableName(this.Name);
            }
        }

        public string TypeName {
            get {
                return this.Name + "Data";
            }
        }

        public bool HasRequiredFields {
            get {
                return this.Properties.Where(x => x.IsRequired).Count() > 0;
            }
        }

        public string RequiredPropertiesForInitParameters(bool forMethodCall = false)
        {
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            var seperator = "";
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).RequiredPropertiesForInitParameters(forMethodCall));
                seperator = ", ";
            }

            // Emit each property, except for named Enumerated types, as a pointer to the type
            foreach (var property in properties)
            {
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                var modelType = property.ModelType;
                if(modelType is PrimaryTypeSwift) {
                    ((PrimaryTypeSwift)modelType).IsRequired = property.IsRequired;
                }

                var modelDeclaration = modelType.Name;
                if (modelType is IVariableType)
                {
                    modelDeclaration = ((IVariableType)modelType).VariableTypeDeclaration(property.IsRequired);
                }


                var output = string.Empty;
                var propName = property.VariableName;

                if (property.IsRequired)
                {
                    if(forMethodCall) {
                        indented.Append($"{seperator}{propName}: {propName}");
                    }else {
                        indented.Append($"{seperator}{propName}: {modelDeclaration}");
                    }

                    seperator = ", ";
                }
            }

            return indented.ToString();
        }

        public string RequiredPropertiesSettersForInitParameters()
        {
            var indented = new IndentedStringBuilder("    ");
            var properties = Properties.Cast<PropertySwift>().ToList();
            if (BaseModelType != null)
            {
                indented.Append(((CompositeTypeSwift)BaseModelType).RequiredPropertiesSettersForInitParameters());
            }

            foreach (var property in properties)
            {
                if(property.IsPolymorphicDiscriminator) {
                    continue;
                }

                var propName = property.VariableName;
                var modelType = property.ModelType;
                if(modelType is PrimaryTypeSwift) {
                    ((PrimaryTypeSwift)modelType).IsRequired = property.IsRequired;
                }

                if (property.IsRequired)
                {
                    indented.Append($"self.{propName} = {propName}\r\n");
                }
            }

            return indented.ToString();
        }
    }
}