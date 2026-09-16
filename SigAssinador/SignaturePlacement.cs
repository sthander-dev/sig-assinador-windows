using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace SigAssinador;

internal enum SignaturePosition
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

internal sealed record SignaturePlacement(int PageNumber, SignaturePosition Position)
{
    public XRect GetRectangle(PdfPage page)
    {
        const double margin = 28;
        const double preferredWidth = 300;
        const double height = 64;

        var pageWidth = page.Width.Point;
        var pageHeight = page.Height.Point;
        var width = Math.Min(preferredWidth, Math.Max(160, pageWidth - (margin * 2)));

        var x = Position switch
        {
            SignaturePosition.TopCenter or SignaturePosition.MiddleCenter or SignaturePosition.BottomCenter
                => (pageWidth - width) / 2,
            SignaturePosition.TopRight or SignaturePosition.MiddleRight or SignaturePosition.BottomRight
                => pageWidth - width - margin,
            _ => margin
        };

        var y = Position switch
        {
            SignaturePosition.TopLeft or SignaturePosition.TopCenter or SignaturePosition.TopRight
                => pageHeight - height - margin,
            SignaturePosition.MiddleLeft or SignaturePosition.MiddleCenter or SignaturePosition.MiddleRight
                => (pageHeight - height) / 2,
            _ => margin
        };

        return new XRect(x, y, width, height);
    }
}
