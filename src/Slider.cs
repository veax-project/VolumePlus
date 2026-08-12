using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VolumePlus;

/// <summary>Curseur horizontal dessine a la main, charte indigo.</summary>
public sealed class Slider : DrawControl
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum { get; set; } = 100;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum { get; set; } = 500;
    /// <summary>Marques verticales tous les N (0 = aucune).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TickStep { get; set; } = 100;

    public event EventHandler? ValueChanged;

    private int _value = 100;
    private bool _drag;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            int v = Math.Clamp(value, Minimum, Maximum);
            if (v == _value) return;
            _value = v;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Slider()
    {
        Height = 28;
        Cursor = Cursors.Hand;
    }

    private float Knob => 9f * DpiScale;
    private float TrackLeft => Knob + 2f;
    private float TrackRight => Width - Knob - 2f;
    private float TrackW => Math.Max(1f, TrackRight - TrackLeft);

    private void SetFromMouse(int x)
    {
        double t = (x - TrackLeft) / TrackW;
        t = Math.Clamp(t, 0, 1);
        Value = Minimum + (int)Math.Round(t * (Maximum - Minimum));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { _drag = true; SetFromMouse(e.X); }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_drag) SetFromMouse(e.X);
    }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _drag = false; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        float cy = Height / 2f;
        float t = (Value - Minimum) / (float)(Maximum - Minimum);
        float kx = TrackLeft + t * TrackW;

        // Rail.
        using (var railPen = new Pen(Palette.Track, 4f * DpiScale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(railPen, TrackLeft, cy, TrackRight, cy);

        // Marques.
        if (TickStep > 0)
        {
            using var tickPen = new Pen(Color.FromArgb(70, 74, 80), 1f);
            for (int v = Minimum; v <= Maximum; v += TickStep)
            {
                if (v == Minimum || v == Maximum) continue;
                float tx = TrackLeft + (v - Minimum) / (float)(Maximum - Minimum) * TrackW;
                g.DrawLine(tickPen, tx, cy - 7f * DpiScale, tx, cy + 7f * DpiScale);
            }
        }

        // Partie remplie.
        using (var fillPen = new Pen(Palette.Accent, 4f * DpiScale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(fillPen, TrackLeft, cy, kx, cy);

        // Pastille.
        using var kb = new SolidBrush(Palette.Knob);
        using var ring = new Pen(Palette.Accent, 2f * DpiScale);
        g.FillEllipse(kb, kx - Knob, cy - Knob, Knob * 2, Knob * 2);
        g.DrawEllipse(ring, kx - Knob, cy - Knob, Knob * 2, Knob * 2);
    }
}
