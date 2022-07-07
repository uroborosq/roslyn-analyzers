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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerTemplateCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerTemplate.AnalyzerTemplateAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            context.RegisterCodeFix(
                CodeAction.Create(
                CodeFixResources.CodeFixTitle,
                c => ReverseBoolValuesNames(context.Document, token, c)),
                diagnostic);
        }

        private async Task<Solution> ReverseBoolValuesNames(Document document, SyntaxToken token, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var symbol = model.GetDeclaredSymbol(token.Parent, cancellationToken);
            var newName = char.ToLower(token.ToString()[3]) + token.ToString().Substring(4, token.ToString().Length - 4);
            var solution = document.Project.Solution;
            
            solution = await Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, cancellationToken);
            document = solution.GetDocument(document.Id);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var gen = SyntaxGenerator.GetGenerator(document);
            model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);


            if (token.Parent is ParameterSyntax)
            {
                
            }
            else
            {
                LocalDeclarationStatementSyntax statement = null;
                foreach (var item in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
                {
                    foreach (var variableDeclaratorSyntax in item.Declaration.Variables)
                    {
                        var identifierName = variableDeclaratorSyntax.Identifier.ToString();
                        if (identifierName.Length <= 3) continue;
                        if (variableDeclaratorSyntax.Identifier.ToString() != newName) continue;
                        var type = model.GetTypeInfo(variableDeclaratorSyntax.Initializer.Value).Type.ToDisplayString();
                        if (type != "bool") continue;
                        statement = item;
                        break;
                    }
                }
              
                var oldStatement = statement;
                if (statement.Declaration.Variables.First().Initializer.Value.Kind() == SyntaxKind.TrueLiteralExpression)
                {
                    statement = statement.ReplaceNode(statement.Declaration.Variables.First().Initializer.Value, gen.LiteralExpression(false)); 
                }
                else if (statement.Declaration.Variables.First().Initializer.Value.Kind() == SyntaxKind.FalseLiteralExpression)
                {
                    statement = statement.ReplaceNode(statement.Declaration.Variables.First().Initializer.Value, gen.LiteralExpression(true));
                }
                else
                {
                    var expression = statement.Declaration.Variables.First().Initializer.Value;
                    if (expression.IsKind(SyntaxKind.LogicalNotExpression))
                    {
                        var newExpression = (PrefixUnaryExpressionSyntax) expression;
                        statement = statement.ReplaceNode(expression, newExpression.Operand);
                    }
                    else
                    {
                        var newExpression = gen.LogicalNotExpression(expression);
                        statement = statement.ReplaceNode(expression, newExpression);
                    }
                }
                root = root.ReplaceNode(oldStatement, statement);
            }
            
            
            
           

            SyntaxNode nodeToReplace = null;
            SyntaxNode replacingNode = null;
            var counter = 0;
            while (true)
            {
                var localCounter = 0;
                foreach (var descendantNode in root.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var oldName = descendantNode.Identifier.ValueText;
                
                    if (oldName.Length < 4) continue;
                    if (descendantNode.ToString() != newName) continue;
                    if (localCounter < counter)
                    {
                        localCounter++;
                        continue;
                    }
                    counter++;
                    if (descendantNode.Parent.IsKind(SyntaxKind.LogicalNotExpression))
                    {
                        nodeToReplace = descendantNode.Parent;
                        replacingNode = descendantNode;
                        break;
                    }

                    nodeToReplace = descendantNode;
                    replacingNode = gen.LogicalNotExpression(descendantNode);
                    break;
                }

                if (nodeToReplace is null || replacingNode is null)
                {
                    break;
                }
                root = root.ReplaceNode(nodeToReplace, replacingNode);
                nodeToReplace = null;
                replacingNode = null;
            }
            
            document = document.WithSyntaxRoot(root);
            
            
            
            
            return document.Project.Solution;
        }
    }
}
