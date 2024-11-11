#include <math.h>
#define _USE_MATH_DEFINES
#define h 0.5

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
            double sum = (start[i * 4] - base[4*j]) * (start[i * 4] - base[4*j]) + (start[i * 4 + 1] - base[4*j + 1]) * (start[i * 4 + 1] - base[4*j + 1]) + (start[i * 4 + 2] - base[4*j + 2]) * (start[i * 4 + 2] - base[4*j + 2]);
            output[base_count*i + j] = sqrt(sum);
        }
    }
}
extern "C" void kernel(double* lenghts_start,unsigned long chunk,unsigned long size,double* output)
{
    for (unsigned long i = 0; i < size; i++)
    {
        for (unsigned long j = 0; j < chunk; j++)
        {
            if(lenghts_start[size*j + i] > 2* h)
            {
                output[size*j + i] = 0;
                continue;
            }
            double q = lenghts_start[size*j + i] / h;

            if(1>q && q >0)
            {
                output[size*j + i] = (1 - 1.5*q*q + 0.75 * q * q * q) / (M_PI * h * h * h);
                continue;
            }
            else if(2>=q && q>= 1)
            {
                output[size*j + i] = 0.25*(2-q)*(2-q)*(2-q) / (M_PI * h * h * h);
                continue;
            }
            else
            {
                output[size*j + i] = 0;
                continue;
            }
        }
    }
}
extern "C" void kernel_derivative(double* lenghts,double* vector_start,double* vectors,unsigned long chunk, unsigned long size, double* output)
{
    for (unsigned long i = 0; i < size; i++)
    {
        for (unsigned long j = 0; j < chunk; j++)
        {
            unsigned long index = (size * j + i) * 4;
            if(lenghts[size*j + i]> 2 * h)
            {
                output[index] = 0;
                output[index + 1] = 0;
                output[index + 2] = 0;
                output[index + 3] = 0;
                continue;
            }
            double q = lenghts[size*j + i] / h;
            double vec[3] = {vector_start[4*i]-vectors[4*j],vector_start[4*i+1]-vectors[4*j+1],vector_start[4*i+2]-vectors[4*j+2]};

            if(1>q && q >0)
            {
                output[index] = (-3*q+2.25*q*q*vec[0]) / (lenghts[size*j + i] * M_PI * h * h * h * h);
                output[index + 1] = (-3*q+2.25*q*q*vec[1]) / (lenghts[size*j + i] * M_PI * h * h * h * h);
                output[index + 2] = (-3*q+2.25*q*q*vec[2]) / (lenghts[size*j + i] * M_PI * h * h * h * h);
                output[index + 3] = 0;
                continue;
            }
            else if(2>=q && q>= 1)
            {
                output[index] = -0.75*(2-q)*(2-q)*vec[0]/(lenghts[size*j + i] * M_PI * h * h * h * h);
                output[index + 1] = -0.75*(2-q)*(2-q)*vec[1]/(lenghts[size*j + i] * M_PI * h * h * h * h);
                output[index + 2] = -0.75*(2-q)*(2-q)*vec[2]/(lenghts[size*j + i] * M_PI * h * h * h * h);
                output[index + 3] = 0;
                continue;
            }
            else
            {
                output[index] = 0;
                output[index + 1] = 0;
                output[index + 2] = 0;
                output[index + 3] = 0;
                continue;
            }
        }
    }
}