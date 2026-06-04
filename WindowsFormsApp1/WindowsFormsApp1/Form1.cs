using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // ---- Controles de configuración ----
        private TextBox txtOctave;
        private Button btnExaminar;
        private readonly TextBox[] txtK = new TextBox[4];
        private readonly TextBox[] txtB = new TextBox[4];
        private readonly TextBox[] txtM = new TextBox[3];
        private ComboBox cmbVarK, cmbVarB, cmbVarM;
        private TextBox txtKMin, txtKMax, txtKPaso;
        private TextBox txtBMin, txtBMax, txtBPaso;
        private TextBox txtMMin, txtMMax, txtMPaso;
        private TextBox txtElementos, txtFactor;
        private Button btnCombos, btnSimular;
        private Label lblInfo;
        private TextBox txtGs;

        // ---- Resultados y animación ----
        private FlowLayoutPanel flpResultados;
        private ControlAnimacion anim;
        private Button btnPlay, btnPause, btnReset;
        private RadioButton rbPaso, rbImpulso;
        private Label lblTiempo;
        private Timer timerAnim;
        private Chart chartVivo;

        private readonly List<ResultadoIteracion> _resultados = new List<ResultadoIteracion>();
        private List<SistemaParametros> _combinaciones = new List<SistemaParametros>();
        private RespuestaDinamica _animFuente;
        private int _frame;

        public Form1()
        {
            InitializeComponent();
            BuildUi();
        }

        // ============================================================
        //  CONSTRUCCIÓN DE LA INTERFAZ (por código)
        // ============================================================
        private void BuildUi()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var tabSim = new TabPage("Simulación");
            var tabAnim = new TabPage("Animación");
            tabs.TabPages.Add(tabSim);
            tabs.TabPages.Add(tabAnim);
            Controls.Add(tabs);

            // ---- Panel de configuración (izquierda) ----
            var pnlConfig = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 372,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(6),
                BackColor = Color.FromArgb(245, 245, 248)
            };

            pnlConfig.Controls.Add(GrupoOctave());
            pnlConfig.Controls.Add(GrupoConstante("Constante k (rigidez) [N/m]", txtK, out cmbVarK,
                out txtKMin, out txtKMax, out txtKPaso, new[] { "k1", "k2", "k3", "k4" }, "100"));
            pnlConfig.Controls.Add(GrupoConstante("Constante b (amortiguamiento) [N·s/m]", txtB, out cmbVarB,
                out txtBMin, out txtBMax, out txtBPaso, new[] { "b1", "b2", "b3", "b4" }, "2"));
            pnlConfig.Controls.Add(GrupoConstante("Masa m [kg]", txtM, out cmbVarM,
                out txtMMin, out txtMMax, out txtMPaso, new[] { "m1", "m2", "m3" }, "1"));
            pnlConfig.Controls.Add(GrupoSimulacion());
            pnlConfig.Controls.Add(GrupoGs());

            // ---- Panel de resultados (derecha) ----
            flpResultados = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White
            };

            // El Fill debe agregarse primero para ocupar el espacio restante.
            tabSim.Controls.Add(flpResultados);
            tabSim.Controls.Add(pnlConfig);

            // ---- Pestaña de animación ----
            ConstruirTabAnimacion(tabAnim);

            // Valores por defecto de los rangos.
            cmbVarK.SelectedIndex = 0; txtKMin.Text = "100"; txtKMax.Text = "200"; txtKPaso.Text = "100";
            cmbVarB.SelectedIndex = 0; txtBMin.Text = "1"; txtBMax.Text = "2"; txtBPaso.Text = "1";
            cmbVarM.SelectedIndex = 0; txtMMin.Text = "1"; txtMMax.Text = "2"; txtMPaso.Text = "1";
        }

        private GroupBox Grupo(string titulo, int alto)
        {
            return new GroupBox
            {
                Text = titulo,
                Width = 350,
                Height = alto,
                Margin = new Padding(3, 3, 3, 8),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
        }

        private static Label Lbl(string t, int x, int y, int w = 60)
            => new Label { Text = t, Left = x, Top = y, Width = w, Font = new Font("Segoe UI", 8.5f), AutoSize = false };

        private static TextBox Txt(string val, int x, int y, int w)
            => new TextBox { Text = val, Left = x, Top = y, Width = w, Font = new Font("Segoe UI", 9) };

        private GroupBox GrupoOctave()
        {
            var g = Grupo("Ruta del CLI de Octave", 92);
            g.Controls.Add(Lbl("octave-cli.exe (o 'octave-cli' si está en PATH):", 8, 22, 330));
            txtOctave = Txt(@"C:\Program Files\GNU Octave\Octave-11.1.0\mingw64\bin\octave-cli.exe", 8, 42, 250);
            btnExaminar = new Button { Text = "Examinar…", Left = 264, Top = 41, Width = 78 };
            btnExaminar.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "octave-cli|octave-cli.exe|Ejecutables|*.exe|Todos|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtOctave.Text = ofd.FileName;
            };
            g.Controls.Add(txtOctave);
            g.Controls.Add(btnExaminar);
            return g;
        }

        private GroupBox GrupoConstante(string titulo, TextBox[] fijos, out ComboBox combo,
            out TextBox min, out TextBox max, out TextBox paso, string[] nombres, string valDefecto)
        {
            var g = Grupo(titulo, 132);

            // Fila de valores fijos.
            int x = 8;
            for (int i = 0; i < nombres.Length; i++)
            {
                g.Controls.Add(Lbl(nombres[i], x, 25, 22));
                fijos[i] = Txt(valDefecto, x + 22, 22, 48);
                g.Controls.Add(fijos[i]);
                x += 80;
            }

            // Combo: cuál variar.
            g.Controls.Add(Lbl("Variar:", 8, 60, 48));
            combo = new ComboBox { Left = 58, Top = 57, Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
            combo.Items.AddRange(nombres);
            g.Controls.Add(combo);

            // Fila de rango.
            g.Controls.Add(Lbl("Mín", 8, 95, 30));
            min = Txt("", 40, 92, 55); g.Controls.Add(min);
            g.Controls.Add(Lbl("Máx", 102, 95, 32));
            max = Txt("", 136, 92, 55); g.Controls.Add(max);
            g.Controls.Add(Lbl("Paso", 198, 95, 34));
            paso = Txt("", 236, 92, 55); g.Controls.Add(paso);

            return g;
        }

        private GroupBox GrupoSimulacion()
        {
            var g = Grupo("Parámetros de simulación", 185);
            g.Controls.Add(Lbl("N.º de elementos:", 8, 25, 110));
            txtElementos = Txt("200", 120, 22, 70); g.Controls.Add(txtElementos);
            g.Controls.Add(Lbl("Factor animación:", 8, 53, 110));
            txtFactor = Txt("800", 120, 50, 70); g.Controls.Add(txtFactor);

            var lblF = Lbl("Fuerza F(t) aplicada en: m3", 8, 80, 330);
            lblF.Font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Italic);
            g.Controls.Add(lblF);

            btnCombos = new Button { Text = "Calcular combinaciones", Left = 8, Top = 104, Width = 165 };
            btnCombos.Click += (s, e) => CalcularCombinaciones(true);
            g.Controls.Add(btnCombos);

            btnSimular = new Button { Text = "Simular", Left = 178, Top = 104, Width = 164, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnSimular.Click += (s, e) => Simular();
            g.Controls.Add(btnSimular);

            lblInfo = new Label { Left = 8, Top = 138, Width = 334, Height = 40, Font = new Font("Segoe UI", 8.5f), AutoSize = false };
            g.Controls.Add(lblInfo);
            return g;
        }

        private GroupBox GrupoGs()
        {
            var g = Grupo("Funciones de transferencia G(s) (última combinación)", 210);
            txtGs = new TextBox
            {
                Left = 8,
                Top = 20,
                Width = 334,
                Height = 180,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8.5f),
                BackColor = Color.White
            };
            g.Controls.Add(txtGs);
            return g;
        }

        private void ConstruirTabAnimacion(TabPage tab)
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(238, 238, 242) };
            btnPlay = new Button { Text = "▶ Reproducir", Left = 10, Top = 12, Width = 100 };
            btnPause = new Button { Text = "⏸ Pausar", Left = 116, Top = 12, Width = 90 };
            btnReset = new Button { Text = "⟲ Reiniciar", Left = 212, Top = 12, Width = 95 };
            rbPaso = new RadioButton { Text = "Respuesta al paso", Left = 330, Top = 14, Width = 140, Checked = true };
            rbImpulso = new RadioButton { Text = "Respuesta al impulso", Left = 478, Top = 14, Width = 150 };
            lblTiempo = new Label { Text = "t = 0.000 s", Left = 650, Top = 16, Width = 160, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            btnPlay.Click += (s, e) => Reproducir();
            btnPause.Click += (s, e) => timerAnim.Stop();
            btnReset.Click += (s, e) => { timerAnim.Stop(); _frame = 0; anim.Reiniciar(); ReiniciarChartVivo(); lblTiempo.Text = "t = 0.000 s"; };
            rbPaso.CheckedChanged += (s, e) => CambiarFuenteAnim();
            rbImpulso.CheckedChanged += (s, e) => CambiarFuenteAnim();

            pnlTop.Controls.AddRange(new Control[] { btnPlay, btnPause, btnReset, rbPaso, rbImpulso, lblTiempo });

            anim = new ControlAnimacion { Dock = DockStyle.Fill };
            chartVivo = CrearChartVivo();   // gráfica en vivo (a la derecha)

            // Orden de docking: Fill primero, luego el borde derecho, y el Top al final
            // para que la barra de controles abarque todo el ancho.
            tab.Controls.Add(anim);
            tab.Controls.Add(chartVivo);
            tab.Controls.Add(pnlTop);

            timerAnim = new Timer { Interval = 40 };
            timerAnim.Tick += TimerAnim_Tick;
        }

        /// <summary>Gráfica que se dibuja punto a punto al ritmo de la animación.</summary>
        private Chart CrearChartVivo()
        {
            var ch = new Chart { Dock = DockStyle.Right, Width = 500, BackColor = Color.White };
            var area = new ChartArea("viva");
            area.AxisX.Title = "t [s]";
            area.AxisY.Title = "y [m]";
            area.AxisX.Minimum = 0;                 // origen fijo; el máximo crece solo
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            ch.ChartAreas.Add(area);
            ch.Titles.Add(new Title("Respuesta en tiempo real (sincronizada con la animación)",
                Docking.Top, new Font("Segoe UI", 9, FontStyle.Bold), Color.Black));
            ch.Legends.Add(new Legend { Docking = Docking.Bottom, Font = new Font("Segoe UI", 8) });
            ch.Series.Add(new Series("y1") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.RoyalBlue });
            ch.Series.Add(new Series("y2") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.SeaGreen });
            ch.Series.Add(new Series("y3") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.DarkOrange });
            return ch;
        }

        /// <summary>Borra los puntos dibujados y deja los ejes en modo automático.</summary>
        private void ReiniciarChartVivo()
        {
            if (chartVivo == null) return;
            foreach (var s in chartVivo.Series) s.Points.Clear();
            var area = chartVivo.ChartAreas[0];
            area.AxisX.Minimum = 0;
            area.AxisX.Maximum = double.NaN;        // auto
            area.AxisY.Minimum = double.NaN;        // auto
            area.AxisY.Maximum = double.NaN;        // auto
            chartVivo.Invalidate();
        }

        // ============================================================
        //  LÓGICA: combinaciones, validaciones y simulación
        // ============================================================
        private static bool TryNum(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(',', '.');
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
        }

        private bool LeerSistemaBase(out SistemaParametros p, out string error)
        {
            p = new SistemaParametros();
            error = null;
            for (int i = 0; i < 4; i++)
            {
                if (!TryNum(txtK[i].Text, out double kv)) { error = $"k{i + 1} no es un número válido."; return false; }
                if (kv <= 0) { error = $"k{i + 1} debe ser positivo."; return false; }
                p.K[i] = kv;

                if (!TryNum(txtB[i].Text, out double bv)) { error = $"b{i + 1} no es un número válido."; return false; }
                if (bv < 0) { error = $"b{i + 1} no puede ser negativo."; return false; }
                p.B[i] = bv;
            }
            for (int i = 0; i < 3; i++)
            {
                if (!TryNum(txtM[i].Text, out double mv)) { error = $"m{i + 1} no es un número válido."; return false; }
                if (mv <= 0) { error = $"m{i + 1} debe ser positiva."; return false; }
                p.M[i] = mv;
            }
            return true;
        }

        private bool LeerRango(string nombre, TextBox tMin, TextBox tMax, TextBox tPaso,
            bool permiteCero, out double[] valores, out string error)
        {
            valores = null; error = null;
            if (!TryNum(tMin.Text, out double mn) || !TryNum(tMax.Text, out double mx) || !TryNum(tPaso.Text, out double ps))
            { error = $"Rango de {nombre}: valores no numéricos."; return false; }
            if (ps <= 0) { error = $"Rango de {nombre}: el paso debe ser mayor que cero."; return false; }
            if (mx < mn) { error = $"Rango de {nombre}: el máximo no puede ser menor que el mínimo."; return false; }
            if (!permiteCero && mn <= 0) { error = $"Rango de {nombre}: los valores deben ser positivos."; return false; }
            if (permiteCero && mn < 0) { error = $"Rango de {nombre}: los valores no pueden ser negativos."; return false; }
            valores = new RangoParametro { Min = mn, Max = mx, Paso = ps }.Generar();
            return true;
        }

        /// <summary>Construye las combinaciones y, si <paramref name="mostrar"/>, actualiza el panel de info.</summary>
        private bool CalcularCombinaciones(bool mostrar)
        {
            if (!LeerSistemaBase(out var baseP, out string e1)) { if (mostrar) Aviso(e1); return false; }

            // k positiva, c >= 0, m positiva.
            if (!LeerRango("k", txtKMin, txtKMax, txtKPaso, false, out var vk, out string e2)) { if (mostrar) Aviso(e2); return false; }
            if (!LeerRango("b", txtBMin, txtBMax, txtBPaso, true, out var vb, out string e3)) { if (mostrar) Aviso(e3); return false; }
            if (!LeerRango("m", txtMMin, txtMMax, txtMPaso, false, out var vm, out string e4)) { if (mostrar) Aviso(e4); return false; }

            _combinaciones = GeneradorCombinaciones.Generar(baseP,
                cmbVarK.SelectedIndex, vk, cmbVarB.SelectedIndex, vb, cmbVarM.SelectedIndex, vm);

            int n = _combinaciones.Count;
            if (mostrar)
                lblInfo.Text = $"n_k={vk.Length}, n_b={vb.Length}, n_m={vm.Length}\n" +
                               $"Combinaciones: N = {n}\n" +
                               $"Simulaciones a ejecutar: {2 * n}  (paso + impulso)";
            return true;
        }

        private void Simular()
        {
            if (string.IsNullOrWhiteSpace(txtOctave.Text))
            {
                if (Aviso("No se indicó la ruta del CLI de Octave. ¿Intentar con 'octave-cli' del PATH?",
                    MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                txtOctave.Text = "octave-cli";
            }
            if (!int.TryParse(txtElementos.Text.Trim(), out int nElem) || nElem < 2)
            { Aviso("El n.º de elementos debe ser un entero ≥ 2."); return; }
            if (!TryNum(txtFactor.Text, out _)) { Aviso("El factor de animación no es válido."); return; }
            if (!CalcularCombinaciones(true)) return;

            int n = _combinaciones.Count;
            if (n == 0) { Aviso("No se generaron combinaciones."); return; }
            if (2 * n > 100 &&
                Aviso($"Se ejecutarán {2 * n} simulaciones. ¿Continuar?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            timerAnim.Stop();
            _resultados.Clear();
            flpResultados.Controls.Clear();
            Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var motor = new MotorOctave(txtOctave.Text.Trim());
                for (int i = 0; i < n; i++)
                {
                    var p = _combinaciones[i];
                    lblInfo.Text = $"Simulando iteración {i + 1} de {n}…";
                    lblInfo.Refresh();

                    motor.Simular(p, nElem, out var paso, out var impulso);

                    FuncionesTransferencia.Calcular(p, out string g1, out string g2, out string g3, out _);
                    var res = new ResultadoIteracion
                    {
                        Indice = i + 1,
                        Parametros = p,
                        Paso = paso,
                        Impulso = impulso,
                        G1 = g1,
                        G2 = g2,
                        G3 = g3
                    };
                    _resultados.Add(res);
                    flpResultados.Controls.Add(PanelResultado(res));
                    Application.DoEvents();
                }

                lblInfo.Text = $"Listo. {n} combinaciones · {2 * n} simulaciones ejecutadas.";
                MostrarGsUltima();
                CambiarFuenteAnim();
            }
            catch (Exception ex)
            {
                Aviso("Error al ejecutar Octave:\n\n" + ex.Message +
                      "\n\nVerifique la ruta del octave-cli.exe y que el paquete 'control' esté instalado.");
            }
            finally
            {
                Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        // ============================================================
        //  PRESENTACIÓN DE RESULTADOS
        // ============================================================
        private Control PanelResultado(ResultadoIteracion r)
        {
            var pnl = new Panel { Width = 860, Height = 330, Margin = new Padding(6), BorderStyle = BorderStyle.FixedSingle };

            var cab = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(6, 4, 4, 4),
                Text = $"Iteración {r.Indice}   ·   {r.Parametros}\n" +
                       $"Máx |y| paso:   y1={Fmt(r.Paso.MaxAbs(0))}  y2={Fmt(r.Paso.MaxAbs(1))}  y3={Fmt(r.Paso.MaxAbs(2))} m" +
                       $"      |      impulso:   y1={Fmt(r.Impulso.MaxAbs(0))}  y2={Fmt(r.Impulso.MaxAbs(1))}  y3={Fmt(r.Impulso.MaxAbs(2))} m\n" +
                       $"Tiempo de simulación:   paso = {r.Paso.Milisegundos} ms      impulso = {r.Impulso.Milisegundos} ms"
            };

            var chPaso = CrearChart("Respuesta al paso", r.Paso);
            var chImp = CrearChart("Respuesta al impulso", r.Impulso);
            chPaso.Left = 4; chPaso.Top = 60; chPaso.Width = 420; chPaso.Height = 262;
            chImp.Left = 432; chImp.Top = 60; chImp.Width = 420; chImp.Height = 262;

            pnl.Controls.Add(chPaso);
            pnl.Controls.Add(chImp);
            pnl.Controls.Add(cab);
            return pnl;
        }

        private Chart CrearChart(string titulo, RespuestaDinamica r)
        {
            var ch = new Chart { BackColor = Color.White };
            var area = new ChartArea("a");
            area.AxisX.Title = "t [s]";
            area.AxisY.Title = "y [m]";
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            ch.ChartAreas.Add(area);
            ch.Titles.Add(new Title(titulo, Docking.Top, new Font("Segoe UI", 9, FontStyle.Bold), Color.Black));
            ch.Legends.Add(new Legend { Docking = Docking.Bottom, Font = new Font("Segoe UI", 7.5f) });

            ch.Series.Add(Serie("y1", r.T, r.Y1, Color.RoyalBlue));
            ch.Series.Add(Serie("y2", r.T, r.Y2, Color.SeaGreen));
            ch.Series.Add(Serie("y3", r.T, r.Y3, Color.DarkOrange));
            return ch;
        }

        private static Series Serie(string nombre, double[] t, double[] y, Color color)
        {
            var s = new Series(nombre) { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = color };
            if (t != null && y != null)
            {
                int n = Math.Min(t.Length, y.Length);
                for (int i = 0; i < n; i++) s.Points.AddXY(t[i], y[i]);
            }
            return s;
        }

        private static string Fmt(double v) => v.ToString("0.####E+0", CultureInfo.InvariantCulture);

        private void MostrarGsUltima()
        {
            if (_resultados.Count == 0) return;
            var r = _resultados.Last();
            txtGs.Text =
                $"Combinación {r.Indice}:  {r.Parametros}\r\n" +
                "(numerador / denominador en s; Δ = denominador común)\r\n\r\n" +
                "G1(s) = Y1(s)/F(s) =\r\n" + Norm(r.G1) + "\r\n\r\n" +
                "G2(s) = Y2(s)/F(s) =\r\n" + Norm(r.G2) + "\r\n\r\n" +
                "G3(s) = Y3(s)/F(s) =\r\n" + Norm(r.G3) + "\r\n";
        }

        private static string Norm(string s) => s.Replace("\n", "\r\n");

        // ============================================================
        //  ANIMACIÓN
        // ============================================================
        private void CambiarFuenteAnim()
        {
            if (_resultados.Count == 0) return;
            var r = _resultados.Last();
            _animFuente = rbImpulso.Checked ? r.Impulso : r.Paso;
            _frame = 0;
            anim.Reiniciar();
            ReiniciarChartVivo();
            lblTiempo.Text = "t = 0.000 s";
        }

        private void Reproducir()
        {
            if (_animFuente == null) CambiarFuenteAnim();
            if (_animFuente == null) { Aviso("Ejecute primero una simulación."); return; }
            // Si terminó, reinicia el recorrido y la gráfica para volver a dibujarla.
            if (_frame >= _animFuente.T.Length) { _frame = 0; anim.Reiniciar(); ReiniciarChartVivo(); }
            timerAnim.Start();
        }

        private void TimerAnim_Tick(object sender, EventArgs e)
        {
            if (_animFuente == null) { timerAnim.Stop(); return; }
            if (_frame >= _animFuente.T.Length) { timerAnim.Stop(); return; }

            TryNum(txtFactor.Text, out double factor);
            anim.SetFrame(
                _animFuente.Y1[_frame] * factor,
                _animFuente.Y2[_frame] * factor,
                _animFuente.Y3[_frame] * factor);

            // Dibuja el punto actual en la gráfica viva y reajusta los ejes.
            double t = _animFuente.T[_frame];
            chartVivo.Series["y1"].Points.AddXY(t, _animFuente.Y1[_frame]);
            chartVivo.Series["y2"].Points.AddXY(t, _animFuente.Y2[_frame]);
            chartVivo.Series["y3"].Points.AddXY(t, _animFuente.Y3[_frame]);
            try { chartVivo.ChartAreas[0].RecalculateAxesScale(); } catch { /* sin datos aún */ }

            lblTiempo.Text = "t = " + t.ToString("0.000", CultureInfo.InvariantCulture) + " s";
            _frame++;
        }

        // ============================================================
        private DialogResult Aviso(string msg, MessageBoxButtons botones = MessageBoxButtons.OK)
            => MessageBox.Show(msg, "Simulador", botones,
                botones == MessageBoxButtons.OK ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
    }
}
