using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SmashTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsedWithReflectionAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "VF0001";

  private const string Category = "Usage";
  private const string AttributeNamespace = "JetBrains.Annotations";
  private const string AttributeName = "UsedWithReflection";
  private const string AttributeNameWithSuffix = "UsedWithReflectionAttribute";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Member is reserved for reflection use",
    "'{0}' is marked with UsedWithReflection and should only be referenced through reflection",
    Category,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Methods and constructors marked with UsedWithReflection are intended to be called " +
                 "through reflection, not directly.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
  }

  private static void AnalyzeInvocation(OperationAnalysisContext context)
  {
    IInvocationOperation invocation = (IInvocationOperation)context.Operation;
    ReportIfUsedWithReflection(context, invocation.TargetMethod, invocation.Syntax.GetLocation());
  }

  private static void AnalyzeObjectCreation(OperationAnalysisContext context)
  {
    IObjectCreationOperation objectCreation = (IObjectCreationOperation)context.Operation;
    ReportIfUsedWithReflection(context, objectCreation.Constructor, objectCreation.Syntax.GetLocation());
  }

  private static void ReportIfUsedWithReflection(OperationAnalysisContext context, IMethodSymbol method, Location location)
  {
    if (method is null || !HasUsedWithReflectionAttribute(method))
      return;

    Diagnostic diagnostic = Diagnostic.Create(Rule, location, method.ToDisplayString());
    context.ReportDiagnostic(diagnostic);
  }

  private static bool HasUsedWithReflectionAttribute(ISymbol symbol)
  {
    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      if (IsUsedWithReflectionAttribute(attribute.AttributeClass))
      {
        return true;
      }
    }
    return false;
  }

  private static bool IsUsedWithReflectionAttribute(INamedTypeSymbol attributeClass)
  {
    if (attributeClass is null)
      return false;

    return attributeClass.ContainingNamespace.ToDisplayString() is AttributeNamespace &&
      (attributeClass.MetadataName is AttributeName or AttributeNameWithSuffix);
  }
}
