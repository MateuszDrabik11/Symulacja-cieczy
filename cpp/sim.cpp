#include <math.h>
#include <stdio.h>
#define _USE_MATH_DEFINES
#define k 2000
double h = 2.0;

extern "C" int add(int a, int b)
{
    return a + b;
}
extern "C" void lenght(double *start, unsigned long count, unsigned long base_count, double *base, double *output)
{
    for (unsigned long j = 0; j < base_count; j++)
    {
        for (unsigned long i = 0; i < count; i++)
        {
            double sum = (start[i * 4] - base[4 * j]) * (start[i * 4] - base[4 * j]) + (start[i * 4 + 1] - base[4 * j + 1]) * (start[i * 4 + 1] - base[4 * j + 1]) + (start[i * 4 + 2] - base[4 * j + 2]) * (start[i * 4 + 2] - base[4 * j + 2]);
            output[base_count * i + j] = sqrt(sum);
        }
    }
}
extern "C" void kernel(double *lenghts_start, unsigned long chunk, unsigned long size, double *output)
{
    for (unsigned long i = 0; i < size; i++)
    {
        for (unsigned long j = 0; j < chunk; j++)
        {
            double r = lenghts_start[size*j + i];
            if(r * r > h*h)
            {
                output[size*j + i] = 0;
                continue;
            }
            output[size*j + i] = 315.0f / (64.0f * 3.1416 * pow(h, 9)) * pow(h*h - r*r, 3);
        }
    }
}
extern "C" void kernel_derivative(double *lenghts, double *vector_start, double *vectors, unsigned long chunk, unsigned long size, double *output)
{
    for (unsigned long i = 0; i < size; i++)
    {
        for (unsigned long j = 0; j < chunk; j++)
        {
            unsigned long index = (size * j + i) * 4;
            double vec[3] = {vector_start[4 * i] - vectors[4 * j], vector_start[4 * i + 1] - vectors[4 * j + 1], vector_start[4 * i + 2] - vectors[4 * j + 2]};
            if(lenghts[size * j + i] == 0)
            {
                output[index] = 0;
                output[index + 1] = 0;
                output[index + 2] = 0;
                output[index + 3] = 0;
                continue;
            }
            float t1 = -45.0f / (M_PI * pow(h, 6));
            float t2 = pow(h - lenghts[size * j + i], 2);
            output[index] = vec[0] * t1 * t2/lenghts[size * j + i];
            output[index + 1] = vec[1] * t1 * t2/lenghts[size * j + i];
            output[index + 2] = vec[2] * t1 * t2/lenghts[size * j + i];
            output[index + 3] = 0;
        }
    }
}
double max(double a, double b)
{
    return a > b ? a : b;
}
double min(double a, double b)
{
    return a < b ? a : b;
}
extern "C" void calc_density_and_pressure(double *masses, double *kernels, long particle_index, long number_of_particles, long chunk, double *out_density, double *out_pressure, double fluid_density)
{
    for (long j = 0; j < chunk; ++j)
    {
        double density = 0;
        for (long i = 0; i < number_of_particles; ++i)
        {
            density += masses[i] * kernels[number_of_particles * (particle_index + j) + i];
        }
        density = max(density,1.0);
        out_density[particle_index + j] = density;
        out_pressure[particle_index + j] = k * (density - fluid_density);
    }
}

extern "C" void calc_forces(double *masses, double *densities, double *kernel_derivatives, double *kernels, double *velocities, double *positions, long particles, long start_index, long chunk, double *accelerations)
{
    for (long i = 0; i < chunk; i++)
    {
        double pressure[4] = {0, 0, 0, 0};
        for (long j = 0; j < particles; j++)
        {
            pressure[0] -= masses[j] * (masses[start_index + i] / (densities[start_index + i] * densities[start_index + i]) +masses[j] / (densities[j] * densities[j])) *
                           kernel_derivatives[particles * (start_index + i) + j];
            pressure[1] -= masses[j] *
                           (masses[start_index + i] / (densities[start_index + i] * densities[start_index + i]) +
                            masses[j] / (densities[j] * densities[j])) *
                           kernel_derivatives[particles * (start_index + i) + j + 1];
            pressure[2] -= masses[j] *
                           (masses[start_index + i] / (densities[start_index + i] * densities[start_index + i]) +
                            masses[j] / (densities[j] * densities[j])) *
                           kernel_derivatives[particles * (start_index + i) + j + 2];
        }
        double viscosity[4] = {0, 0, 0, 0};
        for (long j = 0; j < particles; j++)
        {
            viscosity[0] += (velocities[4 * j] - velocities[4 * (start_index + i)]) * kernels[particles * (start_index + i) + j];
            viscosity[1] += (velocities[4 * j + 1] - velocities[4 * (start_index + i) + 1]) * kernels[particles * (start_index + i) + j];
            viscosity[2] += (velocities[4 * j + 2] - velocities[4 * (start_index + i) + 2]) * kernels[particles * (start_index + i) + j];
        }
        accelerations[4 * (start_index + i)] = pressure[0] + viscosity[0] / densities[start_index + i];
        accelerations[4 * (start_index + i) + 1] = pressure[1] + viscosity[1] / densities[start_index + i];
        accelerations[4 * (start_index + i) + 2] = pressure[2] + viscosity[2] / densities[start_index + i];
    }
}
extern "C" void time_integration(double *positions, double *velocities, double *accelerations, double dt, long start_index, long chunk)
{
    for (long i = 0; i < chunk; i++)
    {
        velocities[4 * (start_index + i)] += dt * accelerations[4 * (start_index + i)];
        velocities[4 * (start_index + i) + 1] += dt * accelerations[4 * (start_index + i) + 1];
        velocities[4 * (start_index + i) + 2] += dt * accelerations[4 * (start_index + i) + 2];

        positions[4 * (start_index + i)] += dt * velocities[4 * (start_index + i)];
        positions[4 * (start_index + i) + 1] += dt * velocities[4 * (start_index + i) + 1];
        positions[4 * (start_index + i) + 2] += dt * velocities[4 * (start_index + i) + 2];
    }
}
extern "C" void add_external_force(double *accelerations, double *forces, long start_index, long chunk)
{
    for (long i = 0; i < chunk; i++)
    {
        accelerations[4 * (start_index + i)] += forces[4 * (start_index + i)];
        accelerations[4 * (start_index + i) + 1] += forces[4 * (start_index + i) + 1];
        accelerations[4 * (start_index + i) + 2] += forces[4 * (start_index + i) + 2];
    }
}
extern "C" void gravity(double *accelerations, double g, long start_index, long chunk)
{
    for (long i = 0; i < chunk; i++)
    {
        accelerations[4 * (start_index + i) + 2] += -g;
    }
}
extern "C" void boundries(double *positions, double *velocities, long start_index, long chunk, double x_max, double y_max, double z_max, double bouncines)
{
    double eps = 0.001;
    for (long i = 0; i < chunk; i++)
    {
        if (positions[4 * (start_index + i)] < 0 || positions[4 * (start_index + i)] > x_max)
        {
            velocities[4 * (start_index + i)] = -velocities[4 * (start_index + i)] * bouncines;
        }
        if (positions[4 * (start_index + i) + 1] < 0 || positions[4 * (start_index + i) + 1] > y_max)
        {
            velocities[4 * (start_index + i) + 1] = -velocities[4 * (start_index + i) + 1] * bouncines;
        }
        if (positions[4 * (start_index + i) + 2] < 0 || positions[4 * (start_index + i) + 2] > z_max)
        {
            velocities[4 * (start_index + i) + 2] = -velocities[4 * (start_index + i) + 2] * bouncines;
        }
        positions[4 * (start_index + i)] = max(0+eps, min(positions[4 * (start_index + i)], x_max-eps));
        positions[4 * (start_index + i) + 1] = max(0+eps, min(positions[4 * (start_index + i) + 1], y_max-eps));
        positions[4 * (start_index + i) + 2] = max(0+eps, min(positions[4 * (start_index + i) + 2], z_max-eps));
    }
}