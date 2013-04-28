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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Pomona.Common.TypeSystem;
using Pomona.Internals;

namespace Pomona.Common.Serialization.Json
{
    // Provides custom rules for serialization

    public class PomonaJsonSerializer : ISerializer<PomonaJsonSerializer.Writer>
    {
        private readonly IDictionary<IMappedType, PomonaJsonSerializerTypeEntry> typeCache;
        private bool inlineDocumentationEnabled = true;

        internal PomonaJsonSerializer(IDictionary<IMappedType, PomonaJsonSerializerTypeEntry> typeCache)
        {
            if (typeCache == null)
                throw new ArgumentNullException("typeCache");
            this.typeCache = typeCache;
        }

        #region Implementation of ISerializer<PomonaJsonSerializerState>

        private static readonly MethodInfo serializeDictionaryGenericMethod =
            ReflectionHelper.GetGenericMethodDefinition<PomonaJsonSerializer>(
                x => x.SerializeDictionaryGeneric<object, object>(null, null));

        public Writer CreateWriter(TextWriter textWriter)
        {
            return new Writer(textWriter);
        }


        public void SerializeNode(ISerializerNode node, ISerializerWriter writer)
        {
            SerializeNode(node, CastWriter(writer));
        }


        public void SerializeNode(ISerializerNode node, Writer writer)
        {
            if (node.Value == null)
            {
                writer.JsonWriter.WriteNull();
                return;
            }

            var mappedType = node.ExpectedBaseType ?? node.ValueType;
            switch (mappedType.SerializationMode)
            {
                case TypeSerializationMode.Dictionary:
                    SerializeDictionary(node, writer);
                    break;
                case TypeSerializationMode.Complex:
                    SerializeComplex(node, writer);
                    break;
                case TypeSerializationMode.Array:
                    SerializeCollection(node, writer);
                    break;
                case TypeSerializationMode.Value:
                    SerializeValue(node, writer);
                    break;
            }
        }

        private static void SerializeValue(ISerializerNode node, Writer writer)
        {
            var value = node.Value;

            var boxValueWithTypeSpec = node.ExpectedBaseType != null &&
                                       node.ExpectedBaseType.MappedTypeInstance == typeof (object) &&
                                       (!(node.Value is string));

            if (boxValueWithTypeSpec)
            {
                writer.JsonWriter.WriteStartObject();
                writer.JsonWriter.WritePropertyName("_type");
                writer.JsonWriter.WriteValue(node.ValueType.Name);
                writer.JsonWriter.WritePropertyName("value");
            }

            var jsonConverter = node.ValueType.JsonConverter;
            if (jsonConverter != null)
                jsonConverter.WriteJson(writer.JsonWriter, value, null);
            else
                writer.JsonWriter.WriteValue(value);

            if (boxValueWithTypeSpec)
                writer.JsonWriter.WriteEndObject();
        }


        public void SerializeQueryResult(
            QueryResult queryResult, ISerializationContext fetchContext, ISerializerWriter writer,
            IMappedType elementType)
        {
            SerializeQueryResult(queryResult, fetchContext, CastWriter(writer), elementType);
        }


        public void SerializeQueryResult(
            QueryResult queryResult, ISerializationContext fetchContext, Writer writer, IMappedType elementType)
        {
            var jsonWriter = writer.JsonWriter;
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("_type");
            jsonWriter.WriteValue("__result__");

            jsonWriter.WritePropertyName("totalCount");
            jsonWriter.WriteValue(queryResult.TotalCount);

            jsonWriter.WritePropertyName("count");
            jsonWriter.WriteValue(queryResult.Count);

            Uri previousPageUri;
            if (queryResult.TryGetPage(-1, out previousPageUri))
            {
                jsonWriter.WritePropertyName("previous");
                jsonWriter.WriteValue(previousPageUri.ToString());
            }

            Uri nextPageUri;
            if (queryResult.TryGetPage(1, out nextPageUri))
            {
                jsonWriter.WritePropertyName("next");
                jsonWriter.WriteValue(nextPageUri.ToString());
            }

            jsonWriter.WritePropertyName("items");
            var itemNode = new ItemValueSerializerNode(queryResult, fetchContext.GetClassMapping(queryResult.ListType),
                                                       string.Empty, fetchContext);
            itemNode.Serialize(this, writer);

            if (queryResult.DebugInfo.Count > 0)
            {
                jsonWriter.WritePropertyName("debugInfo");
                jsonWriter.WriteStartObject();
                foreach (var kvp in queryResult.DebugInfo)
                {
                    jsonWriter.WritePropertyName(kvp.Key);
                    jsonWriter.WriteValue(kvp.Value);
                }
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
        }


        private static void SerializeReference(ISerializerNode node, Writer writer)
        {
            var jsonWriter = writer.JsonWriter;

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("_ref");
            jsonWriter.WriteValue(node.Uri);
            if (node.ExpectedBaseType != node.ValueType)
            {
                jsonWriter.WritePropertyName("_type");
                jsonWriter.WriteValue(node.ValueType.Name);
            }

            jsonWriter.WriteEndObject();
        }


        private Writer CastWriter(ISerializerWriter writer)
        {
            var castedWriter = writer as Writer;
            if (castedWriter == null)
                throw new ArgumentException("Writer required to be of type PomonaJsonSerializationWriter", "writer");
            return castedWriter;
        }


        private void SerializeCollection(ISerializerNode node, Writer writer)
        {
            var jsonWriter = writer.JsonWriter;
            if (node.SerializeAsReference)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("_ref");
                jsonWriter.WriteValue(node.Uri);
                jsonWriter.WriteEndObject();
            }
            else
            {
                jsonWriter.WriteStartArray();
                var elementType = node.ExpectedBaseType.ElementType;
                foreach (var item in (IEnumerable) node.Value)
                {
                    var itemNode = new ItemValueSerializerNode(item, elementType, node.ExpandPath, node.Context);
                    itemNode.Serialize(this, writer);
                }
                jsonWriter.WriteEndArray();
            }
        }


        private void SerializeComplex(ISerializerNode node, Writer writer)
        {
            if (node.Value == null)
                writer.JsonWriter.WriteNull();
            else if (node.SerializeAsReference)
                SerializeReference(node, writer);
            else
                SerializeExpanded(node, writer);
        }


        private void SerializeDictionary(ISerializerNode node, Writer writer)
        {
            var keyMappedType = node.ExpectedBaseType.DictionaryKeyType.MappedTypeInstance;
            var valueMappedType = node.ExpectedBaseType.DictionaryValueType.MappedTypeInstance;
            serializeDictionaryGenericMethod
                .MakeGenericMethod(keyMappedType, valueMappedType)
                .Invoke(this, new object[] {node, writer});
        }


        private object SerializeDictionaryGeneric<TKey, TValue>(
            ISerializerNode node, Writer writer)
        {
            var jsonWriter = writer.JsonWriter;
            var dict = (IDictionary<TKey, TValue>) node.Value;
            var expectedValueType = node.ExpectedBaseType.DictionaryValueType;

            jsonWriter.WriteStartObject();
            foreach (var kvp in dict)
            {
                // TODO: Support other key types than string
                jsonWriter.WritePropertyName((string) ((object) kvp.Key));
                var itemNode = new ItemValueSerializerNode(kvp.Value, expectedValueType, node.ExpandPath, node.Context);
                itemNode.Serialize(this, writer);
            }
            jsonWriter.WriteEndObject();

            return null;
        }


        private void SerializeExpanded(ISerializerNode node, Writer writer)
        {
            var jsonWriter = writer.JsonWriter;

            jsonWriter.WriteStartObject();
            if (node.ValueType.HasUri)
            {
                jsonWriter.WritePropertyName("_uri");
                jsonWriter.WriteValue(node.Uri);
            }
            if (node.ExpectedBaseType != node.ValueType && !node.ValueType.IsAnonymous())
            {
                jsonWriter.WritePropertyName("_type");
                jsonWriter.WriteValue(node.ValueType.Name);
            }

            PomonaJsonSerializerTypeEntry cacheTypeEntry;
            IEnumerable<IPropertyInfo> propertiesToSerialize = null;
            if (typeCache.TryGetValue(node.ValueType, out cacheTypeEntry))
            {
                cacheTypeEntry.WritePropertiesFunc(jsonWriter, node.Value);
                propertiesToSerialize = cacheTypeEntry.ManuallyWrittenProperties;
            }

            propertiesToSerialize = propertiesToSerialize ?? node.ValueType.Properties;

            var pomonaSerializable = node.Value as IPomonaSerializable;
            if (pomonaSerializable != null)
            {
                propertiesToSerialize = propertiesToSerialize.Where(x => pomonaSerializable.PropertyIsSerialized(x.Name));
            }

            foreach (var prop in propertiesToSerialize)
            {

                jsonWriter.WritePropertyName(prop.JsonName);
                var propNode = new PropertyValueSerializerNode(node, prop);
                propNode.Serialize(this, writer);
            }
            jsonWriter.WriteEndObject();
        }

        #endregion

        #region Implementation of ISerializer

        ISerializerWriter ISerializer.CreateWriter(TextWriter textWriter)
        {
            return CreateWriter(textWriter);
        }

        #endregion

        public class Writer : ISerializerWriter, IDisposable
        {
            private readonly JsonTextWriter jsonWriter;


            public Writer(TextWriter textWriter)
            {
                if (textWriter == null)
                    throw new ArgumentNullException("textWriter");
                jsonWriter = new JsonTextWriter(textWriter) {Formatting = Formatting.Indented};
            }


            public JsonWriter JsonWriter
            {
                get { return jsonWriter; }
            }

            #region Implementation of IDisposable

            public void Dispose()
            {
                // NOTE: Not sure if this is correct
                jsonWriter.Flush();
            }

            #endregion
        }
    }
}