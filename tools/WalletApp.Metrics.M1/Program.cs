using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string projectDir = args.Length > 0
    ? args[0]
    : "../../src/Layered/WalletApp.Layered.BusinessLogic";
string infraNamespacePrefix = args.Length > 1 ? args[1] : "WalletApp.Layered.DataAccess";

// 1) Najdeme vsechny .cs soubory analyzovaneho projektu (mimo bin/obj)
var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
             && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
    .ToList();

if (csFiles.Count == 0)
{
    Console.WriteLine($"Nenalezeny zadne .cs soubory v {projectDir}");
    return;
}

var syntaxTrees = csFiles
    .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f))
    .ToList();

// 2) Sestavime referencni sestaveni - bin slozka analyzovaneho projektu + zakladni .NET sestaveni
string binSearchDir = Path.Combine(projectDir, "bin");
var referencedDlls = Directory.Exists(binSearchDir)
    ? Directory.GetFiles(binSearchDir, "*.dll", SearchOption.AllDirectories).Distinct()
    : Enumerable.Empty<string>();

var references = new List<MetadataReference>();

foreach (var dll in referencedDlls)
{
    try { references.Add(MetadataReference.CreateFromFile(dll)); }
    catch { /* nekompatibilni nebo nenahratelne DLL - preskocime */ }
}

// Zakladni .NET sestaveni potrebne pro preklad jakehokoli C# kodu
string[] coreAssemblies =
{
    typeof(object).Assembly.Location,                              // System.Private.CoreLib
    typeof(Console).Assembly.Location,                              // System.Console
    typeof(System.Linq.Enumerable).Assembly.Location,               // System.Linq
    typeof(System.Threading.Tasks.Task).Assembly.Location,          // System.Tasks
    typeof(System.Collections.Generic.List<int>).Assembly.Location, // System.Collections
};

foreach (var asm in coreAssemblies.Distinct())
    references.Add(MetadataReference.CreateFromFile(asm));

// 3) Vytvorime Compilation - DULEZITE: musi byt validni preklad, jinak semanticky model nebude presny
var compilation = CSharpCompilation.Create(
    "AnalyzedProject",
    syntaxTrees,
    references.Distinct(),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

// Kontrola, ze preklad nema kriticke chyby (chybejici reference apod.)
var diagnostics = compilation.GetDiagnostics()
    .Where(d => d.Severity == DiagnosticSeverity.Error)
    .ToList();

if (diagnostics.Any())
{
    Console.WriteLine($"[upozorneni] Compilation obsahuje {diagnostics.Count} chyb - semanticka analyza muze byt nepresna:");
    foreach (var d in diagnostics.Take(5))
        Console.WriteLine($"  {d.Id}: {d.GetMessage()}");
    Console.WriteLine();
}

// 4) Hlavni analyza - pro kazdy strom najdeme tridy a jejich zavislosti
var dependencies = new List<(string File, string Class, string ReferencedType, string Kind)>();

foreach (var syntaxTree in compilation.SyntaxTrees)
{
    var semanticModel = compilation.GetSemanticModel(syntaxTree);
    var root = syntaxTree.GetRoot();
    string fileName = Path.GetFileName(syntaxTree.FilePath);

    foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        string className = classDecl.Identifier.Text;

        foreach (var field in classDecl.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var typeSymbol = semanticModel.GetTypeInfo(field.Declaration.Type).Type;
            TryRecord(typeSymbol, className, fileName, "field_declaration");
        }

        foreach (var ctor in classDecl.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var param in ctor.ParameterList.Parameters)
            {
                if (param.Type is null) continue;
                var typeSymbol = semanticModel.GetTypeInfo(param.Type).Type;
                TryRecord(typeSymbol, className, fileName, "constructor_parameter");
            }
        }

        foreach (var expr in classDecl.DescendantNodes().OfType<ExpressionSyntax>())
        {
            if (expr is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
            {
                var typeSymbol = semanticModel.GetTypeInfo(expr).Type;
                TryRecord(typeSymbol, className, fileName, "object_creation");
            }
        }

        foreach (var localDecl in classDecl.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            foreach (var variable in localDecl.Declaration.Variables)
            {
                if (semanticModel.GetDeclaredSymbol(variable) is ILocalSymbol declaredSymbol)
                    TryRecord(declaredSymbol.Type, className, fileName, "local_variable");
            }
        }

        foreach (var method in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var returnTypeSymbol = semanticModel.GetTypeInfo(method.ReturnType).Type;
            TryRecord(UnwrapTaskType(returnTypeSymbol), className, fileName, "method_return_type");
        }
    }
}

var deduplicated = dependencies
    .GroupBy(d => (d.Class, d.ReferencedType))
    .Select(g => new
    {
        Class = g.Key.Class,
        ReferencedType = g.Key.ReferencedType,
        Kinds = string.Join(", ", g.Select(x => x.Kind).Distinct()),
        File = g.First().File
    })
    .OrderBy(d => d.Class)
    .ToList();

Console.WriteLine("=== M1 - Přímé závislosti na infrastruktuře (semantic model, AdhocWorkspace) ===");
Console.WriteLine($"Projekt: {projectDir}");
Console.WriteLine($"Infrastrukturní namespace prefix: {infraNamespacePrefix}");
Console.WriteLine();

foreach (var group in deduplicated.GroupBy(d => d.Class))
{
    Console.WriteLine($"Třída: {group.Key}");
    foreach (var dep in group)
        Console.WriteLine($"  - {dep.ReferencedType}  (zjištěno jako: {dep.Kinds})  [{dep.File}]");
    Console.WriteLine();
}

Console.WriteLine($"CELKOVÝ POČET UNIKÁTNÍCH ZÁVISLOSTÍ: {deduplicated.Count}");
Console.WriteLine($"Počet tříd s alespoň jednou závislostí: {deduplicated.Select(d => d.Class).Distinct().Count()}");

return;

void TryRecord(ITypeSymbol? typeSymbol, string className, string fileName, string kind)
{
    if (typeSymbol is null) return;
    string? ns = typeSymbol.ContainingNamespace?.ToDisplayString();
    if (ns is null || !ns.StartsWith(infraNamespacePrefix)) return;
    dependencies.Add((fileName, className, typeSymbol.Name, kind));
}

ITypeSymbol? UnwrapTaskType(ITypeSymbol? typeSymbol)
{
    if (typeSymbol is INamedTypeSymbol named && named.TypeArguments.Length == 1)
        return named.TypeArguments[0];
    return typeSymbol;
}