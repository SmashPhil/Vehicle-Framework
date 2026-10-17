using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SmashTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DisposableWindow : DiagnosticAnalyzer
{
  public const string DiagnosticId = "VF0002";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Windows must not implement IDisposable",
    "'{0}' inherits from Verse.Window and implements System.IDisposable, which is incompatible with the window lifecycle",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Verse.Window removes from the stack without disposing.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol window = startContext.Compilation.GetTypeByMetadataName("Verse.Window");
      INamedTypeSymbol disposable = startContext.Compilation.GetTypeByMetadataName("System.IDisposable");
      if (window is null || disposable is null)
        return;

      startContext.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, window, disposable),
        SymbolKind.NamedType);
    });
  }

  private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol window,
    INamedTypeSymbol disposable)
  {
    INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
    if (type.TypeKind != TypeKind.Class)
      return;

    INamedTypeSymbol baseType = type.BaseType;
    while (baseType is not null)
    {
      if (!SymbolEqualityComparer.Default.Equals(baseType, window))
      {
        baseType = baseType.BaseType;
        continue;
      }

      foreach (INamedTypeSymbol implementedInterface in type.AllInterfaces)
      {
        if (SymbolEqualityComparer.Default.Equals(implementedInterface, disposable))
        {
          context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.ToDisplayString()));
          return;
        }
      }
      return;
    }
  }
}
