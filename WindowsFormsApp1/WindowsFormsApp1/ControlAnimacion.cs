using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Dibuja el sistema vertical masa-resorte-amortiguador de 3 masas y anima el
    /// desplazamiento vertical relativo de cada masa. La fuerza F(t) actúa sobre m3.
    /// </summary>
    public class ControlAnimacion : Control
    {
        // Desplazamientos verticales actuales en píxeles (ya escalados por el factor).
        private double _off1, _off2, _off3;

        private const int BloqueAncho = 150;
        private const int BloqueAlto = 38;
        private const int Margen = 18;

        public ControlAnimacion()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        /// <summary>Fija el frame actual (offsets en píxeles) y repinta.</summary>
        public void SetFrame(double off1, double off2, double off3)
        {
            _off1 = off1;
            _off2 = off2;
            _off3 = off3;
            Invalidate();
        }

        public void Reiniciar()
        {
            _off1 = _off2 = _off3 = 0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int topY = Margen;
            int botY = Height - Margen;
            int cx = w / 2;

            // Posiciones de reposo (parte superior de cada bloque).
            int segmento = (botY - topY - 3 * BloqueAlto) / 4;
            if (segmento < 20) segmento = 20;
            int rest1 = topY + segmento;
            int rest2 = rest1 + BloqueAlto + segmento;
            int rest3 = rest2 + BloqueAlto + segmento;

            // Posiciones reales con desplazamiento.
            int y1 = rest1 + (int)_off1;
            int y2 = rest2 + (int)_off2;
            int y3 = rest3 + (int)_off3;

            // Soporte superior e inferior (rayado).
            DibujarSoporte(g, cx - BloqueAncho / 2 - 25, topY, BloqueAncho + 50, true);
            DibujarSoporte(g, cx - BloqueAncho / 2 - 25, botY, BloqueAncho + 50, false);

            int sx = cx - 38;   // x del resorte
            int dx = cx + 38;   // x del amortiguador

            // Conectores: techo->m1, m1->m2, m2->m3, m3->piso.
            DibujarConector(g, sx, dx, topY, y1, "k1", "b1");
            DibujarConector(g, sx, dx, y1 + BloqueAlto, y2, "k2", "b2");
            DibujarConector(g, sx, dx, y2 + BloqueAlto, y3, "k3", "b3");
            DibujarConector(g, sx, dx, y3 + BloqueAlto, botY, "k4", "b4");

            // Bloques de las masas.
            DibujarMasa(g, cx, y1, Color.FromArgb(120, 170, 220), "m1");
            DibujarMasa(g, cx, y2, Color.FromArgb(140, 200, 140), "m2");
            DibujarMasa(g, cx, y3, Color.FromArgb(235, 205, 120), "m3");

            // Flecha de la fuerza F(t) sobre m3.
            DibujarFuerza(g, cx - BloqueAncho / 2 - 20, y3 + BloqueAlto / 2);
        }

        private void DibujarSoporte(Graphics g, int x, int y, int ancho, bool arriba)
        {
            using (var pen = new Pen(Color.DimGray, 2))
            {
                g.DrawLine(pen, x, y, x + ancho, y);
                int dir = arriba ? -1 : 1;
                for (int i = 0; i <= ancho; i += 10)
                    g.DrawLine(pen, x + i, y, x + i - 8, y + dir * 8);
            }
        }

        private void DibujarMasa(Graphics g, int cx, int top, Color color, string etiqueta)
        {
            var rect = new Rectangle(cx - BloqueAncho / 2, top, BloqueAncho, BloqueAlto);
            using (var b = new SolidBrush(color))
            using (var p = new Pen(Color.FromArgb(60, 60, 60), 1.5f))
            {
                g.FillRectangle(b, rect);
                g.DrawRectangle(p, rect);
            }
            TextRenderer.DrawText(g, etiqueta, new Font("Segoe UI", 11, FontStyle.Bold),
                rect, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DibujarConector(Graphics g, int sx, int dx, int yTop, int yBot, string lblK, string lblB)
        {
            if (yBot < yTop + 6) yBot = yTop + 6; // evita inversión visual
            DibujarResorte(g, sx, yTop, yBot);
            DibujarAmortiguador(g, dx, yTop, yBot);

            using (var f = new Font("Segoe UI", 7.5f))
            {
                g.DrawString(lblK, f, Brushes.DarkGreen, sx - 24, (yTop + yBot) / 2 - 7);
                g.DrawString(lblB, f, Brushes.Firebrick, dx + 8, (yTop + yBot) / 2 - 7);
            }
        }

        private void DibujarResorte(Graphics g, int x, int y1, int y2)
        {
            using (var pen = new Pen(Color.SeaGreen, 1.8f))
            {
                int espiras = 6;
                int amplitud = 9;
                double largo = y2 - y1;
                double paso = largo / (espiras + 1);

                var pts = new System.Collections.Generic.List<PointF>();
                pts.Add(new PointF(x, y1));
                pts.Add(new PointF(x, (float)(y1 + paso)));
                for (int i = 0; i < espiras; i++)
                {
                    int lado = (i % 2 == 0) ? 1 : -1;
                    pts.Add(new PointF(x + lado * amplitud, (float)(y1 + paso * (i + 1.5))));
                }
                pts.Add(new PointF(x, (float)(y2 - paso)));
                pts.Add(new PointF(x, y2));
                g.DrawLines(pen, pts.ToArray());
            }
        }

        private void DibujarAmortiguador(Graphics g, int x, int y1, int y2)
        {
            using (var pen = new Pen(Color.Firebrick, 1.8f))
            {
                int medio = (y1 + y2) / 2;
                int cilAlto = Math.Max(14, (y2 - y1) / 3);
                int cilTop = medio - cilAlto / 2;
                int cilBot = medio + cilAlto / 2;
                int ancho = 9;

                // Vástago superior y cilindro (abierto abajo).
                g.DrawLine(pen, x, y1, x, cilTop);
                g.DrawLine(pen, x - ancho, cilTop, x + ancho, cilTop);
                g.DrawLine(pen, x - ancho, cilTop, x - ancho, cilBot);
                g.DrawLine(pen, x + ancho, cilTop, x + ancho, cilBot);

                // Pistón inferior.
                g.DrawLine(pen, x, y2, x, medio);
                g.DrawLine(pen, x - ancho + 2, medio, x + ancho - 2, medio);
            }
        }

        private void DibujarFuerza(Graphics g, int x, int y)
        {
            using (var pen = new Pen(Color.Red, 2.2f))
            {
                pen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(pen, x, y - 22, x, y + 10);
                g.DrawString("F(t)", new Font("Segoe UI", 8.5f, FontStyle.Bold), Brushes.Red, x - 30, y - 26);
            }
        }
    }
}
