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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSemanticModelAction(analysisContext =>
            {
                var syntaxTrees = analysisContext.SemanticModel.Compilation.SyntaxTrees;
                foreach (var syntaxTree in syntaxTrees)
                {
                    var model = analysisContext.SemanticModel.Compilation.GetSemanticModel(syntaxTree);
                    var root = syntaxTree.GetRoot(analysisContext.CancellationToken);
                    
                    foreach (var statement in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
                    {
                        foreach (var variableDeclaratorSyntax in statement.Declaration.Variables)
                        {
                            var identifierName = variableDeclaratorSyntax.Identifier.ToString();
                            if (identifierName.Length <= 3) continue;
                            if (variableDeclaratorSyntax.Identifier.ToString().Substring(0, 3) != "not") continue;                            if (variableDeclaratorSyntax.Identifier.ToString().Substring(0, 3) != "not") continue;
                            if (char.IsLower(variableDeclaratorSyntax.Identifier.ToString()[3])) continue;

                            var type = model.GetTypeInfo(variableDeclaratorSyntax.Initializer.Value).Type
                                .ToDisplayString();
                            if (type != "bool") continue;
                            var diagnostic = Diagnostic.Create(Rule,
                                statement.Declaration.Variables.First().Identifier.GetLocation(),
                                statement.Declaration.Variables.First().Identifier.ToString());
                            analysisContext.ReportDiagnostic(diagnostic);
                        }
                    }

                    foreach (var argument in root.DescendantNodes().OfType<ParameterSyntax>())
                    {
                        var identifierName = argument.Identifier.ToString();
                        if (identifierName.Length <= 3) continue;
                        if (argument.Identifier.ToString().Substring(0, 3) != "not") continue;

                        if (argument.Type.ToString() != "bool")
                            continue;

                        var diagnostic = Diagnostic.Create(Rule, argument.Identifier.GetLocation(),
                            argument.Identifier.ToString());
                        analysisContext.ReportDiagnostic(diagnostic);
                    }
                }
            });
           
        }
    }
}
