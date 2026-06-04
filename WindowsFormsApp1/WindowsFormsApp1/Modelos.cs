using System;
using System.Linq;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Rango de valores (límite inferior, superior y paso) para una constante a variar.
    /// </summary>
    public class RangoParametro
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Paso { get; set; }

        /// <summary>
        /// Genera los valores del rango incluyendo el extremo superior.
        /// Ej: Min=1, Max=2, Paso=0.2 -> {1, 1.2, 1.4, 1.6, 1.8, 2}.
        /// </summary>
        public double[] Generar()
        {
            if (Paso <= 0) throw new ArgumentException("El paso debe ser mayor que cero.");
            if (Max < Min) throw new ArgumentException("El límite superior no puede ser menor que el inferior.");

            // Tolerancia para incluir el extremo superior pese a errores de punto flotante.
            int n = (int)Math.Floor((Max - Min) / Paso + 1e-9) + 1;
            var valores = new double[n];
            for (int i = 0; i < n; i++)
            {
                // Se redondea para evitar arrastre de error (1.2000000000001 -> 1.2).
                valores[i] = Math.Round(Min + i * Paso, 10);
            }
            return valores;
        }
    }

    /// <summary>
    /// Conjunto completo de parámetros del sistema (k1..k4, b1..b4, m1..m3) para una iteración.
    /// </summary>
    public class SistemaParametros
    {
        public double[] K { get; set; } = new double[4]; // k1, k2, k3, k4
        public double[] B { get; set; } = new double[4]; // b1, b2, b3, b4 (amortiguamiento)
        public double[] M { get; set; } = new double[3]; // m1, m2, m3

        public SistemaParametros Clonar()
        {
            return new SistemaParametros
            {
                K = (double[])K.Clone(),
                B = (double[])B.Clone(),
                M = (double[])M.Clone()
            };
        }

        public override string ToString()
        {
            string k = string.Join(", ", K.Select((v, i) => $"k{i + 1}={v:0.###}"));
            string b = string.Join(", ", B.Select((v, i) => $"b{i + 1}={v:0.###}"));
            string m = string.Join(", ", M.Select((v, i) => $"m{i + 1}={v:0.###}"));
            return $"{k} | {b} | {m}";
        }
    }

    /// <summary>
    /// Respuesta dinámica (al paso o al impulso) con los tres desplazamientos verticales y1, y2, y3.
    /// </summary>
    public class RespuestaDinamica
    {
        public double[] T { get; set; }   // vector temporal
        public double[] Y1 { get; set; }  // desplazamiento masa 1
        public double[] Y2 { get; set; }  // desplazamiento masa 2
        public double[] Y3 { get; set; }  // desplazamiento masa 3
        public long Milisegundos { get; set; } // tiempo de simulación de esta respuesta

        /// <summary>Máxima elongación (desplazamiento absoluto) de la masa indicada (0,1,2).</summary>
        public double MaxAbs(int masa)
        {
            double[] y = masa == 0 ? Y1 : masa == 1 ? Y2 : Y3;
            if (y == null || y.Length == 0) return 0;
            return y.Max(v => Math.Abs(v));
        }
    }

    /// <summary>
    /// Resultado completo de una iteración: parámetros usados, respuestas y funciones de transferencia.
    /// </summary>
    public class ResultadoIteracion
    {
        public int Indice { get; set; }
        public SistemaParametros Parametros { get; set; }
        public RespuestaDinamica Paso { get; set; }
        public RespuestaDinamica Impulso { get; set; }

        // Funciones de transferencia (texto) para esta combinación.
        public string G1 { get; set; }
        public string G2 { get; set; }
        public string G3 { get; set; }
    }
}
