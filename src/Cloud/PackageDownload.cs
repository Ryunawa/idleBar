using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace IdleBar.Cloud;

public static class PackageDownload
{
    private const string UserAgent = "IdleBar";
    private const int BufferSize = 81920;

    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(30);

    public static async Task SaveAsync(ReleaseAsset asset, string path, IProgress<double> progress)
    {
        using HttpClient http = new();
        using CancellationTokenSource stall = new(StallTimeout);
        using HttpRequestMessage request = new(HttpMethod.Get, asset.BrowserDownloadUrl);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stall.Token);
        response.EnsureSuccessStatusCode();

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using Stream source = await response.Content.ReadAsStreamAsync(stall.Token);
        await using FileStream target = new(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[BufferSize];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, stall.Token)) > 0)
        {
            stall.CancelAfter(StallTimeout);
            hash.AppendData(buffer, 0, read);
            await target.WriteAsync(buffer.AsMemory(0, read), stall.Token);
            received += read;
            progress.Report((double)received / asset.Size);
        }

        string digest = Convert.ToHexString(hash.GetHashAndReset());
        if (received != asset.Size || !string.Equals(digest, asset.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdateFailedException("Le fichier téléchargé est incomplet ou abîmé.");
        }
    }
}
