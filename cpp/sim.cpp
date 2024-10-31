#include <math.h>

extern "C" int add(int a, int b)
{
    return a+b;
}
extern "C" void lenght(double* start, unsigned long count, double* base, double* output)
{
    for(unsigned long i = 0; i < count; ++i)
    {
        double sum = (start[i * 4] - base[0]) * (start[i * 4] - base[0]) 
        + (start[i * 4 + 1] - base[1]) * (start[i * 4 + 1] - base[1])
        + (start[i * 4 + 2] - base[2]) * (start[i * 4 + 2] - base[2]);
        output[i] = sqrt(sum);
    }
}