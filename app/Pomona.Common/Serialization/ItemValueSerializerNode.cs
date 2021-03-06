#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2013 Karsten Nikolai Strand
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

using Pomona.Common.TypeSystem;

namespace Pomona.Common.Serialization
{
    public class ItemValueSerializerNode : ISerializerNode
    {
        private readonly ISerializationContext context;

        private readonly string expandPath;
        private readonly TypeSpec expectedBaseType;
        private readonly bool isRemoved;
        private readonly ISerializerNode parentNode;
        private readonly object value;
        private TypeSpec valueType;

        #region Implementation of ISerializerNode

        public ItemValueSerializerNode(
            object value, TypeSpec expectedBaseType, string expandPath, ISerializationContext context,
            ISerializerNode parentNode, bool isRemoved = false)
        {
            this.value = value;
            this.expectedBaseType = expectedBaseType;
            this.expandPath = expandPath;
            this.context = context;
            this.parentNode = parentNode;
            this.isRemoved = isRemoved;
        }


        public ISerializationContext Context
        {
            get { return context; }
        }

        public string ExpandPath
        {
            get { return expandPath; }
        }

        public TypeSpec ExpectedBaseType
        {
            get { return expectedBaseType; }
        }

        public bool SerializeAsReference { get; set; }

        public string Uri
        {
            get { return Context.GetUri(Value); }
        }

        public object Value
        {
            get { return value; }
        }

        public TypeSpec ValueType
        {
            get
            {
                if (valueType == null)
                    valueType = context.GetClassMapping(Value.GetType());
                return valueType;
            }
        }

        public ISerializerNode ParentNode
        {
            get { return parentNode; }
        }

        #endregion

        public bool IsRemoved
        {
            get { return isRemoved; }
        }
    }
}