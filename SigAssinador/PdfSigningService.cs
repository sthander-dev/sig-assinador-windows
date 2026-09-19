using System.Security.Cryptography.X509Certificates;
using PdfSharp.Drawing;
using PdfSharp.Pdf.Annotations;
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
        var page = document.Pages[pageIndex];
        var rectangle = placement.GetRectangle(page);
        var appearance = new SignatureAppearanceHandler(certificate, job);

        // Put the visible seal in page content BEFORE computing the signature.
        // PDF viewers may omit widget annotations in print/share rendering.
        // XGraphics uses a top-left origin; the signature rectangle uses PDF coordinates.
        using (var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
        {
            graphics.TranslateTransform(rectangle.X, page.Height.Point - rectangle.Y - rectangle.Height);
            appearance.DrawAppearance(graphics, new XRect(0, 0, rectangle.Width, rectangle.Height));
        }

        var options = new DigitalSignatureOptions
        {
            ContactInfo = "Sthander Info — SIG",
            Location = "Brasil",
            Reason = "Assinatura digital ICP-Brasil",
            PageIndex = pageIndex,
            Rectangle = rectangle,
            AppearanceHandler = new ContentSealAppearance()
        };

        _ = DigitalSignatureHandler.ForDocument(
            document,
            new PdfSharpDefaultSigner(certificate, PdfMessageDigestType.SHA256),
            options);

        await document.SaveAsync(outputPath);
    }

    // Keep the cryptographic signature field and its clickable rectangle without
    // drawing a second copy over the seal already embedded in the page content.
    private sealed class ContentSealAppearance : IAnnotationAppearanceHandler
    {
        public void DrawAppearance(XGraphics graphics, XRect rectangle) { }
    }
}
