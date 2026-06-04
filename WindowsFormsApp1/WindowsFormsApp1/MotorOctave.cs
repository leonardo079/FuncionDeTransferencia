using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Octave.NET;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Puente con GNU Octave (vía Octave.NET). Por cada combinación construye el modelo en
    /// espacio de estados y obtiene las respuestas al paso y al impulso de y1, y2, y3.
    /// La fuerza F(t) se aplica sobre la masa 3.
    /// </summary>
    public class MotorOctave
    {
        public MotorOctave(string rutaCli)
        {
            // La ruta del octave-cli.exe se configura globalmente antes de crear contextos.
            OctaveContext.OctaveSettings.OctaveCliPath = rutaCli;
        }

        private static string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>
        /// Simula una combinación y devuelve la respuesta al paso y al impulso.
        /// </summary>
        public void Simular(SistemaParametros p, int nElementos,
            out RespuestaDinamica paso, out RespuestaDinamica impulso)
        {
            using (var oct = new OctaveContext())
            {
                // Definición numérica del modelo en espacio de estados.
                var sb = new StringBuilder();
                sb.Append("clear all;");
                sb.Append("pkg load control;");
                // Formato exponencial por elemento (sin factor de escala común) y sin
                // ajuste de líneas: garantiza que los vectores se impriman parseables.
                sb.Append("format long e;");
                sb.Append("split_long_rows(0);");
                sb.Append($"m1={F(p.M[0])};m2={F(p.M[1])};m3={F(p.M[2])};");
                sb.Append($"k1={F(p.K[0])};k2={F(p.K[1])};k3={F(p.K[2])};k4={F(p.K[3])};");
                sb.Append($"c1={F(p.C[0])};c2={F(p.C[1])};c3={F(p.C[2])};c4={F(p.C[3])};");
                sb.Append("M=diag([m1 m2 m3]);");
                sb.Append("K=[k1+k2 -k2 0; -k2 k2+k3 -k3; 0 -k3 k3+k4];");
                sb.Append("C=[c1+c2 -c2 0; -c2 c2+c3 -c3; 0 -c3 c3+c4];");
                sb.Append("A=[zeros(3) eye(3); -M\\K -M\\C];");
                sb.Append("B=[zeros(3,1); M\\[0;0;1]];"); // fuerza en m3
                sb.Append("Cm=[eye(3) zeros(3)];");
                sb.Append("D=zeros(3,1);");
                sb.Append("sys=ss(A,B,Cm,D);");
                oct.Execute(sb.ToString());

                paso = Resolver(oct, "step", nElementos);
                impulso = Resolver(oct, "impulse", nElementos);
            }
        }

        /// <summary>
        /// Ejecuta step/impulse dejando que Octave elija el tiempo final y luego remuestrea
        /// con nElementos puntos. Cronometra la simulación de esta respuesta.
        /// </summary>
        private RespuestaDinamica Resolver(OctaveContext oct, string funcion, int nElementos)
        {
            var reloj = Stopwatch.StartNew();

            // Tiempo final automático segun la dinamica del sistema.
            oct.Execute($"[yy,tt]={funcion}(sys);");
            oct.Execute("tfin=tt(end);");
            // Remuestreo uniforme con el numero de elementos solicitado.
            oct.Execute($"[y,t]={funcion}(sys,tfin,tfin/{nElementos});");

            var r = new RespuestaDinamica
            {
                // Se transponen a vectores fila para que AsVector() los parsee por espacios.
                T = oct.Execute("t'").AsVector(),
                Y1 = oct.Execute("y(:,1)'").AsVector(),
                Y2 = oct.Execute("y(:,2)'").AsVector(),
                Y3 = oct.Execute("y(:,3)'").AsVector()
            };

            reloj.Stop();
            r.Milisegundos = reloj.ElapsedMilliseconds;
            return r;
        }
    }
}
