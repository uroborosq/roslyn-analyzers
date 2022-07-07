using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerTemplateCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerTemplateAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => ReplaceEqualsOperatorWithEqualsMethod(context.Document, diagnostic, root, c),
                    equivalenceKey:  nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private static Task<Document> ReplaceEqualsOperatorWithEqualsMethod(Document document, Diagnostic diagnostic, SyntaxNode root, CancellationToken cancellationToken)
        {
            var identifier = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<IdentifierNameSyntax>();
            var statement = (BinaryExpressionSyntax)identifier.Parent;

            var first = statement.Left == identifier ? statement.Left : statement.Right;
            var second = statement.Left == identifier ? statement.Right : statement.Left;
            var newExpression = CreateEqualsMethodCall(statement.Left, statement.Right);
            var newRoot = root.ReplaceNode(statement, newExpression);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static InvocationExpressionSyntax CreateEqualsMethodCall(ExpressionSyntax firstIdentifier, ExpressionSyntax argument)
        {
            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        firstIdentifier,
                        SyntaxFactory.IdentifierName("Equals")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument(
                                argument))));
        }
    }
}
