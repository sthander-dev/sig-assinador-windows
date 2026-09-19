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
    await PdfSigningService.SignAsync(source, signed, cert, new SignaturePlacement(2, .10, .60, .80, .17), job);
    if (!originalHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source)))) throw new Exception("Original modified");

    var bytes = File.ReadAllBytes(signed);
    var raw = Encoding.Latin1.GetString(bytes);
    var match = Regex.Match(raw, @"/ByteRange\s*\[\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*\]");
    if (!match.Success) throw new Exception("Missing ByteRange");
    var ranges = Enumerable.Range(1,4).Select(i=>int.Parse(match.Groups[i].Value)).ToArray();
    if (ranges[0] != 0 || ranges[2]+ranges[3] != bytes.Length) throw new Exception("Signature does not cover file");
    var payload = bytes.AsSpan(ranges[0],ranges[1]).ToArray().Concat(bytes.AsSpan(ranges[2],ranges[3]).ToArray()).ToArray();
    var hex = Encoding.ASCII.GetString(bytes,ranges[1]+1,ranges[2]-ranges[1]-2);
    var cms = new SignedCms(new ContentInfo(payload), detached:true);
    cms.Decode(Convert.FromHexString(hex));
    cms.CheckSignature(verifySignatureOnly:true);
    Console.WriteLine($"PASS rotation={rotation}: signed byte ranges, CMS integrity, original preserved");
}
