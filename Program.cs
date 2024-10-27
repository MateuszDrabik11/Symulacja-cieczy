using Avalonia;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Symulacja_cząsteczek_cieczy;

record Particle
{
    public double[] position = new double[3];
    public double[] velocity = new double[3];
    public double[] acceleration = new double[3];
    public double pressure;
    public double density;
    public double volume;
    public double mass;
}

class Program
{
    [DllImport("../../../libasm.so", EntryPoint = "kernel_function")]
    extern static void kernel(double[] a, double[] output);
    [DllImport("../../../libasm.so", EntryPoint = "kernel_function_derivative")]
    extern static void kernel_derivative(double[] vec, double length, double[] output);

    [DllImport("../../../libasm.so", EntryPoint = "distance_between_two_points")]
    extern static double lenght(double[] a, double[] b);

    [DllImport("../../../libasm.so", EntryPoint = "increment_array")]
    extern static int add1(ref int a, int size);

    public static void parallel_loop()
    {
        int[] ints = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

        const int numberOfThreads = 4;
        int chunkSize = ints.Length / numberOfThreads;

        Thread[] threads = new Thread[numberOfThreads];
        for (int i = 0; i < numberOfThreads; ++i)
        {
            int t = i;
            threads[i] = new Thread(() => add1(ref ints[t * chunkSize], 3));
            threads[i].Start();
        }
        foreach (var i in ints)
        {
            Console.WriteLine(i);
        }
    }
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
        parallel_loop();
        double[] a = [1, 1, 1];
        double[] b = [2, 32, 1];
        Console.WriteLine(lenght(a, b));
        double[] c = [0.001, 0.2, 1.0, 0.8];
        double[] t = new double[4];
        kernel(c, t);
        foreach (double T in t)
        {
            Console.WriteLine(T);
        }
        Console.WriteLine("kernel derivative");
        double[] vec = [0, 0.01, 0, 0];
        double[] zero = [0, 0, 0, 0];
        double vec_length = lenght(vec, zero);
        double[] vec_derivative = [0, 0, 0, 0];
        kernel_derivative(vec, vec_length, vec_derivative);
        foreach (var item in vec_derivative)
        {
            Console.WriteLine(item);
        }

    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
