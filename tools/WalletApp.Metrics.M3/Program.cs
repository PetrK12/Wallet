using System.Diagnostics;
using System.Text.RegularExpressions;

string baseBranch    = args.Length > 0 ? args[0] : "master";
string targetBranch  = args.Length > 1 ? args[1] : "experiment/h2-multicurrency-change";
string repoRoot      = args.Length > 2 ? args[2] : "../..";

// 1) Cesty, ktere mereni vylucuje (popsano v kapitole 6.3 prace)
//    - artefakty version control / IDE / mereni / nastroju, nikoli aplikacniho kodu
string[] excludedPathPrefixes =
{
    ".gitignore", ".gitattributes", ".idea",
    "WalletApp.sln", "metrics/", "tools/"
};

// 2) Spustime git diff --name-status pro seznam zmenenych souboru
string nameStatus = RunGit(repoRoot, $"diff --name-status {baseBranch} {targetBranch}");

var changedFiles = nameStatus
    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    .Select(line => line.Split('\t', 2))
    .Where(parts => parts.Length == 2)
    .Select(parts => new ChangedFile(Status: parts[0].Trim(), Path: parts[1].Trim()))
    .Where(f => !excludedPathPrefixes.Any(prefix => f.Path.StartsWith(prefix)))
    .ToList();

// 3) Pro kazdy soubor stahneme jeho diff a klasifikujeme zmenu
var businessLogicPatterns = new[]
{
    new Regex(@"^\+\s*(if|else|throw|return)\b", RegexOptions.Compiled),
    new Regex(@"^\+\s*(public|private|protected|internal|static).*\(.*\)", RegexOptions.Compiled),
    new Regex(@"^\+.*[!=<>]=|^\+.*[^=]==", RegexOptions.Compiled)
};

var classifiedFiles = new List<ClassifiedFile>();

foreach (var file in changedFiles)
{
    string diffOutput = RunGit(repoRoot, $"diff {baseBranch} {targetBranch} -- \"{file.Path}\"");

    // Pocitame jen pridane radky ('+' na zacatku, ale ne '+++' hlavicku)
    var addedLines = diffOutput
        .Split('\n')
        .Where(l => l.StartsWith("+") && !l.StartsWith("+++"))
        .ToList();

    bool hasBusinessLogic = addedLines.Any(line =>
        businessLogicPatterns.Any(p => p.IsMatch(line)));

    string classification = hasBusinessLogic ? "business_logic" : "mechanical";

    // Kategorie podle slozky (jen pro reporting - neovlivnuje klasifikaci)
    string category =
        file.Path.Contains("src/Layered/") ? "layered_production" :
        file.Path.Contains("src/Hexagonal/") ? "hexagonal_production" :
        file.Path.Contains("tests/") && file.Path.Contains("Layered") ? "layered_tests" :
        file.Path.Contains("tests/") && file.Path.Contains("Hexagonal") ? "hexagonal_tests" :
        "other";

    classifiedFiles.Add(new ClassifiedFile(
        file.Path,
        file.Status,
        category,
        classification,
        addedLines.Count));
}

// 4) Vypis - rozdeleny po kategoriich
Console.WriteLine("=== M3 - Rozsah zmeny kodu ===");
Console.WriteLine($"Base branch:   {baseBranch}");
Console.WriteLine($"Target branch: {targetBranch}");
Console.WriteLine();

PrintCategory("Layered - produkcni kod", "layered_production");
PrintCategory("Hexagonal - produkcni kod", "hexagonal_production");
PrintCategory("Layered - testy", "layered_tests");
PrintCategory("Hexagonal - testy", "hexagonal_tests");

Console.WriteLine("=== SHRNUTI ===");
PrintSummary("Layered produkce", "layered_production");
PrintSummary("Hexagonal produkce", "hexagonal_production");
PrintSummary("Layered testy", "layered_tests");
PrintSummary("Hexagonal testy", "hexagonal_tests");

return;

// ===== Helpery =====

void PrintCategory(string title, string category)
{
    var items = classifiedFiles.Where(f => f.Category == category).ToList();
    if (items.Count == 0) return;

    Console.WriteLine($"--- {title} ---");
    foreach (var f in items)
    {
        string marker = f.Classification == "business_logic" ? "[B]" : "[M]";
        Console.WriteLine($"  {marker} {f.Status}  {f.Path}  (+{f.AddedLines} radku)");
    }
    Console.WriteLine();
}

void PrintSummary(string title, string category)
{
    var items = classifiedFiles.Where(f => f.Category == category).ToList();
    int total = items.Count;
    int business = items.Count(f => f.Classification == "business_logic");
    int mechanical = items.Count(f => f.Classification == "mechanical");
    Console.WriteLine($"{title}: {total} souboru ({business} business logic, {mechanical} mechanical)");
}

string RunGit(string workingDir, string arguments)
{
    var psi = new ProcessStartInfo("git", arguments)
    {
        WorkingDirectory = Path.GetFullPath(workingDir),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    using var proc = Process.Start(psi)!;
    string output = proc.StandardOutput.ReadToEnd();
    proc.WaitForExit();
    return output;
}

record ChangedFile(string Status, string Path);
record ClassifiedFile(string Path, string Status, string Category, string Classification, int AddedLines);