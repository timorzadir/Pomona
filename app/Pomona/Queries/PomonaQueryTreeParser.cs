﻿// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antlr.Runtime.Tree;

namespace Pomona.Queries
{
    internal class PomonaQueryTreeParser
    {
        private static readonly HashSet<NodeType> binaryNodeTypes;
        private static readonly Dictionary<int, NodeType> nodeTypeDict;


        static PomonaQueryTreeParser()
        {
            binaryNodeTypes = new HashSet<NodeType>
                {
                    NodeType.AndAlso,
                    NodeType.OrElse,
                    NodeType.Multiply,
                    NodeType.Modulo,
                    NodeType.Add,
                    NodeType.Div,
                    NodeType.Subtract,
                    NodeType.GreaterThan,
                    NodeType.LessThan,
                    NodeType.GreaterThanOrEqual,
                    NodeType.LessThanOrEqual,
                    NodeType.Equal,
                    NodeType.NotEqual,
                    NodeType.Dot,
                    NodeType.As,
                    NodeType.In
                };
            nodeTypeDict = new Dictionary<int, NodeType>
                {
                    {PomonaQueryParser.LT_OP, NodeType.LessThan},
                    {PomonaQueryParser.EQ_OP, NodeType.Equal},
                    {PomonaQueryParser.NE_OP, NodeType.NotEqual},
                    {PomonaQueryParser.GT_OP, NodeType.GreaterThan},
                    {PomonaQueryParser.GE_OP, NodeType.GreaterThanOrEqual},
                    {PomonaQueryParser.LE_OP, NodeType.LessThanOrEqual},
                    {PomonaQueryParser.ADD_OP, NodeType.Add},
                    {PomonaQueryParser.SUB_OP, NodeType.Subtract},
                    {PomonaQueryParser.AND_OP, NodeType.AndAlso},
                    {PomonaQueryParser.OR_OP, NodeType.OrElse},
                    {PomonaQueryParser.MUL_OP, NodeType.Multiply},
                    {PomonaQueryParser.DIV_OP, NodeType.Div},
                    {PomonaQueryParser.MOD_OP, NodeType.Modulo},
                    {PomonaQueryParser.DOT_OP, NodeType.Dot},
                    {PomonaQueryParser.AS_OP, NodeType.As},
                    {PomonaQueryParser.STRING, NodeType.StringLiteral},
                    {PomonaQueryParser.IN_OP, NodeType.In}
                };
        }


        public static NodeBase ParseTree(ITree tree, int depth, string parsedString)
        {
            var node = ParseTreeInner(tree, depth, parsedString);
            node.ParserNode = tree;
            return node;
        }

        private static NodeBase ParseTreeInner(ITree tree, int depth, string parsedString)
        {
            depth++;

            if (tree.Type == 0)
                throw QueryParseException.Create(tree, "Parse error", parsedString, null);

            if (tree.Type == PomonaQueryParser.PREFIXED_STRING)
            {
                var text = tree.Text;
                var stringPartStart = text.IndexOf('\'');
                if (stringPartStart == -1)
                    throw new InvalidOperationException("Unable to parse prefixed literal.");

                var prefix = text.Substring(0, stringPartStart);
                var value = text.Substring(stringPartStart + 1, text.Length - stringPartStart - 2);

                switch (prefix)
                {
                    case "t":
                        return new TypeNameNode(value);
                    case "guid":
                        return new GuidNode(Guid.Parse(value));
                    case "datetime":
                        return
                            new DateTimeNode(
                                DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                }
            }

            if (IsReduceableBinaryOperator(tree.Type) && tree.ChildCount == 1)
                return ParseTree(tree.GetChild(0), depth, parsedString);

            switch (tree.Type)
            {
                case PomonaQueryParser.NOT_OP:
                    return new NotNode(ParseChildren(tree, depth, parsedString));
                case PomonaQueryParser.METHOD_CALL:
                    return new MethodCallNode(tree.GetChild(0).Text, ParseChildren(tree, depth, parsedString, 1));
                case PomonaQueryParser.INDEXER_ACCESS:
                    return new IndexerAccessNode(tree.GetChild(0).Text, ParseChildren(tree, depth, parsedString, 1));
                case PomonaQueryParser.INT:
                    return new NumberNode(tree.Text);
                case PomonaQueryParser.STRING:
                    return new StringNode(tree.Text);
                case PomonaQueryParser.ID:
                    return new SymbolNode(tree.Text, ParseChildren(tree, depth, parsedString));
                case PomonaQueryParser.ROOT:
                    return ParseTree(tree.GetChild(0), depth, parsedString);
                case PomonaQueryParser.LAMBDA_OP:
                    return new LambdaNode(ParseChildren(tree, depth, parsedString));
                case PomonaQueryParser.ARRAY_LITERAL:
                    return new ArrayNode(ParseChildren(tree, depth, parsedString));
            }

            NodeType nodeType;
            if (IsBinaryOperator(tree.Type, out nodeType))
            {
                var childNodes = new Queue<NodeBase>(ParseChildren(tree, depth, parsedString));

                var expr = childNodes.Dequeue();
                while (childNodes.Count > 0)
                    expr = new BinaryOperatorNode(nodeType, new[] {expr, childNodes.Dequeue()});
                return expr;
            }

            return new UnhandledNode(tree);
        }


        private static IEnumerable<ITree> GetChildren(ITree tree)
        {
            var childCount = tree.ChildCount;
            for (var i = 0; i < childCount; i++)
                yield return tree.GetChild(i);
        }


        private static bool IsBinaryOperator(int type, out NodeType nodeType)
        {
            nodeType = nodeTypeDict[type];
            return binaryNodeTypes.Contains(nodeType);
        }


        private static bool IsReduceableBinaryOperator(int type)
        {
            switch (type)
            {
                case PomonaQueryParser.ORDERBY_ASC:
                case PomonaQueryParser.AS_OP:
                case PomonaQueryParser.LAMBDA_OP:
                case PomonaQueryParser.AND_OP:
                case PomonaQueryParser.OR_OP:
                    return true;
            }

            return false;
        }


        private static IEnumerable<NodeBase> ParseChildren(ITree tree, int depth, string parsedString, int skip = 0)
        {
            return GetChildren(tree).Skip(skip).Select(x => ParseTree(x, depth + 1, parsedString));
        }


        //Expression 
    }
}