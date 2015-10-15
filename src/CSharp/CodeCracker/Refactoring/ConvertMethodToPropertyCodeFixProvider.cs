using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBracesToSwitchSectionsCodeFixProvider)), Shared]
    public class ConvertMethodToPropertyCodeFixProvider : CodeFixProvider
    {
        private static readonly SyntaxToken semicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ConvertMethodToProperty.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Add braces to each switch section", ct => MakeMethodPropertyAsync(context.Document, diagnostic, ct), nameof(ConvertMethodToPropertyCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Solution> MakeMethodPropertyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var method = (MethodDeclarationSyntax)root.FindNode(diagnosticSpan);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            var methodClassName = methodSymbol.ContainingType.Name;
            var references = await SymbolFinder.FindReferencesAsync(methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
            var documentGroups = references.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);
            var newSolution = UpdateMainDocument(document, root, method, documentGroups);
            return newSolution;
        }

        private static Solution UpdateMainDocument(Document document, SyntaxNode root, MethodDeclarationSyntax method, IEnumerable<IGrouping<Document, ReferenceLocation>> documentGroups)
        {
            var mainDocGroup = documentGroups.FirstOrDefault(dg => dg.Key.Equals(document));
            SyntaxNode newRoot;
            if (mainDocGroup == null)
            {
                var propertyWithoutBody = SyntaxFactory.PropertyDeclaration(method.ReturnType, method.Identifier).WithModifiers(method.Modifiers).WithAdditionalAnnotations(Formatter.Annotation);
                PropertyDeclarationSyntax property;
                if (method.ExpressionBody == null)
                {
                    var accessorDeclarationSyntax = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, method.Body);
                    if (method.Body == null)
                    {
                        accessorDeclarationSyntax = accessorDeclarationSyntax.WithSemicolonToken(semicolonToken);
                    }
                    property = propertyWithoutBody.
                        WithAccessorList(
                            SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>().Add
                                (
                                    accessorDeclarationSyntax
                                )
                                )
                        );
                }
                else
                {
                    property = propertyWithoutBody.WithExpressionBody(method.ExpressionBody);
                }
                newRoot = root.ReplaceNode(method, property.WithTriviaFrom(method));
            }
            else
            {
                throw new NotImplementedException();

            }
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            return newSolution;
        }
    }
}
