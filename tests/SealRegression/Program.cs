using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SigAssinador;

var output = Path.GetFullPath(args.Length > 0 ? args[0] : "regression-output");
Directory.CreateDirectory(output);
using var rsa = RSA.Create(2048);
var request = new CertificateRequest("CN=SIG TESTE SEM VALIDADE ICP:00000000000000", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
using var cert = request.CreateSelfSigned(DateTimeOffset.Now.AddMinutes(-5), DateTimeOffset.Now.AddDays(1));
var job = new SigningJob(1, "LOCAL-REGRESSION", "MODELO-LOCAL-TESTE", null,
    "SIG-2026-ABCDEF12", "https://sig.sthanderinfo.com.br/validar/SIG-2026-ABCDEF12", "https://sig.sthanderinfo.com.br");

foreach (var rotation in new[] { 0, 90, 180, 270 })
{
    var source = Path.Combine(output, $"source-{rotation}.pdf");
    var signed = Path.Combine(output, $"signed-{rotation}.pdf");
    var signedVertical = Path.Combine(output, $"signed-vertical-{rotation}.pdf");
    using (var document = new PdfDocument())
    {
        for (int n = 0; n < 2; n++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(595); page.Height = XUnit.FromPoint(842);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawString("TESTE SEM VALIDADE ICP - PAGINA " + (n + 1), new XFont("Segoe UI", 12), XBrushes.Black, 35, 45);
            page.Rotate = rotation;
        }
        document.Save(source);
    }
    var originalHash = SHA256.HashData(File.ReadAllBytes(source));
    var placement = new SignaturePlacement(2, .10, .60, .80, .17);
    using (var positionDocument = PdfSharp.Pdf.IO.PdfReader.Open(source, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
    {
        var actual = placement.GetRectangle(positionDocument.Pages[1]);
        var expected = rotation switch
        {
            90 => new XRect(.60 * 595, .10 * 842, .17 * 595, .80 * 842),
            180 => new XRect(.10 * 595, .60 * 842, .80 * 595, .17 * 842),
            270 => new XRect(.23 * 595, .10 * 842, .17 * 595, .80 * 842),
            _ => new XRect(.10 * 595, .23 * 842, .80 * 595, .17 * 842)
        };
        AssertNear(actual.X, expected.X, rotation, "X");
        AssertNear(actual.Y, expected.Y, rotation, "Y");
        AssertNear(actual.Width, expected.Width, rotation, "Width");
        AssertNear(actual.Height, expected.Height, rotation, "Height");
    }
    await PdfSigningService.SignAsync(source, signed, cert, placement, job);
    await PdfSigningService.SignAsync(source, signedVertical, cert,
        new SignaturePlacement(2, .84, .12, .10, .70), job);
    if (!originalHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source)))) throw new Exception("Original modified");

    ValidateSignature(signed);
    ValidateSignature(signedVertical);
    Console.WriteLine($"PASS rotation={rotation}: horizontal and vertical seals, CMS integrity, original preserved");
}

static void ValidateSignature(string path)
{
    var bytes = File.ReadAllBytes(path);
    var raw = Encoding.Latin1.GetString(bytes);
    if (Regex.IsMatch(raw, @"/Subtype\s*/Image\b")) throw new Exception("Seal must use vector QR modules, not raster images");
    var match = Regex.Match(raw, @"/ByteRange\s*\[\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*\]");
    if (!match.Success) throw new Exception("Missing ByteRange");
    var ranges = Enumerable.Range(1,4).Select(i=>int.Parse(match.Groups[i].Value)).ToArray();
    if (ranges[0] != 0 || ranges[2]+ranges[3] != bytes.Length) throw new Exception("Signature does not cover file");
    var payload = bytes.AsSpan(ranges[0],ranges[1]).ToArray().Concat(bytes.AsSpan(ranges[2],ranges[3]).ToArray()).ToArray();
    var hex = Encoding.ASCII.GetString(bytes,ranges[1]+1,ranges[2]-ranges[1]-2);
    var cms = new SignedCms(new ContentInfo(payload), detached:true);
    cms.Decode(Convert.FromHexString(hex));
    cms.CheckSignature(verifySignatureOnly:true);
}

static void AssertNear(double actual, double expected, int rotation, string field)
{
    if (Math.Abs(actual - expected) > 0.01)
        throw new Exception($"Rotation {rotation}: {field} expected {expected}, got {actual}");
}
