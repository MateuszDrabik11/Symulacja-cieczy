using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Symulacja_cząsteczek_cieczy;

class sph_solver
{
    protected const string path = "path-to-dll/so";
    public long Number_of_particles { get; set; }
    public long Number_of_threads { get; set; }

    protected double[,] vectors;
    protected double[,] lenghts;
    protected double[,] kernels; 
    protected double[,,] kernel_derivatives;
    protected double[,] velocities;
    protected double[,] accelerations;
    protected double[] masses;
    protected double[] pressures;
    protected double[] densities;

    public sph_solver(long particles, long threads)
    {
        Number_of_particles = particles;
        Number_of_threads = threads;
        vectors = new double[Number_of_particles, 4];
        lenghts = new double[Number_of_particles, Number_of_particles];
        kernels = new double[Number_of_particles, Number_of_particles];
        kernel_derivatives = new double[Number_of_particles, Number_of_particles, 4];
        velocities = new double[Number_of_particles, 4];
        accelerations = new double[Number_of_particles, 4];
        masses = new double[Number_of_particles];
        pressures = new double[Number_of_particles];    //must be locked or used to prevent gc from free
        densities = new double[Number_of_particles];
        Random r = new Random();
        for (int i = 0; i < Number_of_particles; ++i)
        {
            vectors[i, 0] = r.NextDouble();
            vectors[i, 1] = r.NextDouble();
            vectors[i, 2] = 1 - 0.000001;
        }
        for (int i = 0; i < Number_of_particles; ++i)
        {
            velocities[i, 0] = r.NextDouble();
            velocities[i, 1] = r.NextDouble();
            velocities[i, 2] = r.NextDouble();
        }
        for (int i = 0; i < Number_of_particles; i++)
        {
            masses[i] = 3.33;
        }

    }
    public sph_solver() : this(50, 4)
    {

    }

    public void Step()
    {

    }
    public double[,] GetParticlePosition()
    {
        return vectors;
    }
    public double[] GetPressure()
    {
        return pressures;
    }
    public double[] GetDensity()
    {
        return densities;
    }
}

class asm_solver : sph_solver
{
    [DllImport(path+"libasm.so", EntryPoint = "lenght")]
    extern static void lenght(ref double start, long count, long size, ref double b, ref double output);

    [DllImport(path+"libasm.so", EntryPoint = "kernel")]
    extern static void kernel(ref double lenghts, long chunk, long size, ref double output);
    [DllImport(path+"libasm.so", EntryPoint = "kernel_derivative")]
    extern static void kernel_derivative(ref double lenghts, ref double chunk_start, ref double vectors, long chunk, long size, ref double output);
    [DllImport("../../../libasm.so", EntryPoint = "calc_density_and_pressure")]
    extern static void calc_density(double[] masses, ref double kernels, long p_index, long number_of_particles, long chunk, double[] out_density, double[] out_pressure, double fluid_density);
    [DllImport("../../../libasm.so", EntryPoint = "calc_pressure")]
    extern static void calc_pressure(double[] density, long index, double[] pressure,long chunk, double fluid_density);
    [DllImport("../../../libasm.so", EntryPoint = "calc_forces")]
    extern static void calc_forces(double[] masses, double[] densities, ref double kernel_derivatives, ref double kernels, ref double velocities, ref double positions, long particles, long start_index, long chunk, ref double accelerations);
    [DllImport(path+"libasm.so", EntryPoint = "gravity")]
    extern static void apply_gravity(ref double accelerations, double g, long start_index, long chunk);
    [DllImport(path+"libasm.so", EntryPoint = "time_integration")]
    extern static void time_integration(ref double positions, ref double velocities, ref double accelerations, double dt, long start_index, long chunk);
    //temp
    [DllImport(path+"libasm.so", EntryPoint = "boundries")]
    extern static void boundries(ref double positions, ref double velocities, long start_index, long chunk, double x_max, double y_max, double z_max, double bouncines);
    public asm_solver() : base()
    {
        
    }
    public asm_solver(long particles, long threads) : base(particles,threads)
    {

    }
    public new void Step()
    {
        Thread[] threads = new Thread[Number_of_threads];
        long chunk = Number_of_particles / Number_of_threads;
        long rest = Number_of_particles % Number_of_threads;
        long start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.lenght(ref vectors[localStart, 0], count, Number_of_particles, ref vectors[0, 0], ref lenghts[localStart, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //lenght calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.kernel(ref lenghts[localStart, 0], count, Number_of_particles, ref kernels[localStart, 0]);
                asm_solver.kernel_derivative(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, Number_of_particles, ref kernel_derivatives[localStart, 0, 0]);

            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //kernel calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.calc_density(masses, ref kernels[0, 0], localStart, Number_of_particles, count, densities, pressures,30);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.calc_pressure(densities,localStart,pressures,chunk,30);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //pressure and density calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.calc_forces(masses, densities, ref kernel_derivatives[0, 0, 0], ref kernels[0, 0], ref velocities[0, 0], ref vectors[0, 0], Number_of_particles, localStart, count, ref accelerations[0, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //force calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.apply_gravity(ref accelerations[0, 0], 10, localStart, count);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //gravity influence
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.time_integration(ref vectors[0, 0], ref velocities[0, 0], ref accelerations[0, 0], 0.1, localStart, count);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //time integration
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                asm_solver.boundries(ref vectors[0, 0], ref velocities[0, 0], localStart, count, 1, 1, 1, 0.6);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
    }

}

class c_solver : sph_solver
{
    [DllImport(path+"libc.so", EntryPoint = "kernel")]
    extern static void kernel(ref double lenghts, long chunk, long size, ref double output);
    [DllImport(path+"libc.so", EntryPoint = "kernel_derivative")]
    extern static void kernel_derivative(ref double lenghts, ref double chunk_start, ref double vectors, long chunk, long size, ref double output);

    [DllImport(path+"libc.so", EntryPoint = "lenght")]
    extern static void lenght(ref double start, long count, long size, ref double b, ref double output);
    [DllImport(path+"libc.so", EntryPoint = "calc_density_and_pressure")]
    extern static void calc_density_and_pressure(double[] masses, ref double kernels, long p_index, long number_of_particles, long chunk, double[] out_density, double[] out_pressure, double fluid_density);
    [DllImport(path+"libc.so", EntryPoint = "calc_forces")]
    extern static void calc_forces(double[] masses, double[] densities, ref double kernel_derivatives, ref double kernels, ref double velocities, ref double positions, long particles, long start_index, long chunk, ref double accelerations);

    [DllImport(path+"libc.so", EntryPoint = "time_integration")]
    extern static void time_integration(ref double positions, ref double velocities, ref double accelerations, double dt, long start_index, long chunk);
    [DllImport(path+"libc.so", EntryPoint = "add_external_force")]
    extern static void external_force(ref double accelerations, ref double forces, long start_index, long chunk);
    [DllImport(path+"libc.so", EntryPoint = "gravity")]
    extern static void apply_gravity(ref double accelerations, double g, long start_index, long chunk);
    [DllImport(path+"libc.so", EntryPoint = "boundries")]
    extern static void boundries(ref double positions, ref double velocities, long start_index, long chunk, double x_max, double y_max, double z_max, double bouncines);

    public c_solver(long particles, long threads) : base(particles,threads)
    {

    }

    public c_solver() : base()
    {

    }
    public new void Step()
    {
        Thread[] threads = new Thread[Number_of_threads];
        long chunk = Number_of_particles / Number_of_threads;
        long rest = Number_of_particles % Number_of_threads;
        long start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.lenght(ref vectors[localStart, 0], count, Number_of_particles, ref vectors[0, 0], ref lenghts[localStart, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //lenght calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.kernel(ref lenghts[localStart, 0], count, Number_of_particles, ref kernels[localStart, 0]);
                c_solver.kernel_derivative(ref lenghts[localStart, 0], ref vectors[localStart, 0], ref vectors[0, 0], count, Number_of_particles, ref kernel_derivatives[localStart, 0, 0]);

            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //kernel calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.calc_density_and_pressure(masses, ref kernels[0, 0], localStart, Number_of_particles, count, densities, pressures,30);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //pressure and density calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.calc_forces(masses, densities, ref kernel_derivatives[0, 0, 0], ref kernels[0, 0], ref velocities[0, 0], ref vectors[0, 0], Number_of_particles, localStart, count, ref accelerations[0, 0]);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //force calculation
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.apply_gravity(ref accelerations[0, 0], 10, localStart, count);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //gravity influence
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.time_integration(ref vectors[0, 0], ref velocities[0, 0], ref accelerations[0, 0], 0.01, localStart, count);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
        //time integration
        threads = new Thread[Number_of_threads];
        rest = Number_of_particles % Number_of_threads;
        start = 0;
        for (int i = 0; i < Number_of_threads; i++)
        {
            long count = chunk + (i < rest ? 1 : 0);
            long localStart = start;
            threads[i] = new Thread(() =>
            {
                c_solver.boundries(ref vectors[0, 0], ref velocities[0, 0], localStart, count, 1, 1, 1, 0.6);
            });
            threads[i].Start();
            start += count;
        }
        for (int i = 0; i < Number_of_threads; i++)
        {
            threads[i].Join();
        }
    }
}
