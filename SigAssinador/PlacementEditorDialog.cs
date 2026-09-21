using PdfiumViewer;

namespace SigAssinador;

internal sealed class PlacementEditorDialog : Form
{
    private readonly PdfDocument _pdf;
    private readonly SelectionCanvas _canvas = new();
    private readonly Label _pageLabel = new();
    private readonly Button _previousButton = new();
    private readonly Button _nextButton = new();
    private readonly Button _confirmButton = new();
    private Panel? _viewport;
    private int _pageIndex;

    public SignaturePlacement? Placement { get; private set; }

    public PlacementEditorDialog(string pdfPath)
    {
        _pdf = PdfDocument.Load(pdfPath);

        Text = "Escolha o local da assinatura";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(229, 237, 239);
        Font = new Font("Segoe UI", 10F);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.FromArgb(7, 53, 65)
        };
        header.Controls.Add(new Label
        {
            Text = "MARQUE O LOCAL DA ASSINATURA",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 16F),
            AutoSize = true,
            Location = new Point(24, 13)
        });
        header.Controls.Add(new Label
        {
            Text = "Clique e arraste sobre o documento. A assinatura pode ficar na horizontal ou girada 90°.",
            ForeColor = Color.FromArgb(205, 231, 235),
            AutoSize = true,
            Location = new Point(27, 45)
        });
        Controls.Add(header);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            BackColor = Color.White,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(18, 13, 18, 10),
            WrapContents = false
        };

        ConfigureToolbarButton(_previousButton, "◀ Página anterior", (_, _) => ChangePage(-1));
        ConfigureToolbarButton(_nextButton, "Próxima página ▶", (_, _) => ChangePage(1));
        _pageLabel.AutoSize = false;
        _pageLabel.Size = new Size(145, 38);
        _pageLabel.TextAlign = ContentAlignment.MiddleCenter;
        _pageLabel.Font = new Font("Segoe UI Semibold", 10F);
        _pageLabel.Margin = new Padding(8, 0, 8, 0);

        var clearButton = new Button();
        ConfigureToolbarButton(clearButton, "Limpar marcação", (_, _) =>
        {
            _canvas.ClearSelection();
            UpdateState();
        });
        clearButton.Margin = new Padding(28, 0, 8, 0);

        var cancelButton = new Button();
        ConfigureToolbarButton(cancelButton, "Cancelar", (_, _) => DialogResult = DialogResult.Cancel);
        cancelButton.Margin = new Padding(28, 0, 8, 0);

        _confirmButton.Text = "Confirmar local";
        _confirmButton.Size = new Size(150, 38);
        _confirmButton.BackColor = Color.FromArgb(7, 86, 101);
        _confirmButton.ForeColor = Color.White;
        _confirmButton.FlatStyle = FlatStyle.Flat;
        _confirmButton.FlatAppearance.BorderSize = 0;
        _confirmButton.Enabled = false;
        _confirmButton.Click += (_, _) => ConfirmSelection();

        toolbar.Controls.Add(_previousButton);
        toolbar.Controls.Add(_pageLabel);
        toolbar.Controls.Add(_nextButton);
        toolbar.Controls.Add(clearButton);
        toolbar.Controls.Add(cancelButton);
        toolbar.Controls.Add(_confirmButton);
        Controls.Add(toolbar);

        _viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(199, 211, 214),
            Padding = new Padding(24)
        };
        _canvas.BackColor = Color.White;
        _canvas.Location = new Point(24, 24);
        _canvas.SelectionChanged += (_, _) => UpdateState();
        _viewport.Controls.Add(_canvas);
        Controls.Add(_viewport);
        _viewport.BringToFront();
        header.BringToFront();
        toolbar.BringToFront();

        Shown += (_, _) => RenderCurrentPage();
        ResizeEnd += (_, _) => RenderCurrentPage();
    }

    private static void ConfigureToolbarButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Size = new Size(145, 38);
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Color.White;
        button.ForeColor = Color.FromArgb(7, 86, 101);
        button.FlatAppearance.BorderColor = Color.FromArgb(143, 174, 180);
        button.Margin = new Padding(4, 0, 4, 0);
        button.Click += handler;
    }

    private void ChangePage(int offset)
    {
        var next = Math.Clamp(_pageIndex + offset, 0, _pdf.PageCount - 1);
        if (next == _pageIndex) return;
        _pageIndex = next;
        _canvas.ClearSelection();
        RenderCurrentPage();
    }

    private void RenderCurrentPage()
    {
        if (_pdf.PageCount == 0 || _viewport is null) return;

        var pageSize = _pdf.PageSizes[_pageIndex];
        var maxWidth = Math.Max(500, Math.Min(1100, _viewport.ClientSize.Width - 90));
        var scale = maxWidth / pageSize.Width;
        var width = (int)Math.Round(pageSize.Width * scale);
        var height = (int)Math.Round(pageSize.Height * scale);

        var oldImage = _canvas.PageImage;
        _canvas.PageImage = _pdf.Render(_pageIndex, width, height, 96, 96, PdfRenderFlags.Annotations);
        _canvas.Size = new Size(width, height);
        _canvas.Location = new Point(Math.Max(24, (_viewport.ClientSize.Width - width) / 2), 24);
        oldImage?.Dispose();
        UpdateState();
        _canvas.Invalidate();
    }

    private void ConfirmSelection()
    {
        var selection = _canvas.Selection;
        if (selection is null) return;

        var rectangle = selection.Value;
        Placement = new SignaturePlacement(
            _pageIndex + 1,
            rectangle.X / (double)_canvas.Width,
            rectangle.Y / (double)_canvas.Height,
            rectangle.Width / (double)_canvas.Width,
            rectangle.Height / (double)_canvas.Height);
        DialogResult = DialogResult.OK;
    }

    private void UpdateState()
    {
        _pageLabel.Text = $"Página {_pageIndex + 1} de {_pdf.PageCount}";
        _previousButton.Enabled = _pageIndex > 0;
        _nextButton.Enabled = _pageIndex < _pdf.PageCount - 1;
        _confirmButton.Enabled = _canvas.Selection is not null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _canvas.PageImage?.Dispose();
            _pdf.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class SelectionCanvas : Control
{
    private Point _start;
    private Rectangle? _selection;
    private bool _drawing;

    public Image? PageImage { get; set; }
    public Rectangle? Selection => _selection;
    public event EventHandler? SelectionChanged;

    public SelectionCanvas()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void ClearSelection()
    {
        _selection = null;
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (PageImage is not null)
            e.Graphics.DrawImage(PageImage, ClientRectangle);

        if (_selection is { } rectangle)
        {
            using var fill = new SolidBrush(Color.FromArgb(55, 0, 150, 170));
            using var border = new Pen(Color.FromArgb(0, 105, 120), 3);
            e.Graphics.FillRectangle(fill, rectangle);
            e.Graphics.DrawRectangle(border, rectangle);
            using var font = new Font("Segoe UI Semibold", 9F);
            e.Graphics.DrawString("ASSINATURA", font, Brushes.White,
                new PointF(rectangle.X + 8, rectangle.Y + 7));
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _start = Clamp(e.Location);
        _selection = new Rectangle(_start, Size.Empty);
        _drawing = true;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_drawing) return;
        _selection = Normalize(_start, Clamp(e.Location));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_drawing) return;
        _drawing = false;
        Capture = false;
        var rectangle = Normalize(_start, Clamp(e.Location));
        var longSide = Math.Max(rectangle.Width, rectangle.Height);
        var shortSide = Math.Min(rectangle.Width, rectangle.Height);
        var minimumLongSide = Math.Max(180, (int)Math.Round(Math.Max(Width, Height) * 0.22));
        var minimumShortSide = Math.Max(60, (int)Math.Round(Math.Min(Width, Height) * 0.08));
        _selection = longSide >= minimumLongSide &&
                     shortSide >= minimumShortSide &&
                     longSide >= shortSide * 1.8
            ? rectangle
            : null;
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);

        if (_selection is null)
            MessageBox.Show(this,
                "Desenhe um retângulo maior e alongado, na horizontal ou na vertical, para que o QR Code e os dados fiquem legíveis.",
                "Área muito pequena", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private Point Clamp(Point point) => new(
        Math.Clamp(point.X, 0, Math.Max(0, Width - 1)),
        Math.Clamp(point.Y, 0, Math.Max(0, Height - 1)));

    private static Rectangle Normalize(Point first, Point second) => Rectangle.FromLTRB(
        Math.Min(first.X, second.X),
        Math.Min(first.Y, second.Y),
        Math.Max(first.X, second.X),
        Math.Max(first.Y, second.Y));
}
