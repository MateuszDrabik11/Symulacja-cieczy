using Avalonia;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Symulacja_cząsteczek_cieczy;

class Program
{
    [DllImport("../../../libc.so")]
    extern static int add(int a, int b);

    [DllImport("../../../libasm.so", EntryPoint = "_start")]
    extern static void hello();

    [DllImport("../../../libasm.so", EntryPoint = "increment_array")]
    extern static int add1(ref int a, int size);
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
        int a = 5;
        int b = 2123;
        Console.WriteLine("{0} + {1} = {2}", a, b, add(a, b));
        hello();
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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
