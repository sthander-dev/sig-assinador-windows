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
        var padding = Math.Max(5, height * 0.065);
        var qrSize = Math.Min(height - (padding * 2), rectangle.Width * 0.27);
        var textX = padding + qrSize + Math.Max(7, rectangle.Width * 0.025);
        var textWidth = Math.Max(80, rectangle.Width - textX - padding);
        var scale = Math.Clamp(height / 94d, 0.76, 1.35);

        var regular = new XFont("Segoe UI", 6.7 * scale, XFontStyleEx.Regular);
        var strong = new XFont("Segoe UI", 6.9 * scale, XFontStyleEx.Bold);
        var codeFont = new XFont("Segoe UI", 7.1 * scale, XFontStyleEx.Bold);
        var inkBrush = new XSolidBrush(ink);
        var mutedBrush = new XSolidBrush(muted);

        graphics.DrawRectangle(XBrushes.White, 0, 0, rectangle.Width, rectangle.Height);
        graphics.DrawRectangle(new XPen(border, 0.9), 0.45, 0.45, rectangle.Width - 0.9, rectangle.Height - 0.9);

        using (var stream = new MemoryStream(_qrPng, writable: false))
        using (var qrImage = XImage.FromStream(stream))
            graphics.DrawImage(qrImage, padding, padding, qrSize, qrSize);

        var line = 10.8 * scale;
        var y = padding - 1;
        DrawLine(graphics, "Documento assinado digitalmente de acordo com a", regular, inkBrush, textX, y, textWidth, line);
        y += line;
        DrawLine(graphics, "ICP-Brasil e MP 2.200-2/2001, pelo SIG, por:", regular, inkBrush, textX, y, textWidth, line);
        y += line + 1;
        DrawLine(graphics, _signer, strong, inkBrush, textX, y, textWidth, line);
        y += line;
        DrawLine(graphics, $"CNPJ: {_cnpj}", regular, inkBrush, textX, y, textWidth, line);
        y += line;
        DrawLine(graphics, $"Certificado nº de série: {_serial}", regular, inkBrush, textX, y, textWidth, line);
        y += line;
        DrawLine(graphics, $"Data: {_signedAt:dd/MM/yyyy HH:mm:ss}", regular, inkBrush, textX, y, textWidth, line);
        y += line;
        DrawLine(graphics, "Validação: sig.sthanderinfo.com.br", regular, mutedBrush, textX, y, textWidth, line);
        y += line;
        DrawLine(graphics, $"Código verificador: {_validationCode}", codeFont, inkBrush, textX, y, textWidth, line);
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
