using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.QCTraceSimulatorRuntime;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Simulators.QCTraceSimulators;

namespace Tracking
{
    public class ProgramTracer : QuantumSimulator
    {
        private IDictionary<string, int> Operations;

        public ProgramTracer()
        {
            Operations = new Dictionary<string, int>();
            OnOperationStart += TrackOperationStart;
        }

        public void DisplayStats()
        {
            Operations
                .OrderBy(item => item.Key)
                .ToList()
                .ForEach(item => Console.WriteLine($"{item.Key}: {item.Value}"));
        }

        public void TrackOperationStart(ICallable op, IApplyData data)
        {
            if (Operations.ContainsKey(op.FullName))
            {
                Operations[op.FullName] += 1;
            }
            else
            {
                Operations.Add(op.FullName, 1);
            }
        }
    }
}