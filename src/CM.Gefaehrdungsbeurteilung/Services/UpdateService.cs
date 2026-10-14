using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CM.Gefaehrdungsbeurteilung.Services;

public sealed record ReleaseInfo(Version Version, string Notes, string HtmlUrl, string InstallerUrl, string ChecksumsUrl)
{
    public bool HasInstaller => InstallerUrl.Length > 0 && ChecksumsUrl.Length > 0;
}

public static class UpdateService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(45) };
    static UpdateService() => Http.DefaultRequestHeaders.UserAgent.ParseAdd("CM-Gefaehrdungsbeurteilung/0.1");
    public static Version CurrentVersion => typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 1, 0);
    // Fingerabdruck kommt aus der signierten Binary, nicht aus benutzereditierbaren Einstellungen.
    // Ein Entwicklungsbuild ohne diesen Wert kann Updates finden, aber nicht automatisch installieren.
    public static string TrustedSignerThumbprint => typeof(UpdateService).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "CM.TrustedUpdateSignerThumbprint")?.Value ?? "";
    public static bool CanAutoInstall => Regex.IsMatch(TrustedSignerThumbprint, @"\A[a-fA-F0-9]{40}\z");

    public static async Task<ReleaseInfo> GetLatestAsync(string owner, string repo)
    {
        if (!Regex.IsMatch(owner, @"^[A-Za-z0-9-]{1,39}$") ||
            !Regex.IsMatch(repo, @"^[A-Za-z0-9_.-]{1,100}$"))
            throw new ArgumentException("Bitte einen gültigen GitHub-Eigentümer und Repository-Namen eintragen.");
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Kein öffentliches GitHub-Release gefunden. Repository/Release-Einstellungen prüfen.");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var raw = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(raw.TrimStart('v', 'V').Split('-')[0], out var version))
            throw new InvalidOperationException("Das Release hat keine auswertbare Versionsnummer.");
        var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
        var html = root.GetProperty("html_url").GetString() ?? "";
        var installer = ""; var checksums = "";
        if (root.TryGetProperty("assets", out var assets))
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                var link = asset.GetProperty("browser_download_url").GetString() ?? "";
                if (name.Equals($"CM-Gefaehrdungsbeurteilung-Setup-{version.Major}.{version.Minor}.{version.Build}.exe", StringComparison.OrdinalIgnoreCase)) installer = link;
                if (name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase)) checksums = link;
            }
        return new ReleaseInfo(version, notes, html, installer, checksums);
    }

    private static Uri GitHubReleaseAssetUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Unzulässige Downloadadresse. Nur HTTPS-GitHub-Releaseassets erlaubt.");
        return uri;
    }

    public static async Task<string> DownloadVerifiedInstallerAsync(ReleaseInfo release, string pinnedThumbprint)
    {
        if (!release.HasInstaller) throw new InvalidOperationException("Setup oder Prüfsummendatei fehlt im Release.");
        if (!CanAutoInstall || !string.Equals(pinnedThumbprint, TrustedSignerThumbprint, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("In diesem Programm-Build ist kein freigegebener Update-Herausgeber fest hinterlegt. Automatische Installation deaktiviert.");
        if (release.Version <= CurrentVersion)
            throw new InvalidOperationException("Das Release ist nicht neuer als die installierte Programmversion.");
        var installerUri = GitHubReleaseAssetUrl(release.InstallerUrl);
        var sumsUri = GitHubReleaseAssetUrl(release.ChecksumsUrl);
        string installerName = Uri.UnescapeDataString(installerUri.Segments.Last());
        string expectedName = $"CM-Gefaehrdungsbeurteilung-Setup-{release.Version.Major}.{release.Version.Minor}.{release.Version.Build}.exe";
        if (!installerName.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Installername stimmt nicht mit der Releaseversion überein.");
        if (Uri.UnescapeDataString(sumsUri.Segments.Last()) != "SHA256SUMS.txt")
            throw new InvalidOperationException("Unzulässiger Name der Prüfsummendatei.");
        // Beide Dateien muessen aus dem vom API-Release angegebenen Versionstag stammen.
        string expectedReleasePath = $"/releases/download/v{release.Version.Major}.{release.Version.Minor}.{release.Version.Build}/";
        if (!installerUri.AbsolutePath.Contains(expectedReleasePath, StringComparison.OrdinalIgnoreCase) ||
            !sumsUri.AbsolutePath.Contains(expectedReleasePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Release-Assets passen nicht zum freigegebenen Versionstag.");
        // Die beiden Dateien muessen zum selben GitHub-Release gehoeren.
        if (!installerUri.AbsolutePath.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase) ||
            !sumsUri.AbsolutePath.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase) ||
            !installerUri.AbsolutePath[..installerUri.AbsolutePath.LastIndexOf('/')].Equals(
                sumsUri.AbsolutePath[..sumsUri.AbsolutePath.LastIndexOf('/')], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Setup und Prüfsumme stammen nicht aus demselben GitHub-Release.");
        string sumFile = await Http.GetStringAsync(sumsUri);
        if (sumFile.Length > 128 * 1024)
            throw new InvalidOperationException("Prüfsummendatei ist unerwartet groß.");
        string? checksum = null;
        foreach (var line in sumFile.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Regex.Match(line.Trim(), @"^([a-fA-F0-9]{64})\s+\*?(.+)$");
            if (m.Success && m.Groups[2].Value.Trim() == installerName)
            {
                if (checksum is not null) throw new InvalidOperationException("Mehrdeutige SHA-256-Prüfsumme.");
                checksum = m.Groups[1].Value;
            }
        }
        if (checksum is null) throw new InvalidOperationException("Passende SHA-256-Prüfsumme fehlt.");
        var dir = Path.Combine(StorageService.DataDirectory, "UpdateCache");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, installerName);
        var tmp = path + ".partial-" + Guid.NewGuid().ToString("N");
        try
        {
            // Begrenzter Download, kein Überschreiben einer eventuell anderen Update-Session.
            using (var response = await Http.GetAsync(installerUri, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                const long maxSize = 500L * 1024 * 1024;
                if (response.Content.Headers.ContentLength > maxSize)
                    throw new InvalidOperationException("Installer überschreitet 500 MB.");
                await using var stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await using var source = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[1024 * 128];
                long bytes = 0;
                int read;
                while ((read = await source.ReadAsync(buffer)) > 0)
                {
                    bytes += read;
                    if (bytes > maxSize) throw new InvalidOperationException("Installer überschreitet 500 MB.");
                    await stream.WriteAsync(buffer.AsMemory(0, read));
                }
            }
            await using (var read = File.OpenRead(tmp))
            {
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(read));
                if (!actual.Equals(checksum, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("SHA-256-Prüfung fehlgeschlagen. Update verworfen.");
            }
            // SHA-256 allein beweist keine Herausgeberidentität: zusätzlich gültige Authenticode-Signatur und fest hinterlegter Fingerabdruck.
            await VerifyAuthenticodeAsync(tmp, pinnedThumbprint);
            File.Move(tmp, path, true);
            return path;
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    private static async Task VerifyAuthenticodeAsync(string file, string thumbprint)
    {
        // Eingabepfad wird als Base64-kodierte PowerShell-Zeichenfolge übergeben, nicht als direkt interpolierter Befehl.
        var safe = file.Replace("'", "''");
        var script = $"$s=Get-AuthenticodeSignature -LiteralPath '{safe}'; [PSCustomObject]@{{Status=$s.Status.ToString();Thumbprint=if($s.SignerCertificate){{$s.SignerCertificate.Thumbprint}}else{{''}}}} | ConvertTo-Json -Compress";
        var info = new ProcessStartInfo("powershell.exe")
        {
            Arguments = "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Windows-Signaturprüfung konnte nicht gestartet werden.");
        string output = await process.StandardOutput.ReadToEndAsync();
        string errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("Signaturprüfung fehlgeschlagen: " + errors);
        using var obj = JsonDocument.Parse(output);
        var status = obj.RootElement.GetProperty("Status").GetString();
        var actual = obj.RootElement.GetProperty("Thumbprint").GetString() ?? "";
        if (status != "Valid" || !NormalizeHash(actual).Equals(NormalizeHash(thumbprint), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ungültige Signatur oder unbekannter Herausgeber. Update wird nicht ausgeführt.");
    }

    private static string NormalizeHash(string str) => string.Concat(str.Where(Uri.IsHexDigit)).ToUpperInvariant();
}
