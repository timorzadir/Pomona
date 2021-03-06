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

using Pomona.Common.TypeSystem;

namespace Pomona.Common.Serialization
{
    public class ItemValueDeserializerNode : IDeserializerNode
    {
        private readonly IDeserializationContext context;
        private readonly string expandPath;
        private readonly TypeSpec expectedBaseType;
        private readonly IDeserializerNode parent;
        public TypeSpec valueType;

        #region Implementation of IDeserializerNode

        public ItemValueDeserializerNode(TypeSpec expectedBaseType, IDeserializationContext context,
                                         string expandPath = "", IDeserializerNode parent = null)
        {
            this.parent = parent;
            this.expectedBaseType = expectedBaseType;
            this.context = context;
            this.expandPath = expandPath;
            valueType = expectedBaseType;
        }


        public IDeserializationContext Context
        {
            get { return context; }
        }

        public TypeSpec ExpectedBaseType
        {
            get { return expectedBaseType; }
        }

        public string ExpandPath
        {
            get { return expandPath; }
        }

        public string Uri { get; set; }

        IResourceNode IResourceNode.Parent
        {
            get { return Parent ?? Context.TargetNode; }
        }

        public object Value { get; set; }

        public void CheckItemAccessRights(HttpMethod method)
        {
        }


        public IDeserializerNode Parent
        {
            get { return parent; }
        }

        public TypeSpec ValueType
        {
            get { return valueType; }
        }

        public DeserializerNodeOperation Operation { get; set; }


        public void SetValueType(string typeName)
        {
            valueType = context.GetTypeByName(typeName);
        }


        public void SetValueType(TypeSpec type)
        {
            valueType = context.GetClassMapping(type);
        }


        public void CheckAccessRights(HttpMethod method)
        {
        }

        public void SetProperty(PropertySpec property, object propertyValue)
        {
            context.SetProperty(this, property, propertyValue);
        }

        #endregion
    }
}