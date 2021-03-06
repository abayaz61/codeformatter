// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [SyntaxRuleOrder(SyntaxRuleOrder.HasUsingsOutsideOfNamespaceFormattingRule)]
    internal sealed class HasUsingsOutsideOfNamespaceFormattingRule : ISyntaxFormattingRule
    {
        public SyntaxNode Process(SyntaxNode syntaxNode)
        {
            var root = syntaxNode as CompilationUnitSyntax;
            if (root == null)
                return syntaxNode;

            var newRoot = root;
            while (true)
            {
                var namespaceWithUsings = newRoot.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Any());
                if (namespaceWithUsings == null)
                    break;

                // Moving a using with an alias out of a namespace is an operation which requires
                // semantic knowledge to get correct.
                if (namespaceWithUsings.Usings.Any(x => x.Alias != null))
                    return syntaxNode;

                // Remove nested usings

                var emptyUsingList = SyntaxFactory.List<UsingDirectiveSyntax>();
                var namespaceWithoutUsings = namespaceWithUsings.WithUsings(emptyUsingList);
                newRoot = newRoot.ReplaceNode(namespaceWithUsings, namespaceWithoutUsings);

                // Add usings to compilation unit

                var usings = namespaceWithUsings.Usings.ToArray();

                if (!newRoot.Usings.Any())
                {
                    // Specialize the case where there are no usings yet.
                    //
                    // We want to make sure that leading triviva becomes leading trivia
                    // of the first using. 

                    usings[0] = usings.First().WithLeadingTrivia(newRoot.GetLeadingTrivia());
                    newRoot = newRoot.WithLeadingTrivia(Enumerable.Empty<SyntaxTrivia>());

                    // We want the last using to be separated from the namespace keyword
                    // by a blank line.

                    var trailingTrivia = usings.Last().GetTrailingTrivia();
                    var linebreak = new[] { SyntaxFactory.CarriageReturnLineFeed };
                    usings[usings.Length - 1] = usings.Last().WithTrailingTrivia(trailingTrivia.Concat(linebreak));
                }

                newRoot = newRoot.AddUsings(usings);
            }

            return newRoot;
        }
    }
}