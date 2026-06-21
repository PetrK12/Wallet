using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Cesta k projektu, ktery analyzujeme — uprav podle sve struktury
var targetPath = args.Length > 0 ? args[0] : "../../src/Layered/WalletApp.Layered.BusinessLogic";
var infraNamespacePrefix = args.Length > 1 ? args[1] : "WalletApp.Layered.DataAccess";

var csFiles = Directory.GetFiles(targetPath, "*.cs", SearchOption.AllDirectories);

var dependencies = new List<(string File, string Class, string ReferencedType, string Kind)>();

foreach (var file in csFiles)
{
    var code = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    // Najdeme vsechny deklarace trid v souboru
    var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

    foreach (var classDecl in classDeclarations)
    {
        var className = classDecl.Identifier.Text;

        // 1) Parametry konstruktoru
        var constructors = classDecl.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        foreach (var ctor in constructors)
        {
            foreach (var param in ctor.ParameterList.Parameters)
            {
                string? typeName = param.Type?.ToString();
                if (typeName != null && IsInfraType(typeName, infraNamespacePrefix, code))
                {
                    dependencies.Add((Path.GetFileName(file), className, typeName, "constructor_parameter"));
                }
            }
        }

        // 2) Lokalni promenne (var x = new Wallet(), Wallet x = ...)
        var localDeclarations = classDecl.DescendantNodes().OfType<LocalDeclarationStatementSyntax>();
        foreach (var localDecl in localDeclarations)
        {
            var typeSyntax = localDecl.Declaration.Type;
            var typeName = typeSyntax.ToString();

            // Pokud je "var", zkusime zjistit typ z object creation expression
            if (typeName == "var")
            {
                var objectCreations = localDecl.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                dependencies.AddRange(from oc in objectCreations select oc.Type.ToString() into createdType where IsInfraType(createdType, infraNamespacePrefix, code) select (Path.GetFileName(file), className, createdType, "local_variable_new"));
            }
            else if (IsInfraType(typeName, infraNamespacePrefix, code))
            {
                dependencies.Add((Path.GetFileName(file), className, typeName, "local_variable_declared"));
            }
        }

        // 3) Object creation kdekoli v tele tridy (new Wallet { ... }, new Transaction(...))
        var allObjectCreations = classDecl.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var oc in allObjectCreations)
        {
            var createdType = oc.Type.ToString();
            if (IsInfraType(createdType, infraNamespacePrefix, code))
            {
                // Vyhneme se duplicite s local_variable_new
                var alreadyCounted = dependencies.Any(d => 
                    d.Class == className && d.ReferencedType == createdType && d.Kind == "local_variable_new");
                if (!alreadyCounted)
                {
                    dependencies.Add((Path.GetFileName(file), className, createdType, "object_creation"));
                }
            }
        }

        // 4) Pristup k vlastnostem/metodam pres pole typu infra (napr. _db.Database.BeginTransactionAsync)
        var fieldDeclarations = classDecl.DescendantNodes().OfType<FieldDeclarationSyntax>();
        dependencies.AddRange(from field in fieldDeclarations select field.Declaration.Type.ToString() into fieldType where IsInfraType(fieldType, infraNamespacePrefix, code) select (Path.GetFileName(file), className, fieldType, "field_declaration"));
    }
}
// Deduplikace - pole a jeho konstruktorovy parametr = jedna zavislost
var deduplicated = dependencies
    .GroupBy(d => (d.Class, d.ReferencedType))
    .Select(g => new
    {
        Class = g.Key.Class,
        ReferencedType = g.Key.ReferencedType,
        Kinds = string.Join(", ", g.Select(x => x.Kind).Distinct()),
        File = g.First().File
    })
    .ToList();

// Vypis vysledku
Console.WriteLine("=== M1 - Přímé závislosti na infrastruktuře ===");
Console.WriteLine($"Analyzovaná cesta: {targetPath}");
Console.WriteLine($"Infrastrukturní namespace prefix: {infraNamespacePrefix}");
Console.WriteLine();

foreach (var group in deduplicated.GroupBy(d => d.Class))
{
    Console.WriteLine($"Třída: {group.Key}");
    foreach (var dep in group)
    {
        Console.WriteLine($"  - {dep.ReferencedType} (zjištěno jako: {dep.Kinds}) v souboru {dep.File}");
    }
    Console.WriteLine();
}

Console.WriteLine($"CELKOVÝ POČET UNIKÁTNÍCH ZÁVISLOSTÍ: {deduplicated.Count}");
Console.WriteLine($"Počet tříd s alespoň jednou závislostí: {deduplicated.Select(d => d.Class).Distinct().Count()}");

// Pomocna funkce - rozhoduje, jestli typ patri do infrastrukturniho namespace
bool IsInfraType(string typeName, string infraPrefix, string sourceCode)
{
    // Ocistime typ od generik a nullable znacek
    string cleanType = typeName.TrimEnd('?').Split('<')[0].Trim();
    
    // Znamé infrastrukturní typy podle jména (pokud nejsou fully qualified)
    string[] knownInfraTypeNames = { "WalletRepository", "TransactionRepository", "WalletDbContext", "Wallet", "Transaction" };
    
    if (!knownInfraTypeNames.Contains(cleanType))
        return false;

    // Over, ze v souboru je using na infra namespace (jinak by typ mohl byt z domeny)
    return sourceCode.Contains($"using {infraPrefix}");
}