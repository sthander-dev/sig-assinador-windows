namespace SigAssinador;

internal sealed class PositionDialog : Form
{
    private readonly NumericUpDown _pageSelector = new();
    private readonly Dictionary<SignaturePosition, Button> _positionButtons = new();
    private SignaturePosition _selectedPosition = SignaturePosition.BottomRight;

    public SignaturePlacement Placement => new((int)_pageSelector.Value, _selectedPosition);

    public PositionDialog(int pageCount)
    {
        Text = "Posição da assinatura";
        ClientSize = new Size(520, 500);
        MinimumSize = MaximumSize = new Size(536, 539);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(242, 248, 249);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var title = new Label
        {
            Text = "Onde deseja inserir a assinatura visível?",
            Font = new Font("Segoe UI Semibold", 14F),
            ForeColor = Color.FromArgb(7, 53, 65),
            AutoSize = true,
            Location = new Point(28, 24)
        };
        Controls.Add(title);

        Controls.Add(new Label
        {
            Text = "Página do documento:",
            AutoSize = true,
            Location = new Point(30, 72),
            ForeColor = Color.FromArgb(45, 68, 74)
        });

        _pageSelector.Minimum = 1;
        _pageSelector.Maximum = Math.Max(1, pageCount);
        _pageSelector.Value = pageCount;
        _pageSelector.Width = 74;
        _pageSelector.Location = new Point(184, 68);
        Controls.Add(_pageSelector);

        Controls.Add(new Label
        {
            Text = $"de {pageCount}",
            AutoSize = true,
            Location = new Point(266, 72),
            ForeColor = Color.FromArgb(75, 98, 104)
        });

        var pagePanel = new Panel
        {
            Location = new Point(84, 112),
            Size = new Size(352, 270),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(14)
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(5)
        };
        for (var index = 0; index < 3; index++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
        }

        AddPositionButton(grid, SignaturePosition.TopLeft, 0, 0, "Superior\nesquerda");
        AddPositionButton(grid, SignaturePosition.TopCenter, 1, 0, "Superior\ncentro");
        AddPositionButton(grid, SignaturePosition.TopRight, 2, 0, "Superior\ndireita");
        AddPositionButton(grid, SignaturePosition.MiddleLeft, 0, 1, "Meio\nesquerda");
        AddPositionButton(grid, SignaturePosition.MiddleCenter, 1, 1, "Centro");
        AddPositionButton(grid, SignaturePosition.MiddleRight, 2, 1, "Meio\ndireita");
        AddPositionButton(grid, SignaturePosition.BottomLeft, 0, 2, "Inferior\nesquerda");
        AddPositionButton(grid, SignaturePosition.BottomCenter, 1, 2, "Inferior\ncentro");
        AddPositionButton(grid, SignaturePosition.BottomRight, 2, 2, "Inferior\ndireita");
        pagePanel.Controls.Add(grid);
        Controls.Add(pagePanel);

        Controls.Add(new Label
        {
            Text = "A marca visível será inserida na área escolhida. A assinatura digital protege o documento inteiro.",
            AutoSize = false,
            Location = new Point(38, 392),
            Size = new Size(444, 38),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(75, 98, 104)
        });

        var cancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new Point(264, 444),
            Size = new Size(104, 38),
            FlatStyle = FlatStyle.Flat
        };
        var confirm = new Button
        {
            Text = "Confirmar posição",
            DialogResult = DialogResult.OK,
            Location = new Point(376, 444),
            Size = new Size(128, 38),
            BackColor = Color.FromArgb(7, 86, 101),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        confirm.FlatAppearance.BorderSize = 0;
        Controls.Add(cancel);
        Controls.Add(confirm);
        AcceptButton = confirm;
        CancelButton = cancel;

        UpdateSelection();
    }

    private void AddPositionButton(TableLayoutPanel grid, SignaturePosition position, int column, int row, string text)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(238, 246, 247),
            ForeColor = Color.FromArgb(7, 86, 101),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Tag = position
        };
        button.Click += (_, _) =>
        {
            _selectedPosition = (SignaturePosition)button.Tag;
            UpdateSelection();
        };
        _positionButtons[position] = button;
        grid.Controls.Add(button, column, row);
    }

    private void UpdateSelection()
    {
        foreach (var pair in _positionButtons)
        {
            var selected = pair.Key == _selectedPosition;
            pair.Value.Text = pair.Value.Text.TrimStart('✓', ' ');
            if (selected) pair.Value.Text = $"✓ {pair.Value.Text}";
            pair.Value.BackColor = selected ? Color.FromArgb(7, 86, 101) : Color.FromArgb(238, 246, 247);
            pair.Value.ForeColor = selected ? Color.White : Color.FromArgb(7, 86, 101);
            pair.Value.FlatAppearance.BorderColor = selected
                ? Color.FromArgb(7, 86, 101)
                : Color.FromArgb(174, 203, 208);
        }
    }
}
