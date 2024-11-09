using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
namespace Symulacja_cząsteczek_cieczy;

class Lab
{

    [DllImport("../../../libasm.so", EntryPoint = "kernel_function")]
    extern static void kernel(double[] a, double[] output);
    [DllImport("../../../libasm.so", EntryPoint = "kernel_function_derivative")]
    extern static void kernel_derivative(double[] vec, double length, double[] output);

    [DllImport("../../../libc.so", EntryPoint = "lenght")]

    //[DllImport("../../../libasm.so", EntryPoint = "distance_between_two_points")]
    extern static void lenght(ref double start, long count, double[] b,ref double output);

    [DllImport("../../../libasm.so", EntryPoint = "increment_array")]
    extern static int add1(ref int a, int size);

    static double[] zero = [0, 0, 0, 0];
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
    public static void calcLenghtsAvx()
    {
        Random r = new Random();
        int n = 100000000;
        double[,] vectors = new double[n, 4];
        double[] lenghts = new double[n];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                vectors[i, j] = r.NextDouble();
            }
        }

        int threadCount = 2;
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() => lenght(ref vectors[localStart,0], count, zero, ref lenghts[localStart]));
            Console.WriteLine("Thread {0}: {1}",i,count);
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
            //Console.WriteLine("{0} - |{1},{2},{3}| = {4}", i,vectors[i,0],vectors[i,1],vectors[i,2], lenghts[i]);
        }
        for(int i = 0; i<n; i++)
        {
            //Console.WriteLine("{0} - {1}",i,lenghts[i]==vector_lenght(vectors[i,0],vectors[i,1],vectors[i,2]));
        }
        Console.WriteLine("Execution time: {0} ms, {1} ticks",watch.ElapsedMilliseconds, watch.ElapsedTicks);
    }
    private static double vector_lenght(double x, double y, double z)
    {
        return Math.Sqrt(x*x+y*y+z*z);
    }
}
