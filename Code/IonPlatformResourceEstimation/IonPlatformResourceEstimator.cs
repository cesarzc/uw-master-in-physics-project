using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.QuantumProcessor;
using System;
using System.Linq;

namespace IonPlatformResourceEstimation
{
    class IonPlatformResourceEstimatorProcessor : QuantumProcessorBase
    {
        public readonly IonPlatformPhysicalLayerTracker PhysicalLayerTracker;

        private static class PlatformParameters
        {
            public static int s = 1;
            public static int v = 1;
        };

        public IonPlatformResourceEstimatorProcessor(
            double physicalRGateFidelity,
            double physicalRGateTime,
            double physicalXXGateFidelity,
            double physicalXXGateTime)
        {
            PhysicalLayerTracker = new IonPlatformPhysicalLayerTracker(
                physicalRGateFidelity,
                physicalRGateTime,
                physicalXXGateFidelity,
                physicalXXGateTime);
        }

        public override void ControlledX(IQArray<Qubit> controls, Qubit qubit)
        {
            // N.B. Currently only ONE control qubit is supported.
            if (controls.Count > 1)
            {
                throw new NotSupportedException("Only ONE control qubit is supported");
            }

            var control = controls[0];
            R(Pauli.PauliY, PlatformParameters.v * Math.PI / 2.0, control);
            PhysicalLayerTracker.XX(PlatformParameters.s * Math.PI / 4.0, control, qubit);
            R(Pauli.PauliX, -PlatformParameters.s * Math.PI / 2.0, control);
            R(Pauli.PauliX, -PlatformParameters.v * PlatformParameters.s * Math.PI / 2.0, qubit);
            R(Pauli.PauliY, -PlatformParameters.v * Math.PI / 2.0, control);

        }
        public override void H(Qubit qubit)
        {
            R(Pauli.PauliX, Math.PI, qubit);
            R(Pauli.PauliY, -Math.PI / 2, qubit);
        }

        public override Result M(Qubit qubit)
        {
            // Do nothing.
            return Result.Zero;
        }

        public override void OnAllocateQubits(IQArray<Qubit> qubits)
        {
            qubits.ToList().ForEach(q => PhysicalLayerTracker.AllocateQubit(q));
        }

        public override void OnReleaseQubits(IQArray<Qubit> qubits)
        {
            // Do nothing.
        }

        public void PrintPhysicalLayerStats()
        {
            Console.WriteLine("PHYSICAL LAYER");
            Console.WriteLine("Total Statistics\n----------------");
            Console.WriteLine(
                $"Qubits: {PhysicalLayerTracker.TotalQubits}\n" +
                $"Gate Count: {PhysicalLayerTracker.TotalGateCount}\n" +
                $"Time: {PhysicalLayerTracker.TotalTime}\n" +
                $"Error: { PhysicalLayerTracker.TotalError}\n");

            Console.WriteLine("Gate Statistics\n---------------");
            foreach(var item in PhysicalLayerTracker.GateStats)
            {
                Console.WriteLine(
                    $"{item.Key}:\n" +
                    $" - Count: {item.Value.Count}\n" +
                    $" - Time: {item.Value.Time}\n" +
                    $" - Error: {item.Value.Error}\n");
            }

        }

        // TODO: Add reference to paper in remarks.
        public override void R(Pauli axis, double theta, Qubit qubit)
        {
            void physicalRX(double t, Qubit q) => PhysicalLayerTracker.R(t, 0.0, q);
            void physicalRY(double t, Qubit q) => PhysicalLayerTracker.R(t, Math.PI / 2.0, q);
            Action<double, Qubit> physicalGate = axis switch
            {
                Pauli.PauliI => (t, q) => { },
                Pauli.PauliX => (t, q) => physicalRX(t, q),
                Pauli.PauliY => (t, q) => physicalRY(t, q),
                Pauli.PauliZ => (t, q) =>
                {
                    physicalRY(PlatformParameters.v * Math.PI, q);
                    physicalRX(PlatformParameters.v * t, q);
                    physicalRY(-PlatformParameters.v * Math.PI, q);
                },
                _ => throw new NotSupportedException($"{axis} is not supported")
            };

            physicalGate(theta, qubit);
        }

        public override void X(Qubit qubit)
        {
            R(Pauli.PauliX, Math.PI, qubit);

        }

        public override void Y(Qubit qubit)
        {
            R(Pauli.PauliY, Math.PI, qubit);
        }

        public override void Z(Qubit qubit)
        {
            R(Pauli.PauliZ, Math.PI, qubit);
        }

        public override void Reset(Qubit qubit)
        {
            // Do nothing.
        }
    }
    public class IonPlatformResourceEstimator : QuantumProcessorDispatcher
    {
        public IonPlatformResourceEstimator(
            double physicalRGateFidelity,
            double physicalRGateTime,
            double physicalXXGateFidelity,
            double physicalXXGateTime) :
            base(new IonPlatformResourceEstimatorProcessor(
                physicalRGateFidelity,
                physicalRGateTime,
                physicalXXGateFidelity,
                physicalXXGateTime))
        {
        }

        public void PrintPhysicalLayerStats()
        {
            var processor = (IonPlatformResourceEstimatorProcessor)QuantumProcessor;
            processor.PrintPhysicalLayerStats();
        }
    }
}
