using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace SigAssinador;

internal sealed record SignaturePlacement(
    int PageNumber,
    double XRatio,
    double YRatio,
    double WidthRatio,
    double HeightRatio)
{
    public XRect GetRectangle(PdfPage page)
    {
        var pageWidth = page.Width.Point;
        var pageHeight = page.Height.Point;

        var x = XRatio * pageWidth;
        var width = WidthRatio * pageWidth;
        var height = HeightRatio * pageHeight;
        var y = pageHeight - ((YRatio + HeightRatio) * pageHeight);

        return new XRect(x, y, width, height);
    }
}
