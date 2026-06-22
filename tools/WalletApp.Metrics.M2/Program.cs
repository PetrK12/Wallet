using System.Xml.Linq;

// Cesta ke slozce obsahujici vsechny .csproj projekty jedne architektury
// (napr. cely "src/Layered" nebo cely "src/Hexagonal")
string solutionDir = args.Length > 0 ? args[0] : "../../src/Layered";

// 1) Najdeme vsechny .csproj soubory (mimo bin/obj), to jsou nase "uzly" v grafu zavislosti
var csprojFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
             && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
    .ToList();

if (csprojFiles.Count == 0)
{
    Console.WriteLine($"Nenalezeny zadne .csproj v {solutionDir}");
    return;
}

// Nazev projektu odvozeny z nazvu souboru (bez pripony), napr. "WalletApp.Layered.BusinessLogic"
string ProjectName(string csprojPath) => Path.GetFileNameWithoutExtension(csprojPath);

// 2) Pro kazdy projekt najdeme jeho <ProjectReference> zaznamy = Ce (efferent coupling)
//    a soucasne stavime obracenou mapu, abychom mohli spocitat Ca (afferent coupling)
var efferent = new Dictionary<string, List<string>>();  // projekt -> seznam projektu, na ktere odkazuje
var afferent = new Dictionary<string, List<string>>();   // projekt -> seznam projektu, ktere na nej odkazuji

foreach (var csproj in csprojFiles)
{
    string projectName = ProjectName(csproj);
    efferent[projectName] = new List<string>();
    if (!afferent.ContainsKey(projectName))
        afferent[projectName] = new List<string>();
}

foreach (var csproj in csprojFiles)
{
    string projectName = ProjectName(csproj);

    var doc = XDocument.Load(csproj);

    // <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
    var projectRefs = doc.Descendants("ProjectReference")
        .Select(el => el.Attribute("Include")?.Value)
        .Where(path => path is not null)
        .Select(path => path!.Replace('\\', '/'))           // ← normalizace Windows -> Unix separator
        .Select(path => Path.GetFileNameWithoutExtension(path))
        .ToList();

    foreach (var referencedProject in projectRefs)
    {
        efferent[projectName].Add(referencedProject);

        if (!afferent.ContainsKey(referencedProject))
            afferent[referencedProject] = new List<string>();

        afferent[referencedProject].Add(projectName);
    }
}

// 3) Vypocet a vypis Ca, Ce, I pro kazdy projekt
Console.WriteLine("=== M2 - Coupling metriky (Ca / Ce / Instability) ===");
Console.WriteLine($"Analyzovaná složka: {solutionDir}");
Console.WriteLine();

var allProjectNames = efferent.Keys.OrderBy(n => n).ToList();

foreach (var projectName in allProjectNames)
{
    int ce = efferent[projectName].Count;
    int ca = afferent.TryGetValue(projectName, out var list) ? list.Count : 0;

    double instability = (ca + ce) == 0 ? 0.0 : (double)ce / (ca + ce);

    Console.WriteLine($"Projekt: {projectName}");
    Console.WriteLine($"  Ce (efferent) = {ce}  →  odkazuje na: [{string.Join(", ", efferent[projectName])}]");
    Console.WriteLine($"  Ca (afferent) = {ca}  →  odkazují na něj: [{string.Join(", ", afferent.GetValueOrDefault(projectName, new List<string>()))}]");
    Console.WriteLine($"  I = Ce / (Ca + Ce) = {instability:F2}");
    Console.WriteLine();
}