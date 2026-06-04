# Simulador masa–resorte–amortiguador de 3 masas

Taller Final de Simulación de Computadores (UPTC). Simula un sistema mecánico **vertical** de
3 masas, 4 resortes y 4 amortiguadores, con la fuerza externa **F(t) aplicada sobre m3**.

## Tecnología
- **Visual Studio · Windows Forms · .NET Framework 4.7.2**
- **Octave.NET** (NuGet) como puente con **GNU Octave** (paquete `control`)
- Gráficas con `System.Windows.Forms.DataVisualization.Charting`

## Requisitos
1. **GNU Octave** instalado. En este equipo está en:
   `C:\Program Files\GNU Octave\Octave-11.1.0\mingw64\bin\octave-cli.exe`
2. Visual Studio 2022/2025 (o MSBuild) con .NET Framework 4.7.2.

## Compilar
Abrir `WindowsFormsApp1/WindowsFormsApp1.slnx` en Visual Studio y compilar (NuGet se restaura
automáticamente). O por consola:

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msb "WindowsFormsApp1\WindowsFormsApp1\WindowsFormsApp1.csproj" /t:Restore
& $msb "WindowsFormsApp1\WindowsFormsApp1\WindowsFormsApp1.csproj" /t:Build /p:Configuration=Debug
```

El ejecutable queda en `WindowsFormsApp1\WindowsFormsApp1\bin\Debug\WindowsFormsApp1.exe`.

## Uso
1. En **«Ruta del CLI de Octave»** pegar la ruta de `octave-cli.exe` (o `octave-cli` si está en PATH).
2. Fijar los valores de `k1..k4`, `c1..c4`, `m1..m3` y elegir **cuál** k, c y m se varían (mín/máx/paso).
3. **Calcular combinaciones** → muestra N = n_k·n_c·n_m y el total de simulaciones (2N: paso + impulso).
4. **Simular** → genera por iteración las gráficas de y1,y2,y3 al paso e impulso, métricas y tiempos.
5. Pestaña **Animación** → reproduce la última iteración (paso o impulso), con pausa/reinicio y tiempo.

## Estructura del código
| Archivo | Rol |
|---|---|
| `Modelos.cs` | Clases de datos (rangos, parámetros, respuestas, resultados). |
| `GeneradorCombinaciones.cs` | Producto cartesiano de los rangos. |
| `Polinomio.cs` | Construye G1, G2, G3 por Cramer para mostrarlas. |
| `MotorOctave.cs` | Arma el modelo en espacio de estados y obtiene step/impulse en Octave. |
| `ControlAnimacion.cs` | Dibujo y animación del sistema. |
| `Form1.cs` | Interfaz, validaciones, orquestación. |

## Informe
`Informe/Informe.html` — desarrollo matemático y funciones de transferencia. Abrir en el navegador
e **Imprimir → Guardar como PDF**.

## Modelo
Funciones de transferencia (fuerza en m3). Sustituciones para despejar por Cramer:
`a1=m1s²+(c1+c2)s+(k1+k2)`, `a2=c2s+k2`, `a3=m2s²+(c2+c3)s+(k2+k3)`,
`a4=c3s+k3`, `a5=m3s²+(c3+c4)s+(k3+k4)`, y denominador común
`Δ = a5(a1a3 − a2²) − a1a4²`:

- `G1(s) = a2·a4 / Δ`
- `G2(s) = a1·a4 / Δ`
- `G3(s) = (a1·a3 − a2²) / Δ`
