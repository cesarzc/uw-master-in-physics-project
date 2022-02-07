using System;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators.QCTraceSimulators;

using Shor;

namespace ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Console App");
            using (QuantumSimulator sim = new QuantumSimulator())
            {
                (long factor1, long factor2) =
                    FactorSemiprimeInteger.Run(sim, 15).Result;

                Console.WriteLine($"Factors are {factor1} and {factor2}");
            }


            return 0;
        }

    }
}
