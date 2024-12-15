using Avalonia;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Symulacja_cząsteczek_cieczy;

// record Particle
// {
//     public double[] position = new double[3];
//     public double[] velocity = new double[3];
//     public double[] acceleration = new double[3];
//     public double pressure;
//     public double density;
//     public double volume;
//     public double mass;
// }

//1000 elements, 4 t
//avx - 49 ms, 49524382 ticks, 56 ms, 56250020 ticks, 104 ms, 104077437 ticks
//asm - 51 ms, 51407796 ticks, 69 ms, 69765497 ticks, 29 ms, 29753280 ticks
//c   - 55 ms, 55386917 ticks, 52 ms, 52063571 ticks, 66 ms, 66893889 ticks

//1000 elements, 1 t
//avx - 30 ms, 30732128 ticks
//asm - 25 ms, 25048458 ticks
//c   - 45 ms, 45996242 ticks

//100000000 elements, 1 t
//avx - 3295 ms, 3295304853 ticks
//asm - 5577 ms, 5577760285 ticks
//c   - 7139 ms, 7139345722 ticks

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
        // Tests.Tests test = new Tests.Tests();
        // Console.WriteLine("Lenght test: {0}",test.TestLenght());
        // Console.WriteLine("Kernel test: {0}",test.TestKernel());
        // Console.WriteLine("Kernel derivative test: {0}",test.TestKernelDerivative());
        // Console.WriteLine("Pressure calculation test: {0}",test.TestPressureCalc());
        // Console.WriteLine("Force calculation test: {0}",test.TestForceCalc());
        c_solver solver = new c_solver();
        double[,] pos = solver.GetParticlePosition();
        for (long j = 0; j < pos.GetLength(0); j++)
        {
            Console.WriteLine($"[{pos[j, 0],10:0.0000000},{pos[j, 1],10:0.0000000},{pos[j, 2],10:0.0000000}]");
        }
        for (int i = 0; i < 100; i++)
        {
            solver.Step();
            //pos = solver.GetParticlePosition();
            // for (long j = 0; j < pos.GetLength(0); j++)
            // {
            //     Console.WriteLine($"[{pos[j, 0],10:0.0000000},{pos[j, 1],10:0.0000000},{pos[j, 2],10:0.0000000}]");
            // }
            Console.WriteLine("step {0}", i);
        }
        //Lab.densities();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
