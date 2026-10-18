using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SmashTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HarmonyPatchDelegateAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "VF0004";

  private const string HarmonyMethodName = "HarmonyLib.HarmonyMethod";
  private const string DelegateName = "System.Delegate";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Harmony patch delegates must reference static methods",
    "Harmony patch delegate must directly reference a named static method",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "HarmonyMethod stores a delegate's MethodInfo but not its target. Anonymous functions and " +
                 "instance methods can therefore generate an invalid patch wrapper. Pass a named static method instead.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol harmonyMethod = startContext.Compilation.GetTypeByMetadataName(HarmonyMethodName);
      INamedTypeSymbol delegateType = startContext.Compilation.GetTypeByMetadataName(DelegateName);
      if (harmonyMethod is null || delegateType is null)
        return;

      startContext.RegisterOperationAction(operationContext => AnalyzeCreation(operationContext, harmonyMethod,
        delegateType), OperationKind.ObjectCreation);
      startContext.RegisterOperationAction(operationContext => AnalyzeConversion(operationContext, harmonyMethod,
        delegateType), OperationKind.Conversion);
    });
  }

  private static void AnalyzeCreation(OperationAnalysisContext context, INamedTypeSymbol harmonyMethod,
    INamedTypeSymbol delegateType)
  {
    IObjectCreationOperation creation = (IObjectCreationOperation)context.Operation;
    if (!IsDelegateOverload(creation.Constructor, harmonyMethod, delegateType))
      return;

    foreach (IArgumentOperation argument in creation.Arguments)
    {
      if (argument.Parameter?.Ordinal != 0)
        continue;

      ReportIfUnsafe(context, argument.Value, argument.Syntax.GetLocation());
      return;
    }
  }

  private static void AnalyzeConversion(OperationAnalysisContext context, INamedTypeSymbol harmonyMethod,
    INamedTypeSymbol delegateType)
  {
    IConversionOperation conversion = (IConversionOperation)context.Operation;
    IMethodSymbol method = conversion.OperatorMethod;
    if (method is null || method.Name != "op_Implicit" ||
        !IsDelegateOverload(method, harmonyMethod, delegateType))
      return;

    ReportIfUnsafe(context, conversion.Operand, conversion.Operand.Syntax.GetLocation());
  }

  private static bool IsDelegateOverload(IMethodSymbol method, INamedTypeSymbol harmonyMethod,
    INamedTypeSymbol delegateType)
  {
    return method is not null && method.Parameters.Length > 0 &&
           SymbolEqualityComparer.Default.Equals(method.ContainingType, harmonyMethod) &&
           SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, delegateType);
  }

  private static void ReportIfUnsafe(OperationAnalysisContext context, IOperation value, Location location)
  {
    if (value is null)
      return;

    while (true)
    {
      switch (value)
      {
        case IConversionOperation conversion when conversion.OperatorMethod is null:
          value = conversion.Operand;
          break;
        case IDelegateCreationOperation delegateCreation:
          value = delegateCreation.Target;
          break;
        case IParenthesizedOperation parenthesized:
          value = parenthesized.Operand;
          break;
        case IMethodReferenceOperation methodReference
          when methodReference.Method.IsStatic && methodReference.Method.MethodKind != MethodKind.ReducedExtension:
          return;
        default:
          context.ReportDiagnostic(Diagnostic.Create(Rule, location));
          return;
      }
    }
  }
}
