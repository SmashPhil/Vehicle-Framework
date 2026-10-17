using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SmashTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AccessToolsConstructorAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "VF0003";

  private const string Category = "Usage";
  private const string AccessToolsName = "HarmonyLib.AccessTools";
  private const string MethodName = "Constructor";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "AccessTools.Constructor must not be used",
    "Use AccessTools.DeclaredConstructor instead of AccessTools.Constructor",
    Category,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "AccessTools.Constructor may resolve to inherited constructors, use AccessTools.DeclaredConstructor instead.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol accessTools = startContext.Compilation.GetTypeByMetadataName(AccessToolsName);
      if (accessTools is null)
        return;

      startContext.RegisterOperationAction(operationContext => AnalyzeInvocation(operationContext, accessTools),
        OperationKind.Invocation);
    });
  }

  private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol accessTools)
  {
    IInvocationOperation invocation = (IInvocationOperation)context.Operation;
    IMethodSymbol method = invocation.TargetMethod;
    if (method.Name != MethodName ||
        !SymbolEqualityComparer.Default.Equals(method.ContainingType, accessTools))
      return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
  }
}
