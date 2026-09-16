using System.Security.Cryptography.X509Certificates;
using PdfSharp.Drawing;
using PdfSharp.Pdf.Annotations;

namespace SigAssinador;

internal sealed class SignatureAppearanceHandler(X509Certificate2 certificate) : IAnnotationAppearanceHandler
{
    private readonly string _signer = certificate.GetNameInfo(X509NameType.SimpleName, false);
    private readonly string _identifier = certificate.Thumbprint[^8..];
    private readonly DateTime _signedAt = DateTime.Now;

    public void DrawAppearance(XGraphics graphics, XRect rectangle)
    {
        var teal = XColor.FromArgb(7, 86, 101);
        var muted = XColor.FromArgb(75, 98, 104);
        var border = new XPen(teal, 1.2);
        var titleFont = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
        var textFont = new XFont("Segoe UI", 7.2, XFontStyleEx.Regular);
        var smallFont = new XFont("Segoe UI", 6.5, XFontStyleEx.Regular);

        graphics.DrawRectangle(XBrushes.White, 0, 0, rectangle.Width, rectangle.Height);
        graphics.DrawRectangle(border, 0.6, 0.6, rectangle.Width - 1.2, rectangle.Height - 1.2);
        graphics.DrawRectangle(new XSolidBrush(teal), 0, 0, 7, rectangle.Height);
        graphics.DrawString("ASSINADO DIGITALMENTE", titleFont, new XSolidBrush(teal),
            new XRect(16, 8, rectangle.Width - 24, 15), XStringFormats.TopLeft);
        graphics.DrawString(_signer, textFont, XBrushes.Black,
            new XRect(16, 25, rectangle.Width - 24, 13), XStringFormats.TopLeft);
        graphics.DrawString($"ICP-Brasil · SHA-256 · {_signedAt:dd/MM/yyyy HH:mm} · Cert. {_identifier}",
            smallFont, new XSolidBrush(muted),
            new XRect(16, 43, rectangle.Width - 24, 12), XStringFormats.TopLeft);
    }
}
