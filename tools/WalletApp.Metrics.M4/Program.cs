// Klasifikuje testovaci soubory podle pritomnosti using direktiv ukazujicich na framework/infrastrukturu.
// Pravidlo (viz kapitola 6.3 metodologie):
//   Test je IZOLOVANY, pokud zadny using v souboru neukazuje na:
//     - Microsoft.EntityFrameworkCore.*
//     - Microsoft.AspNetCore.*
//     - WalletApp.*.DataAccess.* (layered infrastruktura)
//     - WalletApp.*.Infrastructure.* (hexagonal infrastruktura)
//   Jinak je INTEGRACNI.
//
// Hand-written InMemory* test doubles patrici do test projektu samotneho (napr. InMemoryWalletRepository)
// NEJSOU povazovany za infrastrukturni zavislost — jsou to ciste C# tridy bez externi knihovny.

using System.Text.RegularExpressions;
using WalletApp.Metrics.M4;

string testsRoot = args.Length > 0 ? args[0] : "../../tests";

// Pole rozhoduje, zda using = infrastrukturni zavislost
var infraNamespacePatterns = new[]
{
    new Regex(@"^using\s+Microsoft\.EntityFrameworkCore", RegexOptions.Compiled),
    new Regex(@"^using\s+Microsoft\.AspNetCore", RegexOptions.Compiled),
    new Regex(@"^using\s+WalletApp\.\w+\.DataAccess", RegexOptions.Compiled),
    new Regex(@"^using\s+WalletApp\.\w+\.Infrastructure", RegexOptions.Compiled),
};

// Pocet testovych metod v souboru — pocita [Fact] a [Theory] atributy
var testAttributePattern = new Regex(@"\[\s*(Fact|Theory)\s*[\(\]]", RegexOptions.Compiled);

var testFiles = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
             && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
    .ToList();

var results = new List<TestFileResult>();

foreach (var file in testFiles)
{
    var lines = File.ReadAllLines(file);
    var content = string.Join("\n", lines);

    // Pocet [Fact] a [Theory] atributu = pocet testovych metod
    int testCount = testAttributePattern.Matches(content).Count;
    if (testCount == 0) continue;  // Soubor nema testy — preskocime (InMemory test doubles, helpery, ...)

    // Najdeme vsechny using direktivy odkazujici na framework/infrastrukturu
    var infraUsings = lines
        .Where(line => infraNamespacePatterns.Any(p => p.IsMatch(line.Trim())))
        .Select(line => line.Trim())
        .ToList();

    bool isIsolated = infraUsings.Count == 0;

    // Architektura podle cesty
    string architecture = file.Contains("Layered") ? "Layered" :
                          file.Contains("Hexagonal") ? "Hexagonal" : "Other";

    results.Add(new TestFileResult(
        Path: file,
        Architecture: architecture,
        TestCount: testCount,
        InfraUsings: infraUsings,
        IsIsolated: isIsolated));
}

// Vypis
Console.WriteLine("=== M4 - Mira izolace testu ===");
Console.WriteLine($"Analyzovana cesta: {testsRoot}");
Console.WriteLine();

foreach (var arch in new[] { "Layered", "Hexagonal" })
{
    var archResults = results.Where(r => r.Architecture == arch).ToList();
    if (archResults.Count == 0) continue;

    Console.WriteLine($"--- {arch} ---");
    foreach (var r in archResults)
    {
        string marker = r.IsIsolated ? "[I]" : "[F]";
        string fileName = Path.GetFileName(r.Path);
        Console.WriteLine($"  {marker} {fileName}  ({r.TestCount} testu)");
        foreach (var u in r.InfraUsings)
            Console.WriteLine($"      ! {u}");
    }
    Console.WriteLine();
}

Console.WriteLine("=== SHRNUTI ===");
foreach (var arch in new[] { "Layered", "Hexagonal" })
{
    var archResults = results.Where(r => r.Architecture == arch).ToList();
    if (archResults.Count == 0) continue;

    int totalTests = archResults.Sum(r => r.TestCount);
    int isolatedTests = archResults.Where(r => r.IsIsolated).Sum(r => r.TestCount);
    int integrationTests = totalTests - isolatedTests;
    double m4 = totalTests == 0 ? 0.0 : (double)isolatedTests / totalTests;

    Console.WriteLine($"{arch}:");
    Console.WriteLine($"  Celkem testu:       {totalTests}");
    Console.WriteLine($"  Izolovanych:        {isolatedTests}");
    Console.WriteLine($"  Frameworkovych:     {integrationTests}");
    Console.WriteLine($"  M4 (mira izolace):  {m4:P0}");
    Console.WriteLine();
}