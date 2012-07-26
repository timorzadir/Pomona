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
using System.Reflection;

namespace Pomona
{
    public interface ITypeMappingFilter
    {
        object GetIdFor(object entity);


        /// <summary>
        /// Gets a list of all types to CONSIDER for inclusion.
        /// (they will be filtered first)
        /// </summary>
        /// <returns>List of types considered for mapping.</returns>
        IEnumerable<Type> GetSourceTypes();


        /// <summary>
        /// This returns what URI this type will be mapped to.
        /// For example if this method returns the type Animal when passed Dog
        /// it means that dogs will be available on same url as Animal.
        /// (ie. http://somehost/animal/{id}, not http://somehost/dog/{id})
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Type GetUriBaseType(Type type);

        Type ResolveRealTypeForProxy(Type type);
        bool PropertyIsIncluded(PropertyInfo propertyInfo);
        string GetPropertyMappedName(PropertyInfo propertyInfo);
        bool TypeIsMapped(Type type);
        bool TypeIsMappedAsCollection(Type type);
        bool TypeIsMappedAsSharedType(Type type);
        bool TypeIsMappedAsTransformedType(Type type);
        bool TypeIsMappedAsValueObject(Type type);
        Func<object, object> GetPropertyGetter(PropertyInfo propertyInfo);
        Action<object, object> GetPropertySetter(PropertyInfo propertyInfo);
        Type GetPropertyType(PropertyInfo propertyInfo);
    }
}