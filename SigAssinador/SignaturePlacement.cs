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
        var rotation = ((page.Rotate % 360) + 360) % 360;

        // Ratios come from the page as displayed by PDFium (top-left origin).
        // Annotation rectangles use the PDF's unrotated user space (bottom-left origin).
        return rotation switch
        {
            90 => new XRect(
                YRatio * pageWidth,
                XRatio * pageHeight,
                HeightRatio * pageWidth,
                WidthRatio * pageHeight),
            180 => new XRect(
                (1 - XRatio - WidthRatio) * pageWidth,
                YRatio * pageHeight,
                WidthRatio * pageWidth,
                HeightRatio * pageHeight),
            270 => new XRect(
                (1 - YRatio - HeightRatio) * pageWidth,
                (1 - XRatio - WidthRatio) * pageHeight,
                HeightRatio * pageWidth,
                WidthRatio * pageHeight),
            _ => new XRect(
                XRatio * pageWidth,
                (1 - YRatio - HeightRatio) * pageHeight,
                WidthRatio * pageWidth,
                HeightRatio * pageHeight)
        };
    }
}
