using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace SigAssinador;

internal sealed record SigningJob(
    int Version,
    string PaymentId,
    string SignerToken,
    string? OriginalDocumentHash,
    string ValidationCode,
    string ValidationUrl,
    string ApiBaseUrl)
{
    public bool IsLocalTest =>
        SignerToken.StartsWith("MODELO-LOCAL-", StringComparison.OrdinalIgnoreCase);

    public static SigningJob Load(string path)
    {
        var job = JsonSerializer.Deserialize<SigningJob>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("A autorização SIG está vazia ou inválida.");

        if (job.Version != 1 || string.IsNullOrWhiteSpace(job.PaymentId) ||
            string.IsNullOrWhiteSpace(job.SignerToken) ||
            !System.Text.RegularExpressions.Regex.IsMatch(job.ValidationCode, @"^SIG-\d{4}-[A-F0-9]{8}$") ||
            !Uri.TryCreate(job.ValidationUrl, UriKind.Absolute, out var validationUri) ||
            validationUri.Scheme != Uri.UriSchemeHttps ||
            !validationUri.Host.Equals("sig.sthanderinfo.com.br", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(job.ApiBaseUrl, UriKind.Absolute, out var apiUri) ||
            apiUri.Scheme != Uri.UriSchemeHttps ||
            !apiUri.Host.Equals("sig.sthanderinfo.com.br", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("A autorização não pertence ao serviço oficial do SIG.");

        return job;
    }
}

internal static class SigningJobService
{
    public static async Task EnsureDocumentMatchesAsync(string documentPath, SigningJob job)
    {
        if (string.IsNullOrWhiteSpace(job.OriginalDocumentHash)) return;
        await using var stream = File.OpenRead(documentPath);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant();
        if (!hash.Equals(job.OriginalDocumentHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Este PDF não é o mesmo documento vinculado ao pagamento. Selecione o arquivo original usado no site.");
    }

    public static async Task RegisterSignedDocumentAsync(
        string signedPath,
        SigningJob job,
        X509Certificate2 certificate)
    {
        using var client = new HttpClient { BaseAddress = new Uri(job.ApiBaseUrl), Timeout = TimeSpan.FromMinutes(2) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", job.SignerToken);

        await using var fileStream = File.OpenRead(signedPath);
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", Path.GetFileName(signedPath));
        form.Add(new StringContent(job.PaymentId), "paymentId");
        form.Add(new StringContent(job.ValidationCode), "validationCode");
        form.Add(new StringContent(certificate.GetNameInfo(X509NameType.SimpleName, false)), "signerName");
        form.Add(new StringContent(CertificateService.GetCnpj(certificate)), "signerCnpj");
        form.Add(new StringContent(certificate.SerialNumber), "certificateSerial");
        form.Add(new StringContent(certificate.Thumbprint), "certificateThumbprint");

        using var response = await client.PostAsync("/api/documents/signed", form);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"O PDF foi assinado, mas o registro público não foi concluído. Resposta do SIG: {message}");
        }
    }
}
