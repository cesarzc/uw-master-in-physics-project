using Microsoft.Quantum.Simulation.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IonPlatformResourceEstimation
{
    internal class IonPlatformPhysicalLayerTracker : IPhysicalLayerStats
    {
        public IDictionary<string, (int Count, double Error, double Time)> GateStats => GateTracking;

        public int TotalGateCount
        {
            get => GateStats.Values.Select(tuple => tuple.Count).Aggregate(0, (total, count) => total += count);
        }

        public double TotalError
        {
            get => GateStats.Values.Select(tuple => tuple.Error).Aggregate(0.0, (total, error) => total += error);
        }

        public int TotalQubits { get; private set; }

        public double TotalTime
        {
            get => GateStats.Values.Select(tuple => tuple.Time).Aggregate(0.0, (total, time) => total += time);
        }

        private readonly Dictionary<string, (int Count, double Error, double Time)> GateTracking;

        private readonly PhysicalRGate RGate;

        private readonly PhysicalXXGate XXGate;

        public IonPlatformPhysicalLayerTracker(
            double physicalRGateFidelity,
            double physicalRGateTime,
            double physicalXXGateFidelity,
            double physicalXXGateTime)
        {
            TotalQubits = 0;
            GateTracking = new Dictionary<string, (int Count, double Error, double Time)>()
            {
                { "R", (0, 0.0, 0.0)},
                { "XX", (0, 0.0, 0.0)}
            };

            RGate = new PhysicalRGate(physicalRGateFidelity, physicalRGateTime);
            XXGate = new PhysicalXXGate(physicalXXGateFidelity, physicalXXGateTime);
        }

        public void AllocateQubit(Qubit qubit) => TotalQubits++;

        public void R(double theta, double phi, Qubit qubit)
        {
            (var gateCount, var gateError, var gateTime) = GateTracking["R"];
            gateCount++;
            gateError += RGate.ErrorCost(theta, phi);
            gateTime += RGate.TimeCost(theta, phi);
            GateTracking["R"] = (gateCount, gateError, gateTime);
        }

        public void XX(double chi, Qubit a, Qubit b)
        {
            (var gateCount, var gateError, var gateTime) = GateTracking["XX"];
            gateCount++;
            gateError += XXGate.ErrorCost(chi);
            gateTime += XXGate.TimeCost(chi);
            GateTracking["XX"] = (gateCount, gateError, gateTime);
        }
    }

    internal class PhysicalRGate : IPhysicalGateProperties
    {
        public double Fidelity { get; private set; }

        public double Time { get; private set; }

        public PhysicalRGate(double fidelity, double time)
        {
            (Fidelity, Time) = (fidelity, time);
        }

        public double ErrorCost(double theta, double phi) => Math.Abs(Math.Sin(theta)) * (1.0 - Fidelity);

        public double TimeCost(double theta, double phi) => Math.Abs(theta) / Math.PI * Time;
    }

    internal class PhysicalXXGate : IPhysicalGateProperties
    {
        public double Fidelity { get; private set; }

        public double Time { get; private set; }

        public PhysicalXXGate(double fidelity, double time)
        {
            (Fidelity, Time) = (fidelity, time);
        }

        public double ErrorCost(double chi) => Math.Abs(Math.Sin(2 * chi)) * (1.0 - Fidelity);

        public double TimeCost(double chi) => Time;
    }
}
