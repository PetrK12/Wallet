namespace WalletApp.Metrics.M4;

public record TestFileResult(
    string Path,
    string Architecture,
    int TestCount,
    List<string> InfraUsings,
    bool IsIsolated);