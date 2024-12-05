using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tests
{
    public class Tests
    {
        //      c library
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

        //      asm library

        [DllImport("../../../libasm.so", EntryPoint = "lenght")]
        extern static void lenghtAsm(ref double start, long count, long size, ref double b, ref double output);

        [DllImport("../../../libasm.so", EntryPoint = "kernel")]
        extern static void kernelAsm(ref double lenghts, long chunk, long size, ref double output);

        private const int threadCount = 1;
        private const int n = 10;

        private Double[,] vectors;
        private Double[,] lenghts = new double[n, n];
        private Double[,] lenghtsAsm = new double[n, n];
        private Double[,] kernels = new double[n, n];
        private Double[,] kernelsAsm = new double[n, n];
        public Tests()
        {
            Random r = new();
            vectors = new double[n, 4];
            for (int i = 0; i < n; i++)
            {
                vectors[i, 0] = r.NextDouble();
                vectors[i, 1] = r.NextDouble();
                vectors[i, 2] = r.NextDouble();
            }
        }
        public bool TestLenght()
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
            threads = new Thread[threadCount];
            chunk = n / threadCount;
            rest = n % threadCount;
            start = 0;
            for (int i = 0; i < threadCount; i++)
            {
                int count = chunk + (i < rest ? 1 : 0);
                int localStart = start;
                threads[i] = new Thread(() =>
                {
                    lenghtAsm(ref vectors[localStart, 0], count, n, ref vectors[0, 0], ref lenghtsAsm[localStart, 0]);
                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (lenghts[i, j] != lenghtsAsm[i, j])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public bool TestKernel()
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
            threads = new Thread[threadCount];
            chunk = n / threadCount;
            rest = n % threadCount;
            start = 0;
            for (int i = 0; i < threadCount; i++)
            {
                int count = chunk + (i < rest ? 1 : 0);
                int localStart = start;
                threads[i] = new Thread(() =>
                {
                    kernelAsm(ref lenghts[localStart, 0], count, n, ref kernelsAsm[localStart, 0]);
                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            const double tolerance = 0.1;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (Math.Abs(kernels[i, j] - kernelsAsm[i, j]) > tolerance)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}