using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AssinadorSIG/1.0.4");
        client.DefaultRequestHeaders.ExpectContinue = false;

        using var form = await CreateUploadFormAsync(signedPath, job, certificate);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/signed")
        {
            Content = form,
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"O PDF foi assinado, mas o registro público não foi concluído. " +
                $"Resposta do SIG (HTTP {(int)response.StatusCode}): {message}");
        }
    }

    internal static async Task<MultipartFormDataContent> CreateUploadFormAsync(
        string signedPath,
        SigningJob job,
        X509Certificate2 certificate)
    {
        // Use a deterministic, broadly compatible multipart shape for Cloudflare Workers:
        // text fields first, an unquoted boundary and a buffered PDF with an ASCII filename.
        // This avoids parsers dropping the whole form when a local filename or streamed part
        // is encoded differently by a Windows HTTP stack.
        var boundary = $"----SIG{Guid.NewGuid():N}";
        var form = new MultipartFormDataContent(boundary);
        var boundaryParameter = form.Headers.ContentType?.Parameters
            .FirstOrDefault(parameter =>
                parameter.Name?.Equals("boundary", StringComparison.OrdinalIgnoreCase) == true);
        if (boundaryParameter is not null)
            boundaryParameter.Value = boundary;

        AddTextPart(form, "paymentId", job.PaymentId);
        AddTextPart(form, "validationCode", job.ValidationCode);
        AddTextPart(form, "signerName", certificate.GetNameInfo(X509NameType.SimpleName, false));
        AddTextPart(form, "signerCnpj", CertificateService.GetCnpj(certificate));
        AddTextPart(form, "certificateSerial", certificate.SerialNumber);
        AddTextPart(form, "certificateThumbprint", certificate.Thumbprint);

        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(signedPath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileName = "\"documento-assinado.pdf\""
        };
        form.Add(fileContent);
        return form;
    }

    private static void AddTextPart(MultipartFormDataContent form, string name, string value)
    {
        var content = new StringContent(value, Encoding.UTF8, "text/plain");
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = $"\"{name}\""
        };
        form.Add(content);
    }
}

