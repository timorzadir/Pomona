#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2012 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Pomona.Common.Internals;
using Pomona.Common.TypeSystem;

namespace Pomona
{
    /// <summary>
    /// Represents a type that is transformed
    /// </summary>
    public class TransformedType : IMappedType
    {
        private readonly Type mappedType;
        private readonly string name;
        private readonly List<PropertyMapping> properties = new List<PropertyMapping>();

        private readonly TypeMapper typeMapper;


        internal TransformedType(Type mappedType, string name, TypeMapper typeMapper)
        {
            if (typeMapper == null)
                throw new ArgumentNullException("typeMapper");
            this.mappedType = mappedType;
            this.name = name;
            this.typeMapper = typeMapper;

            UriBaseType = this;
            PluralName = SingularToPluralTranslator.CamelCaseToPlural(Name);
            PostReturnType = this;
        }


        public ConstructorInfo ConstructorInfo { get; set; }

        public bool MappedAsValueObject { get; set; }

        /// <summary>
        /// Other types having the same URI as this type. (in same inheritance chain)
        /// </summary>
        public IEnumerable<TransformedType> MergedTypes
        {
            get { return typeMapper.TransformedTypes.Where(x => x != this && x.UriBaseType == UriBaseType); }
        }

        public string PluralName { get; set; }

        public bool PostAllowed
        {
            get { return true; }
        }

        /// <summary>
        /// What type will be returned when this type is POST'ed.
        /// By default this will refer to itself.
        /// </summary>
        public TransformedType PostReturnType { get; set; }

        public TypeMapper TypeMapper
        {
            get { return typeMapper; }
        }

        public TransformedType UriBaseType { get; set; }

        public string UriRelativePath { get; set; }

        #region IMappedType Members

        public object Create(IDictionary<IPropertyInfo, object> args)
        {
            var propValues = new List<KeyValuePair<PropertyMapping, object>>(args.Count);
            var ctorValues = new List<KeyValuePair<PropertyMapping, object>>(args.Count);

            var ctorArgs = new object[ConstructorInfo.GetParameters().Length];

            foreach (var kvp in args)
            {
                var propMapping = (PropertyMapping) kvp.Key;
                if (propMapping.ConstructorArgIndex == -1)
                    propValues.Add(new KeyValuePair<PropertyMapping, object>(propMapping, kvp.Value));
                else
                {
                    ctorValues.Add(new KeyValuePair<PropertyMapping, object>(propMapping, kvp.Value));
                }
            }

            foreach (var kvp in ctorValues)
            {
                ctorArgs[kvp.Key.ConstructorArgIndex] = kvp.Value;
            }

            var instance = Activator.CreateInstance(MappedTypeInstance, ctorArgs);

            foreach (var kvp in propValues)
            {
                kvp.Key.Setter(instance, kvp.Value);
            }

            return instance;
        }


        public IMappedType BaseType { get; set; }

        public IMappedType ElementType
        {
            get
            {
                throw new InvalidOperationException(
                    "TransformedType is never a collection, so it won't have a element type.");
            }
        }

        public Type CustomClientLibraryType
        {
            get { return null; }
        }

        public IMappedType DictionaryKeyType
        {
            get { throw new NotSupportedException(); }
        }

        public IMappedType DictionaryType
        {
            get { throw new NotSupportedException(); }
        }

        public IMappedType DictionaryValueType
        {
            get { throw new NotSupportedException(); }
        }

        public IList<IMappedType> GenericArguments
        {
            get { return new IMappedType[] {}; }
        }

        public bool IsAlwaysExpanded
        {
            get { return MappedAsValueObject; }
        }

        public bool IsBasicWireType
        {
            get { return false; }
        }

        public bool IsCollection
        {
            get { return false; }
        }

        public bool IsDictionary
        {
            get { return false; }
        }

        public bool IsGenericType
        {
            get { return false; }
        }

        public bool IsGenericTypeDefinition
        {
            get { return false; }
        }

        public bool IsValueType
        {
            get { return false; }
        }

        public JsonConverter JsonConverter
        {
            get { return null; }
        }

        public Type MappedTypeInstance
        {
            get { return mappedType; }
        }

        public string Name
        {
            get { return name; }
        }

        #endregion

        public bool HasUri
        {
            get { return !MappedAsValueObject; }
        }

        public Type MappedType
        {
            get { return mappedType; }
        }

        public IPropertyInfo PrimaryId { get; set; }

        public IList<IPropertyInfo> Properties
        {
            get { return new CastingListWrapper<IPropertyInfo>(properties); }
        }

        public TypeSerializationMode SerializationMode
        {
            get { return TypeSerializationMode.Complex; }
        }

        public string ConvertToInternalPropertyPath(string externalPath)
        {
            // TODO: Fix for lists, but first gotta find out how that would work..

            string externalPropertyName, remainingExternalPath;
            TakeLeftmostPathPart(externalPath, out externalPropertyName, out remainingExternalPath);

            // TODO: Fix for multiple inherited types with property of same name..
            var prop =
                Properties.Concat(MergedTypes.SelectMany(x => x.Properties)).OfType<PropertyMapping>().FirstOrDefault(
                    x => x.Name.ToLowerInvariant() == externalPropertyName.ToLowerInvariant());
            if (prop == null)
            {
                throw new PomonaMappingException(
                    string.Format(
                        "Could not find property with name {0} on type {1} while resolving path {2}",
                        externalPropertyName,
                        Name,
                        externalPath));
            }

            var internalPropertyName = prop.PropertyInfo.Name;

            if (remainingExternalPath != null)
            {
                var pathType = prop.PropertyType;
                if (pathType.IsCollection)
                    pathType = pathType.ElementType;

                var nextType = (TransformedType) pathType;
                return internalPropertyName + "." + nextType.ConvertToInternalPropertyPath(remainingExternalPath);
            }
            return internalPropertyName;
        }


        public Expression CreateExpressionForExternalPropertyPath(string externalPath)
        {
            if (MappedType == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Can't convert an external property path to expression with source-less type {0} as root!",
                        Name));
            }

            var parameter = Expression.Parameter(MappedType, "x");
            var propertyAccessExpression = CreateExpressionForExternalPropertyPath(parameter, externalPath);

            return Expression.Lambda(propertyAccessExpression, parameter);
        }


        public Expression CreateExpressionForExternalPropertyPath(Expression instance, string externalPath)
        {
            string externalPropertyName, remainingExternalPath;
            TakeLeftmostPathPart(externalPath, out externalPropertyName, out remainingExternalPath);

            var prop =
                Properties.OfType<PropertyMapping>().First(x => x.Name.ToLower() == externalPropertyName.ToLower());

            if (prop.PropertyInfo == null)
            {
                throw new NotImplementedException(
                    "Can only make expression paths for PropertyMappings to a specific internal property (with PropertyInfo)");
            }

            var propertyAccessExpression = Expression.Property(instance, prop.PropertyInfo);

            if (remainingExternalPath != null)
            {
                // TODO Error handling here when remaningpath does not represents a TransformedType
                var transformedPropType = prop.PropertyType as TransformedType;
                if (transformedPropType == null)
                {
                    throw new InvalidOperationException(
                        "Can not filter by subproperty when property is not TransformedType");
                }
                return transformedPropType.CreateExpressionForExternalPropertyPath(
                    propertyAccessExpression, remainingExternalPath);
            }
            return propertyAccessExpression;
        }


        public object GetId(object entity)
        {
            return typeMapper.Filter.GetIdFor(entity);
        }


        public PropertyMapping GetPropertyByJsonName(string jsonPropertyName)
        {
            // TODO: Create a dictionary for this if suboptimal.
            return properties.First(x => x.JsonName == jsonPropertyName);
        }


        public PropertyMapping GetPropertyByName(string propertyName, bool ignoreCase)
        {
            if (ignoreCase)
                propertyName = propertyName.ToLower();

            // TODO: Possible to optimize here by putting property names in a dictionary
            return properties.First(x => x.Name == propertyName);
        }


        /// <summary>
        /// Creates an newinstance of type that TransformedType targets
        /// </summary>
        /// <param name="initValues">Dictionary of initial values, prop names must be lowercased!</param>
        /// <returns></returns>
        public object NewInstance(IDictionary<string, object> initValues)
        {
            // Initvalues must be lowercased!
            // HACK attack! This must probably be rethought..

            var requiredCtorArgCount = ConstructorInfo.GetParameters().Count();
            var ctorArgs = new object[requiredCtorArgCount];

            foreach (
                var ctorProp in
                    properties.Where(
                        x => x.CreateMode == PropertyCreateMode.Required && x.ConstructorArgIndex >= 0))
            {
                // TODO: Proper validation here!
                var value = initValues[ctorProp.Name.ToLower()];

                if (ctorProp.PropertyType.IsBasicWireType)
                    value = Convert.ChangeType(value, ((SharedType) ctorProp.PropertyType).MappedType);

                ctorArgs[ctorProp.ConstructorArgIndex] = value;
            }

            var newInstance = Activator.CreateInstance(mappedType, ctorArgs);

            foreach (var optProp in Properties.Where(x => x.CreateMode == PropertyCreateMode.Optional))
            {
                object propSetValue;
                if (initValues.TryGetValue(optProp.Name.ToLower(), out propSetValue))
                    optProp.Setter(newInstance, propSetValue);
            }

            return newInstance;
        }


        public void TakeLeftmostPathPart(string path, out string leftName, out string remainingPropPath)
        {
            var leftPathSeparatorIndex = path.IndexOf('.');
            if (leftPathSeparatorIndex == -1)
            {
                leftName = path;
                remainingPropPath = null;
            }
            else
            {
                leftName = path.Substring(0, leftPathSeparatorIndex);
                remainingPropPath = path.Substring(leftPathSeparatorIndex + 1);
            }
        }


        internal void ScanProperties(Type type)
        {
            var filter = typeMapper.Filter;

            var scannedProperties = GetPropertiesToScanOrderedByName(type).ToList();

            foreach (var propInfo in scannedProperties)
            {
                if (!filter.PropertyIsIncluded(propInfo))
                    continue;

                IMappedType declaringType;

                if (typeMapper.SourceTypes.Contains(propInfo.DeclaringType))
                    declaringType = typeMapper.GetClassMapping(propInfo.DeclaringType);
                else
                {
                    // TODO: Find lowest base type with this property
                    declaringType = this;
                }

                var propInfoLocal = propInfo;
                var getter = filter.GetPropertyGetter(propInfo);
                var setter = filter.GetPropertySetter(propInfo);
                var propertyType = filter.GetPropertyType(propInfo);
                var propertyTypeMapped = typeMapper.GetClassMapping(propertyType);

                var propDef = new PropertyMapping(
                    typeMapper.Filter.GetPropertyMappedName(propInfo),
                    (TransformedType) declaringType,
                    propertyTypeMapped,
                    propInfo);

                propDef.Getter = getter;
                propDef.Setter = setter;
                propDef.AlwaysExpand = filter.PropertyIsAlwaysExpanded(propInfo);
                if (filter.PropertyIsPrimaryId(propInfo))
                    PrimaryId = propDef;

                // TODO: Fix this for transformed properties with custom get/set methods.
                // TODO: This should rather be configured by filter.
                if (propInfoLocal.CanWrite && propInfoLocal.GetSetMethod() != null)
                {
                    propDef.CreateMode = PropertyCreateMode.Optional;
                    propDef.AccessMode = PropertyMapping.PropertyAccessMode.ReadWrite;
                }
                else
                {
                    propDef.CreateMode = PropertyCreateMode.Excluded;
                    propDef.AccessMode = PropertyMapping.PropertyAccessMode.ReadOnly;
                }

                properties.Add(propDef);
            }

            // TODO: Support not including the whole type hierarchy. Remember that base type might be allowed to be a shared type.
            if (type.BaseType != null)
                BaseType = typeMapper.GetClassMapping(type.BaseType);

            // Find longest (most specific) public constructor
            var constructor = typeMapper.Filter.GetTypeConstructor(type);
            ConstructorInfo = constructor;

            if (constructor != null)
            {
                // TODO: match constructor arguments
                foreach (var ctorParam in constructor.GetParameters())
                {
                    var matchingProperty =
                        properties.FirstOrDefault(x => x.JsonName.ToLower() == ctorParam.Name.ToLower());
                    if (matchingProperty != null)
                    {
                        matchingProperty.CreateMode = PropertyCreateMode.Required;
                        matchingProperty.ConstructorArgIndex = ctorParam.Position;
                    }
                }
            }
        }


        private static IEnumerable<PropertyInfo> GetPropertiesToScanOrderedByName(Type type)
        {
            return
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(
                    x => x.Name).Where(x => x.GetIndexParameters().Count() == 0);
        }
    }
}