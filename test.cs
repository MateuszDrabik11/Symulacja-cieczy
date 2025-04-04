using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Data;

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
        extern static void boundries(ref double positions, ref double velocities, long start_index, long chunk, double x_max, double y_max, double z_max, double bouncines);

        //      asm library

        [DllImport("../../../libasm.so", EntryPoint = "lenght")]
        extern static void lenghtAsm(ref double start, long count, long size, ref double b, ref double output);

        [DllImport("../../../libasm.so", EntryPoint = "kernel")]
        extern static void kernelAsm(ref double lenghts, long chunk, long size, ref double output);
        [DllImport("../../../libasm.so", EntryPoint = "kernel_derivative")]
        extern static void kernel_derivativeAsm(ref double lenghts, ref double chunk_start, ref double vectors, long chunk, long size, ref double output);
        [DllImport("../../../libasm.so", EntryPoint = "calc_density_and_pressure")]
        extern static void calc_density_and_pressureAsm(double[] masses, ref double kernels, long p_index, long number_of_particles, long chunk, double[] out_density, double[] out_pressure);
        [DllImport("../../../libasm.so", EntryPoint = "calc_forces")]
        extern static void calc_forcesAsm(double[] masses, double[] densities, ref double kernel_derivatives, ref double kernels, ref double velocities, ref double positions, long particles, long start_index, long chunk, ref double accelerations);

        [DllImport("../../../libasm.so", EntryPoint = "boundries")]
        extern static void boundriesAsm(ref double positions, ref double velocities, long start_index, long chunk, double x_max, double y_max, double z_max, double bouncines);
        [DllImport("../../../libasm.so", EntryPoint = "add_external_force")]
        extern static void external_forceAsm(ref double accelerations, ref double forces, long start_index, long chunk);


        private const int threadCount = 4;
        private const int n = 10;

        private double[,] vectors;
        private double[,] lenghts = new double[n, n];
        private double[,] lenghtsAsm = new double[n, n];
        private double[,] kernels = new double[n, n];
        private double[,] kernelsAsm = new double[n, n];
        private double[,,] kernel_derivatives = new double[n, n, 4];
        private double[,,] kernel_derivativesAsm = new double[n, n, 4];
        private double[,] velocities = new double[n, 4];
        private double[,] accelerations = new double[n, 4];
        private double[] masses = new double[n];
        private double[] pressures = new double[n];
        private double[] densities = new double[n];
        private double[,] velocitiesAsm = new double[n, 4];
        private double[,] accelerationsAsm = new double[n, 4];
        private double[] pressuresAsm = new double[n];
        private double[] densitiesAsm = new double[n];
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
            for (int i = 0; i < n; i++)
            {
                masses[i] = 3.33;
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
            const double tolerance = 0.05;
            double min = 1.0;
            double max = 0.0;
            double avg = 0;
            bool result = true;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    avg += Math.Abs(kernels[i, j] - kernelsAsm[i, j]);
                    if (Math.Abs(kernels[i, j] - kernelsAsm[i, j]) < min)
                    {
                        min = Math.Abs(kernels[i, j] - kernelsAsm[i, j]);
                    }
                    if (Math.Abs(kernels[i, j] - kernelsAsm[i, j]) > max)
                    {
                        max = Math.Abs(kernels[i, j] - kernelsAsm[i, j]);
                    }
                    if (Math.Abs(kernels[i, j] - kernelsAsm[i, j]) > tolerance)
                    {
                        result = false;
                    }
                }
            }
            avg /= n * n;
            Console.WriteLine("min:{0} max:{1} avg:{2}", min, max, avg);
            return result;
        }
        public bool TestKernelDerivative()
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
                    kernel_derivativeAsm(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, n, ref kernel_derivativesAsm[localStart, 0, 0]);

                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            const double tolerance = 0.2;
            double min = 1.0;
            double max = 0.0;
            double avg = 0;
            bool result = true;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double x = Math.Abs(kernel_derivatives[i, j, 0] - kernel_derivativesAsm[i, j, 0]);
                    double y = Math.Abs(kernel_derivatives[i, j, 1] - kernel_derivativesAsm[i, j, 1]);
                    double z = Math.Abs(kernel_derivatives[i, j, 2] - kernel_derivativesAsm[i, j, 2]);
                    double aa = (x + y + z) / 3;
                    if (aa < min)
                    {
                        min = aa;
                    }
                    if (aa > max)
                    {
                        max = aa;
                    }
                    avg += aa;
                }
            }
            avg /= n * n;
            if (avg > tolerance)
            {
                result = false;
            }
            Console.WriteLine("min:{0} max:{1} avg:{2}", min, max, avg);
            return result;
        }
        public bool TestPressureCalc()
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
                    calc_density_and_pressureAsm(masses, ref kernels[0, 0], localStart, n, count, densitiesAsm, pressuresAsm);
                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            const double tolerance = 0.5;
            double min = 1.0;
            double max = 0.0;
            double avg = 0;
            bool result = true;
            for (int i = 0; i < n; i++)
            {
                avg += Math.Abs(densities[i] - densitiesAsm[i]);
                if (Math.Abs(densities[i] - densitiesAsm[i]) < min)
                {
                    min = Math.Abs(densities[i] - densitiesAsm[i]);
                }
                if (Math.Abs(densities[i] - densitiesAsm[i]) > max)
                {
                    max = Math.Abs(densities[i] - densitiesAsm[i]);
                }
                if (Math.Abs(densities[i] - densitiesAsm[i]) > tolerance)
                {
                    result = false;
                }
            }
            avg /= n;
            Console.WriteLine("min:{0} max:{1} avg:{2}", min, max, avg);
            return result;
        }
        public bool TestForceCalc()
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
                    calc_forcesAsm(masses, densities, ref kernel_derivatives[0, 0, 0], ref kernels[0, 0], ref velocities[0, 0], ref vectors[0, 0], n, localStart, count, ref accelerationsAsm[0, 0]);
                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            const double tolerance = 0.5;
            double min = 1.0;
            double max = 0.0;
            double avg = 0;
            bool result = true;
            for (int i = 0; i < n; i++)
            {
                //Console.WriteLine($"[{accelerations[i, 0],10:0.0000000},{accelerations[i, 1],10:0.0000000},{accelerations[i, 2],10:0.0000000}]     |      [{accelerationsAsm[i, 0],10:0.0000000},{accelerationsAsm[i, 1],10:0.0000000},{accelerationsAsm[i, 2],10:0.0000000}]");
                double x = Math.Abs(accelerations[i, 0] - accelerationsAsm[i, 0]);
                double y = Math.Abs(accelerations[i, 1] - accelerationsAsm[i, 1]);
                double z = Math.Abs(accelerations[i, 2] - accelerationsAsm[i, 2]);
                double aa = (x + y + z) / 3;
                if (aa < min)
                {
                    min = aa;
                }
                if (aa > max)
                {
                    max = aa;
                }
                avg += aa;
            }
            avg /= n;
            if (avg > tolerance)
            {
                result = false;
            }
            Console.WriteLine("min:{0} max:{1} avg:{2}", min, max, avg);
            return result;
        }
        public bool TestBoundries()
        {
            bool result = true;
            double[,] vectorsAsm = new double[n, 4];
            for (int i = 0; i < n; i++)
            {
                velocities[i, 0] = 1;
                velocities[i, 1] = 1;
                velocities[i, 2] = 1;
                velocitiesAsm[i, 0] = 1;
                velocitiesAsm[i, 1] = 1;
                velocitiesAsm[i, 2] = 1;
            }
            vectors[0, 0] = 1;
            vectors[0, 1] = 0.1;
            vectors[0, 2] = 0.1;

            vectors[1, 0] = 1;
            vectors[1, 1] = 1;
            vectors[1, 2] = 0.1;

            vectors[2, 0] = 1;
            vectors[2, 1] = 1;
            vectors[2, 2] = 1;

            vectors[3, 0] = 0;
            vectors[3, 1] = 0.1;
            vectors[3, 2] = 0.1;

            vectors[4, 0] = 0;
            vectors[4, 1] = 0;
            vectors[4, 2] = 0.1;

            vectors[5, 0] = 0;
            vectors[5, 1] = 0;
            vectors[5, 2] = 0;

            vectors[6, 0] = -1;
            vectors[6, 1] = 0;
            vectors[6, 2] = 0;

            vectors[7, 0] = -1;
            vectors[7, 1] = -1;
            vectors[7, 2] = 0;

            vectors[8, 0] = -1;
            vectors[8, 1] = -1;
            vectors[8, 2] = -1;

            vectors[9, 0] = 0.1;
            vectors[9, 1] = 0.1;
            vectors[9, 2] = 0.1;

            vectorsAsm[0, 0] = 1;
            vectorsAsm[0, 1] = 0.1;
            vectorsAsm[0, 2] = 0.1;

            vectorsAsm[1, 0] = 1;
            vectorsAsm[1, 1] = 1;
            vectorsAsm[1, 2] = 0.1;

            vectorsAsm[2, 0] = 1;
            vectorsAsm[2, 1] = 1;
            vectorsAsm[2, 2] = 1;

            vectorsAsm[3, 0] = 0;
            vectorsAsm[3, 1] = 0.1;
            vectorsAsm[3, 2] = 0.1;

            vectorsAsm[4, 0] = 0;
            vectorsAsm[4, 1] = 0;
            vectorsAsm[4, 2] = 0.1;

            vectorsAsm[5, 0] = 0;
            vectorsAsm[5, 1] = 0;
            vectorsAsm[5, 2] = 0;

            vectorsAsm[6, 0] = -1;
            vectorsAsm[6, 1] = 0;
            vectorsAsm[6, 2] = 0;

            vectorsAsm[7, 0] = -1;
            vectorsAsm[7, 1] = -1;
            vectorsAsm[7, 2] = 0;

            vectorsAsm[8, 0] = -1;
            vectorsAsm[8, 1] = -1;
            vectorsAsm[8, 2] = -1;

            vectorsAsm[9, 0] = 0.1;
            vectorsAsm[9, 1] = 0.1;
            vectorsAsm[9, 2] = 0.1;


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
                    boundries(ref vectors[0, 0], ref velocities[0, 0], localStart, count, 1, 1, 1, 0.6);
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
                    boundriesAsm(ref vectorsAsm[0, 0], ref velocitiesAsm[0, 0], localStart, count, 1, 1, 1, 0.6);
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
                for (int j = 0; j < 4; j++)
                {
                    if (vectors[i, j] != vectorsAsm[i, j])
                    {
                        result = false;
                    }
                    if (velocities[i, j] != velocitiesAsm[i, j])
                    {
                        result = false;
                    }
                }
            }
            // for (int i = 0; i < n; i++)
            // {
            //     Console.WriteLine($"{vectors[i, 0]} , {vectors[i, 1]} , {vectors[i, 2]}    |   {vectorsAsm[i, 0]} , {vectorsAsm[i, 1]} , {vectorsAsm[i, 2]}");
            // }
            // for (int i = 0; i < n; i++)
            // {
            //     Console.WriteLine($"{velocities[i, 0]} , {velocities[i, 1]} , {velocities[i, 2]}    |   {velocitiesAsm[i, 0]} , {velocitiesAsm[i, 1]} , {velocitiesAsm[i, 2]}");
            // }
            return result;
        }
        public bool TestExternalForce()
        {
            Random r = new Random();
            double[,] f = new double[n, 4];
            for (int i = 0; i < n; i++)
            {
                f[i, 0] = r.NextDouble();
                f[i, 1] = r.NextDouble();
                f[i, 2] = r.NextDouble();
            }
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
                    external_force(ref accelerations[0, 0], ref f[0, 0], localStart, count);
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
                    external_forceAsm(ref accelerationsAsm[0, 0], ref f[0, 0], localStart, count);
                });
                threads[i].Start();
                start += count;
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            const double tolerance = 0.2;
            double min = 1.0;
            double max = 0.0;
            double avg = 0;
            bool result = true;
            for (int i = 0; i < n; i++)
            {
                double x = Math.Abs(accelerations[i, 0] - accelerationsAsm[i, 0]);
                double y = Math.Abs(accelerations[i, 1] - accelerationsAsm[i, 1]);
                double z = Math.Abs(accelerations[i, 2] - accelerationsAsm[i, 2]);
                double aa = (x + y + z) / 3;
                if (aa < min)
                {
                    min = aa;
                }
                if (aa > max)
                {
                    max = aa;
                }
                avg += aa;
            }
            avg /= n;
            if (avg > tolerance)
            {
                result = false;
            }
            Console.WriteLine("min:{0} max:{1} avg:{2}", min, max, avg);
            return result;

        }
    }
}