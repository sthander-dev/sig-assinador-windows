using System.Security.Cryptography.X509Certificates;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Signatures;

namespace SigAssinador;

internal static class PdfSigningService
{
    public static async Task SignAsync(
        string inputPath,
        string outputPath,
        X509Certificate2 certificate,
        SignaturePlacement placement,
        SigningJob job)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Documento PDF não encontrado.", inputPath);
        if (!certificate.HasPrivateKey)
            throw new InvalidOperationException("O certificado selecionado não possui uma chave privada disponível.");

        using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
        if (placement.PageNumber < 1 || placement.PageNumber > document.PageCount)
            throw new ArgumentOutOfRangeException(nameof(placement), "A página escolhida não existe no documento.");

        var pageIndex = placement.PageNumber - 1;
        var options = new DigitalSignatureOptions
        {
            ContactInfo = "Sthander Info — SIG",
            Location = "Brasil",
            Reason = "Assinatura digital ICP-Brasil",
            PageIndex = pageIndex,
            Rectangle = placement.GetRectangle(document.Pages[pageIndex]),
            AppearanceHandler = new SignatureAppearanceHandler(certificate, job)
        };

        _ = DigitalSignatureHandler.ForDocument(
            document,
            new PdfSharpDefaultSigner(certificate, PdfMessageDigestType.SHA256),
            options);

        await document.SaveAsync(outputPath);
    }
}
