﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2014 Karsten Nikolai Strand
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

using Pomona.Common.Internals;

namespace Pomona.Common.Linq
{
    public abstract class QueryProviderBase : IQueryProvider
    {
        private static readonly MethodInfo createQueryGenericMethod;


        static QueryProviderBase()
        {
            createQueryGenericMethod =
                ReflectionHelper.GetMethodDefinition<IQueryProvider>(x => x.CreateQuery<object>(null));
        }


        public abstract IQueryable<TElement> CreateQuery<TElement>(Expression expression);
        public abstract object Execute(Expression expression, Type returnType);


        private static Type GetElementType(Type type)
        {
            var queryableTypeInstance = type.GetInterfacesOfGeneric(typeof(IQueryable<>)).FirstOrDefault();
            if (queryableTypeInstance == null)
                return type;

            return queryableTypeInstance.GetGenericArguments()[0];
        }


        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return
                (IQueryable)
                    createQueryGenericMethod.MakeGenericMethod(GetElementType(expression.Type)).Invoke(this,
                        new object[] { expression });
        }


        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S)Execute(expression, typeof(S));
        }


        object IQueryProvider.Execute(Expression expression)
        {
            var exprType = expression.Type;
            Type[] genargs;
            if (exprType.TryExtractTypeArguments(typeof(IOrderedQueryable<>), out genargs))
                exprType = typeof(IOrderedEnumerable<>).MakeGenericType(genargs);
            else if (exprType.TryExtractTypeArguments(typeof(IQueryable<>), out genargs))
                exprType = typeof(IEnumerable<>).MakeGenericType(genargs);
            return Execute(expression, exprType);
        }
    }
}