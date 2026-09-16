using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace SigAssinador;

internal sealed class MainForm : Form
{
    private readonly Label _certificateLabel = new();
    private readonly Label _documentLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Button _selectCertificateButton = new();
    private readonly Button _selectDocumentButton = new();
    private readonly Button _signButton = new();
    private X509Certificate2? _certificate;
    private string? _inputPath;

    public MainForm()
    {
        Text = "Assinador SIG — ICP-Brasil A1 e A3";
        ClientSize = new Size(720, 500);
        MinimumSize = new Size(680, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(242, 248, 249);
        Font = new Font("Segoe UI", 10F);

        BuildInterface();
    }

    private void BuildInterface()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.FromArgb(7, 53, 65) };
        header.Controls.Add(new Label
        {
            Text = "SIG  ·  ASSINADOR LOCAL",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F),
            AutoSize = true,
            Location = new Point(28, 20)
        });
        header.Controls.Add(new Label
        {
            Text = "O certificado e a chave privada permanecem neste computador.",
            ForeColor = Color.FromArgb(205, 231, 235),
            AutoSize = true,
            Location = new Point(31, 57)
        });
        Controls.Add(header);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 24),
            ColumnCount = 1,
            RowCount = 7
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        body.Controls.Add(SectionTitle("1. Certificado digital"), 0, 0);
        body.Controls.Add(Row(_certificateLabel, _selectCertificateButton, "Selecionar certificado", SelectCertificate), 0, 1);
        body.Controls.Add(SectionTitle("2. Documento PDF"), 0, 2);
        body.Controls.Add(Row(_documentLabel, _selectDocumentButton, "Selecionar PDF", SelectDocument), 0, 3);

        _certificateLabel.Text = "Nenhum certificado selecionado";
        _documentLabel.Text = "Nenhum documento selecionado";
        _certificateLabel.ForeColor = _documentLabel.ForeColor = Color.FromArgb(75, 98, 104);

        _statusLabel.Text = "Pronto para iniciar.";
        _statusLabel.AutoSize = false;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.ForeColor = Color.FromArgb(75, 98, 104);
        body.Controls.Add(_statusLabel, 0, 4);

        _signButton.Text = "Assinar documento";
        _signButton.Dock = DockStyle.Fill;
        _signButton.Enabled = false;
        _signButton.BackColor = Color.FromArgb(7, 86, 101);
        _signButton.ForeColor = Color.White;
        _signButton.FlatStyle = FlatStyle.Flat;
        _signButton.FlatAppearance.BorderSize = 0;
        _signButton.Font = new Font("Segoe UI Semibold", 11F);
        _signButton.Click += async (_, _) => await SignDocumentAsync();
        body.Controls.Add(_signButton, 0, 5);

        body.Controls.Add(new Label
        {
            Text = "Segurança: a assinatura ocorre pelo provedor criptográfico do Windows ou pelo driver do token A3. O SIG não exporta nem copia a chave privada.",
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(75, 98, 104),
            Padding = new Padding(0, 16, 0, 0)
        }, 0, 6);

        Controls.Add(body);
        body.BringToFront();
    }

    private static Label SectionTitle(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI Semibold", 10F),
        ForeColor = Color.FromArgb(7, 53, 65)
    };

    private static Panel Row(Label label, Button button, string buttonText, EventHandler handler)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14) };
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        button.Text = buttonText;
        button.Dock = DockStyle.Right;
        button.Width = 190;
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Color.White;
        button.ForeColor = Color.FromArgb(7, 86, 101);
        button.FlatAppearance.BorderColor = Color.FromArgb(7, 86, 101);
        button.Click += handler;
        panel.Controls.Add(label);
        panel.Controls.Add(button);
        return panel;
    }

    private void SelectCertificate(object? sender, EventArgs e)
    {
        var selected = CertificateService.LetUserChoose(this);
        if (selected is null) return;
        _certificate?.Dispose();
        _certificate = selected;
        _certificateLabel.Text = CertificateService.Describe(selected);
        _statusLabel.Text = "Certificado selecionado. Escolha o documento PDF.";
        UpdateState();
    }

    private void SelectDocument(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Selecione o documento PDF",
            Filter = "Documentos PDF (*.pdf)|*.pdf",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _inputPath = dialog.FileName;
        _documentLabel.Text = Path.GetFileName(dialog.FileName);
        _statusLabel.Text = "Documento selecionado. Confira o certificado e clique em Assinar.";
        UpdateState();
    }

    private void UpdateState() => _signButton.Enabled = _certificate is not null && _inputPath is not null;

    private async Task SignDocumentAsync()
    {
        if (_certificate is null || _inputPath is null) return;
        using var dialog = new SaveFileDialog
        {
            Title = "Salvar documento assinado",
            Filter = "Documento PDF (*.pdf)|*.pdf",
            FileName = $"{Path.GetFileNameWithoutExtension(_inputPath)}-assinado.pdf",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetBusy(true, "Assinando localmente… Se for A3, confirme o PIN na janela do driver.");
        try
        {
            await PdfSigningService.SignAsync(_inputPath, dialog.FileName, _certificate);
            _statusLabel.Text = "Documento assinado com sucesso.";
            var result = MessageBox.Show(this,
                "O documento foi assinado com sucesso. Deseja abrir a pasta do arquivo?",
                "Assinatura concluída", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
                Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Não foi possível concluir a assinatura.";
            MessageBox.Show(this,
                $"A assinatura não foi concluída.\r\n\r\n{ex.Message}",
                "Erro ao assinar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, _statusLabel.Text);
        }
    }

    private void SetBusy(bool busy, string status)
    {
        UseWaitCursor = busy;
        _selectCertificateButton.Enabled = !busy;
        _selectDocumentButton.Enabled = !busy;
        _signButton.Enabled = !busy && _certificate is not null && _inputPath is not null;
        _statusLabel.Text = status;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _certificate?.Dispose();
        base.Dispose(disposing);
    }
}
