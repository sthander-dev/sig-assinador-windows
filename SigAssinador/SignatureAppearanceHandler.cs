using System.Security.Cryptography.X509Certificates;
using PdfSharp.Drawing;
using PdfSharp.Pdf.Annotations;
using QRCoder;

namespace SigAssinador;

internal sealed class SignatureAppearanceHandler : IAnnotationAppearanceHandler
{
    private readonly string _signer;
    private readonly string _cnpj;
    private readonly string _serial;
    private readonly string _validationCode;
    private readonly byte[] _qrPng;
    private readonly DateTime _signedAt = DateTime.Now;

    public SignatureAppearanceHandler(X509Certificate2 certificate, SigningJob job)
    {
        _signer = certificate.GetNameInfo(X509NameType.SimpleName, false);
        _cnpj = CertificateService.FormatCnpj(CertificateService.GetCnpj(certificate));
        _serial = certificate.SerialNumber.Length > 18
            ? certificate.SerialNumber[^18..]
            : certificate.SerialNumber;
        _validationCode = job.ValidationCode;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(job.ValidationUrl, QRCodeGenerator.ECCLevel.Q);
        _qrPng = new PngByteQRCode(data).GetGraphic(8, new byte[] { 0, 0, 0 }, new byte[] { 255, 255, 255 });
    }

    public void DrawAppearance(XGraphics graphics, XRect rectangle)
    {
        var ink = XColor.FromArgb(20, 36, 40);
        var muted = XColor.FromArgb(65, 82, 87);
        var border = XColor.FromArgb(7, 86, 101);
        var height = rectangle.Height;
        var padding = Math.Max(3.5, Math.Min(height, rectangle.Width) * 0.045);
        var qrSize = Math.Min(height - (padding * 2), rectangle.Width * 0.30);
        var textX = padding + qrSize + Math.Max(7, rectangle.Width * 0.025);
        var textWidth = Math.Max(80, rectangle.Width - textX - padding);

        var lines = new[]
        {
            "Documento assinado digitalmente conforme ICP-Brasil e MP 2.200-2/2001.",
            $"Titular: {_signer}",
            $"CNPJ: {_cnpj}",
            $"Certificado nº de série: {_serial}",
            $"Data: {_signedAt:dd/MM/yyyy HH:mm:ss}",
            "Validação: sig.sthanderinfo.com.br",
            $"Código verificador: {_validationCode}"
        };

        var measuringFont = new XFont("Segoe UI", 8.2, XFontStyleEx.Bold);
        var widestLine = lines.Max(lineText => graphics.MeasureString(lineText, measuringFont).Width);
        var widthScale = textWidth / Math.Max(1, widestLine);
        var heightScale = (height - (padding * 2)) / (lines.Length * 11.5);
        var scale = Math.Clamp(Math.Min(widthScale, heightScale), 0.48, 2.20);

        var regular = new XFont("Segoe UI", 8.0 * scale, XFontStyleEx.Regular);
        var strong = new XFont("Segoe UI", 8.2 * scale, XFontStyleEx.Bold);
        var codeFont = new XFont("Segoe UI", 8.4 * scale, XFontStyleEx.Bold);
        var inkBrush = new XSolidBrush(ink);
        var mutedBrush = new XSolidBrush(muted);

        graphics.DrawRectangle(XBrushes.White, 0, 0, rectangle.Width, rectangle.Height);
        graphics.DrawRectangle(new XPen(border, 0.9), 0.45, 0.45, rectangle.Width - 0.9, rectangle.Height - 0.9);

        using (var stream = new MemoryStream(_qrPng, writable: false))
        using (var qrImage = XImage.FromStream(stream))
            graphics.DrawImage(qrImage, padding, padding, qrSize, qrSize);

        var line = 11.5 * scale;
        var totalTextHeight = lines.Length * line;
        var y = Math.Max(padding, (height - totalTextHeight) / 2);
        DrawLine(graphics, lines[0], regular, inkBrush, textX, y, textWidth, line); y += line;
        DrawLine(graphics, lines[1], strong, inkBrush, textX, y, textWidth, line); y += line;
        DrawLine(graphics, lines[2], regular, inkBrush, textX, y, textWidth, line); y += line;
        DrawLine(graphics, lines[3], regular, inkBrush, textX, y, textWidth, line); y += line;
        DrawLine(graphics, lines[4], regular, inkBrush, textX, y, textWidth, line); y += line;
        DrawLine(graphics, lines[5], regular, mutedBrush, textX, y, textWidth, line); y += line;
        DrawLine(graphics, lines[6], codeFont, inkBrush, textX, y, textWidth, line);
    }

    private static void DrawLine(
        XGraphics graphics,
        string text,
        XFont font,
        XBrush brush,
        double x,
        double y,
        double width,
        double height) =>
        graphics.DrawString(text, font, brush, new XRect(x, y, width, height), XStringFormats.TopLeft);
}
