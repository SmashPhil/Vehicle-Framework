using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SmashTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssignedFromXmlSuppressor : DiagnosticSuppressor
{
  private const string AttributeNamespace = "JetBrains.Annotations";
  private const string UsedWithReflectionAttributeName = "UsedWithReflection";
  private const string UsedWithReflectionAttributeNameWithSuffix = "UsedWithReflectionAttribute";
  private const string AssignedFromXmlAttributeName = "AssignedFromXml";
  private const string AssignedFromXmlAttributeNameWithSuffix = "AssignedFromXmlAttribute";

  private static readonly SuppressionDescriptor UnassignedField = new(
    "VFSPR0001",
    "CS0649",
    "Fields marked with UsedWithReflection or AssignedFromXml may be assigned externally.");

  public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [UnassignedField];

  public override void ReportSuppressions(SuppressionAnalysisContext context)
  {
    foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
    {
      if (diagnostic.Id != "CS0649")
        continue;

      SyntaxTree tree = diagnostic.Location.SourceTree;
      if (tree is null)
        continue;

      SyntaxNode root = tree.GetRoot(context.CancellationToken);
      SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);
      VariableDeclaratorSyntax variable = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
      if (variable is null)
        continue;

      SemanticModel semanticModel = context.GetSemanticModel(tree);
      if (semanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not IFieldSymbol field)
        continue;

      if (HasExternalAssignmentAttribute(field) ||
        HasAssignedFromXmlAttribute(field.ContainingType))
      {
        context.ReportSuppression(Suppression.Create(UnassignedField, diagnostic));
      }
    }
  }

  private static bool HasExternalAssignmentAttribute(ISymbol symbol)
  {
    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      if (IsUsedWithReflectionAttribute(attribute.AttributeClass) ||
        IsAssignedFromXmlAttribute(attribute.AttributeClass))
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
      (attributeClass.MetadataName is UsedWithReflectionAttributeName or UsedWithReflectionAttributeNameWithSuffix);
  }

  private static bool HasAssignedFromXmlAttribute(ISymbol symbol)
  {
    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      if (IsAssignedFromXmlAttribute(attribute.AttributeClass))
      {
        return true;
      }
    }
    return false;
  }

  private static bool IsAssignedFromXmlAttribute(INamedTypeSymbol attributeClass)
  {
    if (attributeClass is null)
      return false;

    return attributeClass.ContainingNamespace.ToDisplayString() is AttributeNamespace &&
      (attributeClass.MetadataName is AssignedFromXmlAttributeName or AssignedFromXmlAttributeNameWithSuffix);
  }
}
