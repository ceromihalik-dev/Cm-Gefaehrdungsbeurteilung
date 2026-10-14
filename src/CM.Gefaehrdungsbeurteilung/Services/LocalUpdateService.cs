using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CM.Gefaehrdungsbeurteilung.Services;

public sealed record LocalUpdateInfo(
    string PackagePath, Version Version, string InstallerName, string Sha256,
    string Notes, string PackageSha256);

/// <summary>
/// Imports an explicitly user-selected .cmupdate file.
/// SHA-256 protects package integrity, NOT its publisher identity.
/// An unsigned package must always require explicit user confirmation.
/// </summary>
public static class LocalUpdateService
{
    private const long MaxInstallerSize = 500L * 1024 * 1024;
    private const long MaxManifestSize = 64 * 1024;
    private static readonly Regex SafeVersion = new(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);

    private static (ZipArchive Archive, ZipArchiveEntry Installer, PackageManifest Manifest) OpenPackage(Stream stream)
    {
        var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        try
        {
            var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToArray();
            if (entries.Length != 2 || archive.Entries.Count != 2)
                throw new InvalidDataException("Update-Datei muss genau manifest.json und das Installationsprogramm enthalten.");
            var meta = entries.SingleOrDefault(e => e.FullName == "manifest.json")
                ?? throw new InvalidDataException("manifest.json fehlt.");
            if (meta.Length <= 0 || meta.Length > MaxManifestSize)
                throw new InvalidDataException("Unzulässige Manifestgröße.");
            PackageManifest manifest;
            using (var input = meta.Open())
            {
                manifest = JsonSerializer.Deserialize<PackageManifest>(input)
                    ?? throw new InvalidDataException("Ungültiges Update-Manifest.");
            }
            if (manifest.SchemaVersion != 1 || string.IsNullOrWhiteSpace(manifest.Version) ||
                !SafeVersion.IsMatch(manifest.Version) ||
                !Version.TryParse(manifest.Version, out var version))
                throw new InvalidDataException("Ungültige Update-Version oder Dateiformat.");
            string expectedName = $"CM-Gefaehrdungsbeurteilung-Setup-{manifest.Version}.exe";
            if (!string.Equals(manifest.InstallerFile, expectedName, StringComparison.Ordinal))
                throw new InvalidDataException("Installerdateiname stimmt nicht mit der Paketversion überein.");
            if (manifest.Sha256 is null || !Regex.IsMatch(manifest.Sha256, @"\A[a-fA-F0-9]{64}\z"))
                throw new InvalidDataException("SHA-256-Prüfsumme fehlt oder ist ungültig.");
            var setup = entries.SingleOrDefault(e => e.FullName == expectedName)
                ?? throw new InvalidDataException("Setup-Datei fehlt oder liegt im falschen Pfad.");
            if (setup.Length <= 0 || setup.Length > MaxInstallerSize)
                throw new InvalidDataException("Setup-Datei überschreitet die zulässige Größe.");
            if (version <= UpdateService.CurrentVersion)
                throw new InvalidOperationException("Die Update-Datei enthält keine neuere Programmversion.");
            return (archive, setup, manifest);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    public static async Task<LocalUpdateInfo> InspectAsync(string packagePath)
    {
        if (!Path.GetExtension(packagePath).Equals(".cmupdate", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Bitte eine .cmupdate-Datei auswählen.");
        await using var file = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > MaxInstallerSize + 2 * 1024 * 1024)
            throw new InvalidDataException("Update-Datei ist zu groß.");
        using var package = OpenPackage(file);
        await using (var setupStream = package.Installer.Open())
        {
            var sum = Convert.ToHexString(await SHA256.HashDataAsync(setupStream));
            if (!sum.Equals(package.Manifest.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SHA-256-Prüfung fehlgeschlagen. Update abgelehnt.");
        }
        file.Position = 0;
        var packageHash = Convert.ToHexString(await SHA256.HashDataAsync(file));
        return new LocalUpdateInfo(packagePath, Version.Parse(package.Manifest.Version!),
            package.Manifest.InstallerFile!, package.Manifest.Sha256!, package.Manifest.Notes ?? "",
            packageHash);
    }

    public static async Task<string> ExtractVerifiedAsync(LocalUpdateInfo info)
    {
        // Unique output file prevents stale installers from being reused.
        string folder = Path.Combine(StorageService.DataDirectory, "UpdateCache");
        Directory.CreateDirectory(folder);
        string destination = Path.Combine(folder, Guid.NewGuid().ToString("N") + "-" + info.InstallerName);
        try
        {
            await using var file = new FileStream(info.PackagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var package = OpenPackage(file);
            if (package.Manifest.Version != $"{info.Version.Major}.{info.Version.Minor}.{info.Version.Build}" ||
                !string.Equals(package.Manifest.Sha256, info.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Die Update-Datei wurde seit der Prüfung verändert.");
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var input = package.Installer.Open())
            {
                var buffer = new byte[128 * 1024];
                long count = 0;
                int n;
                while ((n = await input.ReadAsync(buffer)) > 0)
                {
                    count += n;
                    if (count > MaxInstallerSize)
                        throw new InvalidDataException("Die entpackte Setup-Datei ist zu groß.");
                    await output.WriteAsync(buffer.AsMemory(0, n));
                }
            }
            await using (var read = File.OpenRead(destination))
            {
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(read));
                if (!actual.Equals(info.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Die entpackte Setup-Datei besteht die Integritätsprüfung nicht.");
            }
            return destination;
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            throw;
        }
    }

    private sealed class PackageManifest
    {
        public int SchemaVersion { get; set; }
        public string? Version { get; set; }
        public string? InstallerFile { get; set; }
        public string? Sha256 { get; set; }
        public string? Notes { get; set; }
    }
}
