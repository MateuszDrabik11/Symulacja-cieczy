using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
namespace Symulacja_cząsteczek_cieczy;

class Lab
{

    [DllImport("../../../libc.so", EntryPoint = "kernel")]
    extern static void kernel(ref double lenghts, long chunk, long size, ref double output);
    [DllImport("../../../libc.so", EntryPoint = "kernel_derivative")]
    extern static void kernel_derivative(ref double lenghts, ref double chunk_start, ref double vectors, long chunk, long size, ref double output);

    [DllImport("../../../libasm.so", EntryPoint = "lenght_no_avx")]
    extern static void lenght(ref double start, long count, ref double b, ref double output);

    [DllImport("../../../libasm.so", EntryPoint = "increment_array")]
    extern static int add1(ref int a, int size);

    static double[] zero = [0, 0, 0, 0];
    public static void parallel_loop()
    {
        int[] ints = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

        const int numberOfThreads = 1;
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
    public static void calcLenghtsAvx()
    {
        Random r = new Random();
        int n = 1000000000;
        double[,] vectors = new double[n, 4];
        double[] lenghts = new double[n];
        //double[,] kernels = new double[n, n];
        //double[,,] kernel_derivatives = new double[n, n, 4];
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
                lenght(ref vectors[localStart, 0], count, ref vectors[0, 0], ref lenghts[localStart]);
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
}
