using System.Collections.Generic;

namespace IonPlatformResourceEstimation
{
    public interface IPhysicalLayerStats
    {
        IDictionary<string, (int Count, double Error, double Time)> GateStats { get; }

        int TotalGateCount { get; }

        double TotalError { get; }
        int TotalQubits { get; }

        double TotalTime { get; }
    }
}
