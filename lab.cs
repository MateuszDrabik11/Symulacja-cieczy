using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
namespace Symulacja_cząsteczek_cieczy;

class Lab
{

    [DllImport("../../../libasm.so", EntryPoint = "kernel_function")]
    extern static void kernel(double[] a, double[] output);
    [DllImport("../../../libasm.so", EntryPoint = "kernel_function_derivative")]
    extern static void kernel_derivative(double[] vec, double length, double[] output);

    [DllImport("../../../libasm.so", EntryPoint = "distance_between_two_points")]
    extern static double lenght(double[] a, double[] b);

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
    public static void calcLenghts()
    {
        Random r = new Random();
        int n = 1000;
        double[,] vectors = new double[n, 4];
        double[] lenghts = new double[n];
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                vectors[i, j] = r.NextDouble();
            }
        }

        int threadCount = Environment.ProcessorCount;
        Thread[] threads = new Thread[threadCount];
        int chunkSize = n / threadCount;
        for (int i = 0; i < threadCount; i++)
        {
            int start = i * chunkSize;
            int end = start + chunkSize;
            threads[i] = new Thread(() => ThreadCalcLenght(vectors, start, end, lenghts));
            threads[i].Start();
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        for (int i = 0; i < n; i++)
        {
            double[] vector = GetRow(vectors, i);
            Console.WriteLine("{0}.length = {1}", printVector(vector), lenghts[i]);
        }
    }
    public static double[] GetRow(double[,] matrix, long rowIndex)
    {
        double[] row = new double[4];
        for (int j = 0; j < 4; j++)
        {
            row[j] = matrix[rowIndex, j];
        }
        return row;
    }
    public static string printVector(double[] vec)
    {
        string res = "[" + vec[0].ToString() + ","
        + vec[1].ToString() + ","
        + vec[2].ToString() + "]";
        return res;
    }
    public static void ThreadCalcLenght(double[,] vectors, int start, int end, double[] lenghts)
    {
        for (int i = start; i < end; i++)
        {
            double[] vector = new double[4];
            for (int j = 0; j < 4; j++)
            {
                vector[j] = vectors[i, j];
            }
            lenghts[i] = Lab.lenght(vector, zero);
        }
    }
}