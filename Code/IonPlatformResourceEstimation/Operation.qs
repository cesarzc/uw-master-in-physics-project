namespace IonPlatformResourceEstimation {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;

    operation BernsteinVazirani () : Unit {
        Message("Bernstein-Vazirani");
        let secret = [One, Zero, One, One, Zero];
        use (qubits, aux) = (Qubit[Length(secret)], Qubit()) {
            X(aux);
            H(aux);
            ApplyToEach(H, qubits);

            // Oracle.
            for index in 0 .. Length(qubits) - 1 {
                if (secret[index] == One){
                    CNOT(qubits[index], aux);
                }
            }

            ApplyToEach(H, qubits);
            let results = ForEach(M, qubits);
            ResetAll(qubits);
            Reset(aux);
        }
    }
}
