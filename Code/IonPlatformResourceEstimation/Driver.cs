using System;
using System.Threading.Tasks;

using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

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
            var resourceEstimation = new IonPlatformResourceEstimator(
                IonPhysicalRGateFidelity,
                IonPhysicalRGateTime,
                IonPhysicalXXGateFidelity,
                IonPhysicalXXGateTime);

            //await BernsteinVazirani.Run(resourceEstimation);
            //resourceEstimation.PrintPhysicalLayerStats();

            Console.WriteLine("Full State Simulator");
            using var fullStateSimulator = new QuantumSimulator();
            //await BernsteinVazirani.Run(fullStateSimulator);
            await BernsteinVaziraniErrorCorrected.Run(fullStateSimulator);
        }
    }
}
