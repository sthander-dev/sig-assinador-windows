using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace SigAssinador;

internal static class CertificateService
{
    public static X509Certificate2Collection FindSigningCertificates()
    {
        var result = new X509Certificate2Collection();
        AddFromStore(result, StoreLocation.CurrentUser);
        AddFromStore(result, StoreLocation.LocalMachine);

        var now = DateTime.Now;
        var valid = result
            .OfType<X509Certificate2>()
            .Where(c => c.HasPrivateKey && c.NotBefore <= now && c.NotAfter > now)
            .GroupBy(c => c.Thumbprint, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(c => c.GetNameInfo(X509NameType.SimpleName, false))
            .ToArray();

        return new X509Certificate2Collection(valid);
    }

    public static X509Certificate2? LetUserChoose(IWin32Window owner)
    {
        var certificates = FindSigningCertificates();
        if (certificates.Count == 0)
        {
            MessageBox.Show(owner,
                "Nenhum certificado válido com chave privada foi encontrado. Instale o A1 ou conecte o A3 e o respectivo driver.",
                "Certificado não encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var selected = X509Certificate2UI.SelectFromCollection(
            certificates,
            "Selecionar certificado ICP-Brasil",
            "Escolha o certificado que será usado para assinar o documento.",
            X509SelectionFlag.SingleSelection,
            owner.Handle);

        return selected.Count == 1 ? selected[0] : null;
    }

    public static string Describe(X509Certificate2 certificate)
    {
        var name = certificate.GetNameInfo(X509NameType.SimpleName, false);
        return $"{name}\r\nVálido até {certificate.NotAfter:dd/MM/yyyy} · Final {certificate.Thumbprint[^8..]}";
    }

    public static string GetCnpj(X509Certificate2 certificate)
    {
        var source = $"{certificate.GetNameInfo(X509NameType.SimpleName, false)} {certificate.Subject}";
        var matches = Regex.Matches(source, @"(?<!\d)\d{14}(?!\d)");
        if (matches.Count == 0)
            throw new InvalidOperationException("Não foi possível identificar o CNPJ no certificado selecionado.");
        return matches[0].Value;
    }

    public static string FormatCnpj(string value) =>
        Regex.Replace(value, @"^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$", "$1.$2.$3/$4-$5");

    private static void AddFromStore(X509Certificate2Collection target, StoreLocation location)
    {
        try
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            target.AddRange(store.Certificates);
        }
        catch (CryptographicException)
        {
            // Uma loja indisponível não impede a leitura da outra.
        }
    }
}
