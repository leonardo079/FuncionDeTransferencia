using System.Collections.Generic;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Genera todas las combinaciones del sistema variando una constante k, una c y una m,
    /// dejando el resto de constantes en valores fijos. N = nk * nc * nm.
    /// </summary>
    public static class GeneradorCombinaciones
    {
        /// <param name="baseParams">Valores fijos de todas las constantes (k1..k4, c1..c4, m1..m3).</param>
        /// <param name="indiceK">Índice (0..3) de la k que se varía.</param>
        /// <param name="valoresK">Valores generados para esa k.</param>
        /// <param name="indiceC">Índice (0..3) de la c que se varía.</param>
        /// <param name="valoresC">Valores generados para esa c.</param>
        /// <param name="indiceM">Índice (0..2) de la m que se varía.</param>
        /// <param name="valoresM">Valores generados para esa m.</param>
        public static List<SistemaParametros> Generar(
            SistemaParametros baseParams,
            int indiceK, double[] valoresK,
            int indiceC, double[] valoresC,
            int indiceM, double[] valoresM)
        {
            var lista = new List<SistemaParametros>(valoresK.Length * valoresC.Length * valoresM.Length);

            foreach (double vk in valoresK)
                foreach (double vc in valoresC)
                    foreach (double vm in valoresM)
                    {
                        var p = baseParams.Clonar();
                        p.K[indiceK] = vk;
                        p.C[indiceC] = vc;
                        p.M[indiceM] = vm;
                        lista.Add(p);
                    }

            return lista;
        }
    }
}
