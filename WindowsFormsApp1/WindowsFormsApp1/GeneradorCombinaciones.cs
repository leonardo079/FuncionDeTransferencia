using System.Collections.Generic;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Genera todas las combinaciones del sistema variando una constante k, una b y una m,
    /// dejando el resto de constantes en valores fijos. N = nk * nb * nm.
    /// </summary>
    public static class GeneradorCombinaciones
    {
        /// <param name="baseParams">Valores fijos de todas las constantes (k1..k4, b1..b4, m1..m3).</param>
        /// <param name="indiceK">Índice (0..3) de la k que se varía.</param>
        /// <param name="valoresK">Valores generados para esa k.</param>
        /// <param name="indiceB">Índice (0..3) de la b que se varía.</param>
        /// <param name="valoresB">Valores generados para esa b.</param>
        /// <param name="indiceM">Índice (0..2) de la m que se varía.</param>
        /// <param name="valoresM">Valores generados para esa m.</param>
        public static List<SistemaParametros> Generar(
            SistemaParametros baseParams,
            int indiceK, double[] valoresK,
            int indiceB, double[] valoresB,
            int indiceM, double[] valoresM)
        {
            var lista = new List<SistemaParametros>(valoresK.Length * valoresB.Length * valoresM.Length);

            foreach (double vk in valoresK)
                foreach (double vb in valoresB)
                    foreach (double vm in valoresM)
                    {
                        var p = baseParams.Clonar();
                        p.K[indiceK] = vk;
                        p.B[indiceB] = vb;
                        p.M[indiceM] = vm;
                        lista.Add(p);
                    }

            return lista;
        }
    }
}
