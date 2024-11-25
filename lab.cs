using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
namespace Symulacja_cząsteczek_cieczy;

class Lab
{

    [DllImport("../../../libc.so", EntryPoint = "kernel")]
    extern static void kernel(ref double lenghts, long chunk, long size, ref double output);
    [DllImport("../../../libc.so", EntryPoint = "kernel_derivative")]
    extern static void kernel_derivative(ref double lenghts, ref double chunk_start, ref double vectors, long chunk, long size, ref double output);

    [DllImport("../../../libc.so", EntryPoint = "lenght")]
    extern static void lenght(ref double start, long count, long size, ref double b, ref double output);
    [DllImport("../../../libc.so", EntryPoint = "calc_density_and_pressure")]
    extern static void calc_density_and_pressure(double[] masses, ref double kernels, long p_index, long number_of_particles, long chunk, double[] out_density, double[] out_pressure);
    [DllImport("../../../libc.so", EntryPoint = "calc_forces")]
    extern static void calc_forces(double[] masses, double[] densities,ref double kernel_derivatives,ref double kernels,ref double velocities,ref double positions,long particles, long start_index,long chunk,ref double accelerations);

    static double[] zero = [0, 0, 0, 0];

    public static void calcLenghtsAvx()
    {
        Random r = new Random();
        int n = 100;
        double[,] vectors = new double[n, 4];
        double[] lenghts = new double[n];
        double[,] kernels = new double[n, n];
        double[,,] kernel_derivatives = new double[n, n, 4];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                vectors[i, j] = r.NextDouble();
            }
        }

        int threadCount = 4;
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                //lenght(ref vectors[localStart, 0], count, ref vectors[0, 0], ref lenghts[localStart]);
                //kernel(ref lenghts[localStart, 0], count, n, ref kernels[localStart, 0]);
                //kernel_derivative(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, n, ref kernel_derivatives[localStart, 0, 0]);
            });
            Console.WriteLine("Thread {0}: {1}", i, count);
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        watch.Stop();
        // for (int i = 0; i < n; i++)
        // {
        //     for (int j = 0; j < n - 1; j++)
        //     {
        //         Console.Write($"{lenghts[i, j],10:0.00}");
        //         Console.Write(" | ");
        //     }
        //     Console.WriteLine($"{lenghts[i, n - 1],10:0.00}");
        // }
        // Console.WriteLine("========================================================");
        // for (int i = 0; i < n; i++)
        // {
        //     for (int j = 0; j < n - 1; j++)
        //     {
        //         Console.Write($"{kernels[i, j],10:0.00}");
        //         Console.Write(" | ");
        //     }
        //     Console.WriteLine($"{kernels[i, n - 1],10:0.00}");
        // }
        // Console.WriteLine("========================================================");
        // for (int i = 0; i < n; i++)
        // {
        //     for (int j = 0; j < n - 1; j++)
        //     {
        //         string formattedArray = $"[{kernel_derivatives[i, j, 0]:0.00}, {kernel_derivatives[i, j, 1]:0.00}, {kernel_derivatives[i, j, 2]:0.00}]";
        //         Console.Write($"{formattedArray,25}");
        //         Console.Write(" | ");
        //     }
        //     Console.WriteLine($"{kernel_derivatives[i, n - 1, 0]:0.00},{kernel_derivatives[i, n - 1, 1]:0.00},{kernel_derivatives[i, n - 1, 2]:0.00}");
        // }
        Console.WriteLine("Execution time: {0} ms, {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);
    }
    private static double vector_lenght(double x, double y, double z)
    {
        return Math.Sqrt(x * x + y * y + z * z);
    }
    public static void densities()
    {
        Random r = new Random();
        int n = 5;
        double[,] vectors = new double[n, 4];
        double[,] lenghts = new double[n, n];
        double[,] kernels = new double[n, n];
        double[,,] kernel_derivatives = new double[n, n, 4];
        double[,] velocities = new double[n,4];
        double[,] accelerations = new double[n,4];
        double[] masses = new double[n];
        for (int i = 0; i < n; i++)
        {
            masses[i] = 100;
        }
        double[] pressures = new double[n];
        double[] densities = new double[n];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                vectors[i, j] = r.NextDouble();
            }
        }

        int threadCount = 4;
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                lenght(ref vectors[localStart, 0], count, n, ref vectors[0, 0], ref lenghts[localStart, 0]);
                kernel(ref lenghts[localStart, 0], count, n, ref kernels[localStart, 0]);
                kernel_derivative(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, n, ref kernel_derivatives[localStart, 0, 0]);
                calc_density_and_pressure(masses, ref kernels[0, 0], localStart, n, count, densities, pressures);
                calc_forces(masses, densities, ref kernel_derivatives[0, 0, 0],ref kernels[0,0],ref velocities[0,0],ref vectors[0,0],n,localStart,count,ref accelerations[0,0]);
            });
            Console.WriteLine("Thread {0}: {1}", i, count);
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        watch.Stop();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n - 1; j++)
            {
                Console.Write($"{lenghts[i, j],10:0.00}");
                Console.Write(" | ");
            }
            Console.WriteLine($"{lenghts[i, n - 1],10:0.00}");
        }
        Console.WriteLine("========================================================");
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n - 1; j++)
            {
                Console.Write($"{kernels[i, j],10:0.00}");
                Console.Write(" | ");
            }
            Console.WriteLine($"{kernels[i, n - 1],10:0.00}");
        }
        Console.WriteLine("========================================================");
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n - 1; j++)
            {
                string formattedArray = $"[{kernel_derivatives[i, j, 0]:0.00}, {kernel_derivatives[i, j, 1]:0.00}, {kernel_derivatives[i, j, 2]:0.00}]";
                Console.Write($"{formattedArray,25}");
                Console.Write(" | ");
            }
            Console.WriteLine($"{kernel_derivatives[i, n - 1, 0]:0.00},{kernel_derivatives[i, n - 1, 1]:0.00},{kernel_derivatives[i, n - 1, 2]:0.00}");
        }
        Console.WriteLine("========================================================");
        for (int i = 0; i < n; i++)
        {
            Console.WriteLine($"{densities[i],10:0.00}");
        }
        Console.WriteLine("========================================================");
        for (int i = 0; i < n; i++)
        {
            Console.WriteLine($"{pressures[i],10:0.00}");
        }
        Console.WriteLine("========================================================");
        for (int i = 0; i < n; i++)
        {
            Console.WriteLine($"[{accelerations[i,0],10:0.00},{accelerations[i,1],10:0.00},{accelerations[i,2],10:0.00}]");
        }
        Console.WriteLine("Execution time: {0} ms, {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);
    }
}
