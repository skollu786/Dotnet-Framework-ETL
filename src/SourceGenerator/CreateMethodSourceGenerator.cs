using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

[Generator]
public class SqliteConversionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(ctx => new SqliteSyntaxReceiver());
        context.RegisterForSyntaxNotifications(() => new SqliteSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var syntaxReceiver = (SqliteSyntaxReceiver)context.SyntaxReceiver;

        if (syntaxReceiver == null || !syntaxReceiver.Methods.Any())
            return;

        

        foreach (var method in syntaxReceiver.Methods)
        {
            try
            {
                var methodSyntax = (MethodDeclarationSyntax)method;

                if (methodSyntax.Identifier.ToString() == "Create")
                {

                    // Example conversion logic
                    var newMethod = methodSyntax
                        .WithIdentifier(SyntaxFactory.Identifier("Create"))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.ParseExpression(
                                        "var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString)"
                                    )
                                )
                            )
                        ));

                    var newMethodCode = $@"
        using System;
        
        namespace GeneratedCodeNameSpace
        {{
            public partial class GeneratedCodeClass
            {{
                {newMethod.ToFullString()}
            }}
        }}";

                    context.AddSource($"{methodSyntax.Identifier}.g.cs", newMethodCode);
                    break;
                }
            }
            catch ( Exception ex ) { break; }
        }
    }
}

internal class SqliteSyntaxReceiver : ISyntaxReceiver
{
    public List<SyntaxNode> Methods { get; } = new List<SyntaxNode>();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Look for method declarations
        if (syntaxNode is MethodDeclarationSyntax methodDeclaration)
        {
            Methods.Add(methodDeclaration);
        }
    }
}
