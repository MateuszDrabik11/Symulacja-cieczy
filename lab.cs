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
    extern static void calc_forces(double[] masses, double[] densities, ref double kernel_derivatives, ref double kernels, ref double velocities, ref double positions, long particles, long start_index, long chunk, ref double accelerations);

    [DllImport("../../../libc.so", EntryPoint = "time_integration")]
    extern static void time_integration(ref double positions, ref double velocities, ref double accelerations, double dt, long start_index, long chunk);
    [DllImport("../../../libc.so", EntryPoint = "add_external_force")]
    extern static void external_force(ref double accelerations, ref double forces, long start_index, long chunk);
    [DllImport("../../../libc.so", EntryPoint = "gravity")]
    extern static void apply_gravity(ref double accelerations, double g, long start_index, long chunk);
    [DllImport("../../../libc.so", EntryPoint = "boundries")]
    extern static void boundries(ref double positions, ref double velocities, long start_index, long chunk, double x_max, double y_max, double z_max, double bouncines, double dt);

    public static void lenghtT(double[,] vectors, double[,] lenghts, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                lenght(ref vectors[localStart, 0], count, n, ref vectors[0, 0], ref lenghts[localStart, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    //             kernel(ref lenghts[localStart, 0], count, n, ref kernels[localStart, 0]);
    //             kernel_derivative(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, n, ref kernel_derivatives[localStart, 0, 0]);
    //             calc_density_and_pressure(masses, ref kernels[0, 0], localStart, n, count, densities, pressures);
    //             calc_forces(masses, densities, ref kernel_derivatives[0, 0, 0], ref kernels[0, 0], ref velocities[0, 0], ref vectors[0, 0], n, localStart, count, ref accelerations[0, 0]);
    //             apply_gravity(ref accelerations[0,0],10,localStart,count);
    //             time_integration(ref vectors[0, 0], ref velocities[0, 0], ref accelerations[0, 0], 0.1, localStart, count);
    //             boundries(ref vectors[0,0],ref velocities[0,0],localStart,count,10,10,10,0.6,0.1);
    public static void boundriesT(double[,] vectors, double[,] velocities, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                boundries(ref vectors[0, 0], ref velocities[0, 0], localStart, count, 1, 1, 1, 0.6, 0.1);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void kernelT(double[,] lenghts, double[,] kernels, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                kernel(ref lenghts[localStart, 0], count, n, ref kernels[localStart, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void kernel_derivativesT(double[,] lenghts, double[,] vectors, double[,,] kernel_derivatives, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                kernel_derivative(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, n, ref kernel_derivatives[localStart, 0, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void densityT(double[] masses, double[,] kernels, double[] pressures, double[] densities, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                calc_density_and_pressure(masses, ref kernels[0, 0], localStart, n, count, densities, pressures);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void forcesT(double[] masses, double[,,] kernel_derivatives, double[,] kernels, double[,] velocities, double[,] vectors, double[] densities, double[,] accelerations, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                calc_forces(masses, densities, ref kernel_derivatives[0, 0, 0], ref kernels[0, 0], ref velocities[0, 0], ref vectors[0, 0], n, localStart, count, ref accelerations[0, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void gravityT(double[,] accelerations, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                apply_gravity(ref accelerations[0, 0], 10, localStart, count);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void integrationT(double[,] vectors, double[,] velocities, double[,] accelerations, int n, int threadCount)
    {
        Thread[] threads = new Thread[threadCount];
        int chunk = n / threadCount;
        int rest = n % threadCount;
        int start = 0;
        for (int i = 0; i < threadCount; i++)
        {
            int count = chunk + (i < rest ? 1 : 0);
            int localStart = start;
            threads[i] = new Thread(() =>
            {
                time_integration(ref vectors[0, 0], ref velocities[0, 0], ref accelerations[0, 0], 0.1, localStart, count);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }
        return;
    }
    public static void densities()
    {
        Random r = new Random();
        int n = 100;
        int threadCount = 4;
        double[,] vectors = new double[n, 4];
        double[,] lenghts = new double[n, n];
        double[,] kernels = new double[n, n];
        double[,,] kernel_derivatives = new double[n, n, 4];
        double[,] velocities = new double[n, 4];
        double[,] accelerations = new double[n, 4];
        double[] masses = new double[n];
        for (int i = 0; i < n; i++)
        {
            masses[i] = 3.33;
        }
        double[] pressures = new double[n];
        double[] densities = new double[n];
        for (int i = 0; i < n; ++i)
        {
            vectors[i, 0] = r.NextDouble();
            vectors[i, 1] = r.NextDouble();
            vectors[i, 2] = 1-0.000001;
        }

        Stopwatch watch = Stopwatch.StartNew();
        for (int j = 0; j < 1000; j++)
        {
            lenghtT(vectors, lenghts, n, threadCount);
            //
            kernelT(lenghts, kernels, n, threadCount);
            kernel_derivativesT(lenghts, vectors, kernel_derivatives, n, threadCount);
            //
            densityT(masses, kernels, pressures, densities, n, threadCount);
            //
            forcesT(masses, kernel_derivatives, kernels, velocities, vectors, densities, accelerations, n, threadCount);
            //
            gravityT(accelerations, n, threadCount);
            //
            integrationT(vectors, velocities, accelerations, n, threadCount);
            //
            boundriesT(vectors, velocities, n, threadCount);

            Console.WriteLine("========================================================");
            Console.WriteLine("frame: {0}",j);
            for (int i = 0; i < n; i++)
            {
                Console.WriteLine($"[{vectors[i, 0],10:0.00},{vectors[i, 1],10:0.00},{vectors[i, 2],10:0.00}]");
            }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     for (int z = 0; z < n - 1; z++)
            //     {
            //         Console.Write($"{kernels[i, z],10:0.00}");
            //         Console.Write(" | ");
            //     }
            //     Console.WriteLine($"{kernels[i, n - 1],10:0.00}");
            // }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     Console.WriteLine($"{densities[i],10:0.00}");
            // }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     Console.WriteLine($"{pressures[i],10:0.00}");
            // }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     for (int z = 0; z < n - 1; z++)
            //     {
            //         string formattedArray = $"[{kernel_derivatives[i, z, 0]:0.00}, {kernel_derivatives[i, z, 1]:0.00}, {kernel_derivatives[i, z, 2]:0.00}]";
            //         Console.Write($"{formattedArray,25}");
            //         Console.Write(" | ");
            //     }
            //     Console.WriteLine($"{kernel_derivatives[i, n - 1, 0]:0.00},{kernel_derivatives[i, n - 1, 1]:0.00},{kernel_derivatives[i, n - 1, 2]:0.00}");
            // }
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
        // Console.WriteLine("========================================================");
        // for (int i = 0; i < n; i++)
        // {
        //     Console.WriteLine($"{densities[i],10:0.00}");
        // }
        // Console.WriteLine("========================================================");
        // for (int i = 0; i < n; i++)
        // {
        //     Console.WriteLine($"{pressures[i],10:0.00}");
        // }
        // Console.WriteLine("========================================================");
        // for (int i = 0; i < n; i++)
        // {
        //     Console.WriteLine($"[{accelerations[i,0],10:0.00},{accelerations[i,1],10:0.00},{accelerations[i,2],10:0.00}]");
        // }
        // Console.WriteLine("========================================================");
        // for (int i = 0; i < n; i++)
        // {
        //     Console.WriteLine($"[{velocities[i, 0],10:0.00},{velocities[i, 1],10:0.00},{velocities[i, 2],10:0.00}]");
        // }
        Console.WriteLine("Execution time: {0} ms, {1} ticks", watch.ElapsedMilliseconds, watch.ElapsedTicks);
    }
    public static void fast()
    {
        int n = 10;
        int threadCount = 4;
        double[,] vectors = new double[n, 4];
        double[,] lenghts = new double[n, n];
        double[,] kernels = new double[n, n];
        double[,,] kernel_derivatives = new double[n, n, 4];
        double[,] velocities = new double[n, 4];
        double[,] accelerations = new double[n, 4];
        double[] masses = new double[n];
        for (int i = 0; i < n; i++)
        {
            masses[i] = 100;
        }
        double a = 0;
        double[] pressures = new double[n];
        double[] densities = new double[n];
        for (int i = 0; i < n; ++i)
        {
            vectors[i, 0] = a;
            vectors[i, 1] = a;
            vectors[i, 2] = 10;
            a += 0.5;
        }

        Stopwatch watch = Stopwatch.StartNew();
        for (int j = 0; j < 1000; j++)
        {
            Thread[] threads = new Thread[threadCount];
            int chunk = n / threadCount;
            int rest = n % threadCount;
            int start = 0;
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
                    calc_forces(masses, densities, ref kernel_derivatives[0, 0, 0], ref kernels[0, 0], ref velocities[0, 0], ref vectors[0, 0], n, localStart, count, ref accelerations[0, 0]);
                    apply_gravity(ref accelerations[0, 0], 10, localStart, count);
                    time_integration(ref vectors[0, 0], ref velocities[0, 0], ref accelerations[0, 0], 0.1, localStart, count);
                    boundries(ref vectors[0, 0], ref velocities[0, 0], localStart, count, 10, 10, 10, 0.6, 0.1);
                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }

            Console.WriteLine("========================================================");
            Console.WriteLine("frame: {0}",j);
            for (int i = 0; i < n; i++)
            {
                Console.WriteLine($"[{vectors[i, 0],10:0.00},{vectors[i, 1],10:0.00},{vectors[i, 2],10:0.00}]");
            }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     for (int z = 0; z < n - 1; z++)
            //     {
            //         Console.Write($"{kernels[i, z],10:0.00}");
            //         Console.Write(" | ");
            //     }
            //     Console.WriteLine($"{kernels[i, n - 1],10:0.00}");
            // }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     Console.WriteLine($"{densities[i],10:0.00}");
            // }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     Console.WriteLine($"{pressures[i],10:0.00}");
            // }
            // Console.WriteLine("========================================================");
            // for (int i = 0; i < n; i++)
            // {
            //     for (int z = 0; z < n - 1; z++)
            //     {
            //         string formattedArray = $"[{kernel_derivatives[i, z, 0]:0.00}, {kernel_derivatives[i, z, 1]:0.00}, {kernel_derivatives[i, z, 2]:0.00}]";
            //         Console.Write($"{formattedArray,25}");
            //         Console.Write(" | ");
            //     }
            //     Console.WriteLine($"{kernel_derivatives[i, n - 1, 0]:0.00},{kernel_derivatives[i, n - 1, 1]:0.00},{kernel_derivatives[i, n - 1, 2]:0.00}");
            // }
        }
        watch.Stop();
    }
}
