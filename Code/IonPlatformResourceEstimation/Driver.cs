using System;
using System.Threading.Tasks;

namespace IonPlatformResourceEstimation
{
    class Driver
    {
        // TODO: Get from command line.
        const double IonPhysicalRGateFidelity = 0.99;
        const double IonPhysicalRGateTime = 20.0; // us
        const double IonPhysicalXXGateFidelity = 0.96;
        const double IonPhysicalXXGateTime = 235.0; // us

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Ion Platform Resource Estimation");
            var simulator = new IonPlatformResourceEstimator(
                IonPhysicalRGateFidelity,
                IonPhysicalRGateTime,
                IonPhysicalXXGateFidelity,
                IonPhysicalXXGateTime);

            await BernsteinVazirani.Run(simulator);
            simulator.PrintPhysicalLayerStats();
        }
    }
}
