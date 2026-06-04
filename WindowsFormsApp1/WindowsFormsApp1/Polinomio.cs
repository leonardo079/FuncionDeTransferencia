using System;
using System.Globalization;
using System.Text;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Polinomio en s representado por sus coeficientes: c[i] acompaña a s^i.
    /// Se usa para construir simbólicamente las funciones de transferencia G1, G2, G3
    /// a partir de los parámetros numéricos del sistema.
    /// </summary>
    public class Polinomio
    {
        public double[] Coef; // Coef[i] -> s^i

        public Polinomio(params double[] coef)
        {
            Coef = coef.Length == 0 ? new double[] { 0 } : coef;
        }

        public int Grado => Coef.Length - 1;

        public static Polinomio operator +(Polinomio a, Polinomio b)
        {
            int n = Math.Max(a.Coef.Length, b.Coef.Length);
            var r = new double[n];
            for (int i = 0; i < n; i++)
                r[i] = (i < a.Coef.Length ? a.Coef[i] : 0) + (i < b.Coef.Length ? b.Coef[i] : 0);
            return new Polinomio(r);
        }

        public static Polinomio operator -(Polinomio a, Polinomio b)
        {
            int n = Math.Max(a.Coef.Length, b.Coef.Length);
            var r = new double[n];
            for (int i = 0; i < n; i++)
                r[i] = (i < a.Coef.Length ? a.Coef[i] : 0) - (i < b.Coef.Length ? b.Coef[i] : 0);
            return new Polinomio(r);
        }

        public static Polinomio operator *(Polinomio a, Polinomio b)
        {
            var r = new double[a.Coef.Length + b.Coef.Length - 1];
            for (int i = 0; i < a.Coef.Length; i++)
                for (int j = 0; j < b.Coef.Length; j++)
                    r[i + j] += a.Coef[i] * b.Coef[j];
            return new Polinomio(r);
        }

        /// <summary>Representación legible: "a*s^2 + b*s + c" (potencias descendentes).</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = Coef.Length - 1; i >= 0; i--)
            {
                double v = Coef[i];
                if (Math.Abs(v) < 1e-12) continue;

                string signo = v < 0 ? " - " : (sb.Length > 0 ? " + " : "");
                if (sb.Length == 0 && v < 0) signo = "-";

                string val = Math.Abs(v).ToString("0.####", CultureInfo.InvariantCulture);
                string term;
                if (i == 0) term = val;
                else if (i == 1) term = $"{val}s";
                else term = $"{val}s^{i}";

                sb.Append(signo).Append(term);
            }
            return sb.Length == 0 ? "0" : sb.ToString();
        }
    }

    /// <summary>
    /// Construye las funciones de transferencia G1, G2, G3 = Yi(s)/F(s) del sistema de 3 masas
    /// con la fuerza aplicada sobre m3 (mismo desarrollo del informe). bi = amortiguamiento.
    /// </summary>
    public static class FuncionesTransferencia
    {
        public static void Calcular(SistemaParametros p,
            out string g1, out string g2, out string g3, out Polinomio denominador)
        {
            double k1 = p.K[0], k2 = p.K[1], k3 = p.K[2], k4 = p.K[3];
            double b1 = p.B[0], b2 = p.B[1], b3 = p.B[2], b4 = p.B[3];
            double m1 = p.M[0], m2 = p.M[1], m3 = p.M[2];

            // Sustituciones a1..a5 del informe (Coef[i] -> s^i).
            var A11 = new Polinomio(k1 + k2, b1 + b2, m1);  // a1
            var A22 = new Polinomio(k2 + k3, b2 + b3, m2);  // a3
            var A33 = new Polinomio(k3 + k4, b3 + b4, m3);  // a5
            var A12 = new Polinomio(-k2, -b2);          // -a2 = -(b2 s + k2)
            var A23 = new Polinomio(-k3, -b3);          // -a4 = -(b3 s + k3)

            // Δ = A11*A22*A33 - A11*A23^2 - A12^2*A33 = a5(a1a3 - a2^2) - a1 a4^2
            var delta = A11 * A22 * A33 - A11 * (A23 * A23) - (A12 * A12) * A33;

            // Numeradores (fuerza en m3):
            // G1 = (b2 s+k2)(b3 s+k3) / Δ = (A12*A23) / Δ = a2 a4 / Δ
            var num1 = A12 * A23;
            // G2 = A11*(b3 s+k3) / Δ = a1 a4 / Δ
            var num2 = A11 * new Polinomio(k3, b3);
            // G3 = (A11*A22 - A12^2) / Δ = (a1 a3 - a2^2) / Δ
            var num3 = A11 * A22 - A12 * A12;

            denominador = delta;
            g1 = Fraccion(num1, delta);
            g2 = Fraccion(num2, delta);
            g3 = Fraccion(num3, delta);
        }

        private static string Fraccion(Polinomio num, Polinomio den)
        {
            string n = num.ToString();
            string d = den.ToString();
            int ancho = Math.Max(n.Length, d.Length);
            string linea = new string('-', ancho);
            return $"  {Centrar(n, ancho)}\n  {linea}\n  {Centrar(d, ancho)}";
        }

        private static string Centrar(string s, int ancho)
        {
            if (s.Length >= ancho) return s;
            int izq = (ancho - s.Length) / 2;
            return new string(' ', izq) + s;
        }
    }
}
