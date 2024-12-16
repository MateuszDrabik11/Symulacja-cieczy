using Avalonia;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        run(1000,1);
        run(1000,2);
        run(1000,4);
        run(1000,8);
        run(1000,16);
        run(1000,32);
        
    }
    public static void run(long particles,long threads)
    {
        c_solver solver = new c_solver(particles,threads);
        double[,] pos = solver.GetParticlePosition();
        double[] pres = solver.GetPressure();
        Stopwatch s1 = new Stopwatch();
        s1.Start();
        for (int i = 0; i < 100; i++)
        {
            solver.Step();
        }
        s1.Stop();
        asm_solver solver1 = new asm_solver(particles,threads);
        double[,] pos1 = solver1.GetParticlePosition();
        double[] pres1 = solver1.GetPressure();
        Stopwatch s2 = new Stopwatch();
        s2.Start();
        for (int i = 0; i < 100; i++)
        {
            solver1.Step();
        }
        s2.Stop();
        GC.KeepAlive(pres);
        GC.KeepAlive(pres1);

        Console.WriteLine($"c:{s1.ElapsedMilliseconds} ms, asm: {s2.ElapsedMilliseconds} ms");
        Console.WriteLine($"N: {particles}, Threads: {threads}");
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
