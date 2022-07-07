using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerTemplate
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerTemplateAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AnalyzerTemplate";
        
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSemanticModelAction(analysisContext =>
            {
                var syntaxTrees = analysisContext.SemanticModel.Compilation.SyntaxTrees;
               

                foreach (var syntaxTree in syntaxTrees)
                {
                    var model = analysisContext.SemanticModel.Compilation.GetSemanticModel(syntaxTree);
                    var root = syntaxTree.GetRoot(analysisContext.CancellationToken);

                    foreach (var statement in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
                    {
                        if (!statement.IsKind(SyntaxKind.EqualsExpression)) continue;
                        if (statement.OperatorToken.ToString() != "==") continue;
                        var left = statement.Left;
                        var right = statement.Right;

                        if (right.IsKind(SyntaxKind.NullLiteralExpression) ||
                            left.IsKind(SyntaxKind.NullLiteralExpression))
                            continue;

                        var leftTypeInfo = model.GetTypeInfo(left, analysisContext.CancellationToken);
                        var rightTypeInfo = model.GetTypeInfo(right, analysisContext.CancellationToken);
                        var isLeftTypeOverloaded = true;
                        var leftMethods = leftTypeInfo.ConvertedType.GetMembers();
                        var rightMethods = new ImmutableArray<ISymbol>();
                        
                        if (!SymbolEqualityComparer.Default.Equals(leftTypeInfo.ConvertedType, rightTypeInfo.ConvertedType))
                        {
                            rightMethods = rightTypeInfo.Type.GetMembers();
                        }
                        
                        var availableOverloadsFromLeft = new List<SeparatedSyntaxList<ParameterSyntax>>();
                        var availableOverloadsFromRight = new List<SeparatedSyntaxList<ParameterSyntax>>();
                        

                        foreach (var method in leftMethods)
                        {
                            if (method.Kind != SymbolKind.Method) continue;
                            foreach (var declaration in method.DeclaringSyntaxReferences.Select(reference =>
                                         reference.SyntaxTree.GetRoot().FindNode(reference.Span)))
                            {
                                if (!(declaration is OperatorDeclarationSyntax operatorSyntax)) continue;
                                if (operatorSyntax.OperatorToken.ToString() == "==")
                                {
                                    availableOverloadsFromLeft.Add(operatorSyntax.ParameterList.Parameters);
                                }
                            }
                        }
                        if (rightMethods != null)
                        {
                            foreach (var method in rightMethods)
                            {
                                if (method.Kind != SymbolKind.Method) continue;
                                foreach (var declaration in method.DeclaringSyntaxReferences.Select(reference =>
                                             reference.SyntaxTree.GetRoot().FindNode(reference.Span)))
                                {
                                    if (!(declaration is OperatorDeclarationSyntax operatorSyntax)) continue;
                                    if (operatorSyntax.OperatorToken.ToString() == "==")
                                    {
                                        availableOverloadsFromRight.Add(operatorSyntax.ParameterList.Parameters);
                                    }
                                }
                            }}

                        var isOverloaded = false;
                        foreach (var availableOverload in availableOverloadsFromRight)
                        {
                            var firstType = model.GetTypeInfo(availableOverload[0].Type,
                                analysisContext.CancellationToken);
                            var secondType = model.GetTypeInfo(availableOverload[1].Type,
                                analysisContext.CancellationToken);
                                  
                            if (SymbolEqualityComparer.Default.Equals(firstType.ConvertedType, leftTypeInfo.ConvertedType) &&
                                SymbolEqualityComparer.Default.Equals(secondType.ConvertedType, rightTypeInfo.ConvertedType))
                            {
                                isLeftTypeOverloaded = false;
                                isOverloaded = true;
                                break;
                            }
                        }
                        
                        foreach (var availableOverload in availableOverloadsFromLeft)
                        {
                            var firstType = model.GetTypeInfo(availableOverload[0].Type,
                                analysisContext.CancellationToken);
                            var secondType = model.GetTypeInfo(availableOverload[1].Type,
                                analysisContext.CancellationToken);
                                  
                            if (SymbolEqualityComparer.Default.Equals(firstType.ConvertedType, leftTypeInfo.ConvertedType) &&
                                SymbolEqualityComparer.Default.Equals(secondType.ConvertedType, rightTypeInfo.ConvertedType))
                            {
                                isOverloaded = true;
                                break;
                            }
                        }

                        if (isOverloaded) continue;
                                
                        var diagnostic = Diagnostic.Create(Rule, left.GetLocation(),
                            $"{leftTypeInfo.Type.ToDisplayString()} and {rightTypeInfo.Type.ToDisplayString()}");
                        analysisContext.ReportDiagnostic(diagnostic);
                    }

                }
            });
        }
    }
}
