﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2012 Karsten Nikolai Strand
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
using System.Linq.Expressions;

namespace Pomona.Client
{
    public class ClientRepository<TResource, TPostResponseResource>
        where TResource : IClientResource
        where TPostResponseResource : IClientResource
    {
        private readonly ClientBase client;


        public ClientRepository(ClientBase client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            this.client = client;
        }


        public TPostResponseResource Post(Action<TResource> postAction)
        {
            return (TPostResponseResource)this.client.Post(postAction);
        }


        public IList<TResource> Query(
            Expression<Func<TResource, bool>> predicate, string expand = null, int? top = null, int? skip = null)
        {
            return this.client.Query(predicate, expand, top, skip);
        }
    }
}