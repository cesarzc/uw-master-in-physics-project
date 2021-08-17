namespace IonPlatformResourceEstimation {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.ErrorCorrection;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;

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
            Message($"Results: {results}");
            ResetAll(qubits);
            Reset(aux);
        }

        Message("Error Correction Experiments");
        let bitFlipCode = BitFlipCode();
        let (encode, decode, _) = bitFlipCode!;
        let bitFlipCodeRecoveryFn = BitFlipRecoveryFn();
        use data = Qubit();
        use encodeQubits = Qubit[2];
        let logicalRegister = encode!([data], encodeQubits);
        Message("Encoded");
        //LogicalX(logicalRegister);
        ApplyLogicalGate(X, logicalRegister);
        Recover(bitFlipCode, bitFlipCodeRecoveryFn, logicalRegister);
        let (decodedData, decodedQubits) = decode!(logicalRegister);
        Message("Decoded");
        let decodedResult = M(decodedData[0]);
        Message($"Decoded Result: {decodedResult}");
        Reset(data);
        ResetAll(encodeQubits);
        Message("Reset");
    }

    operation BernsteinVaziraniErrorCorrected() : Unit
    {
        Message("Bernstein-Vazirani (Error Corrected)");
        let secret = [One, Zero, One, One, One];
        let secretLength = Length(secret);
        use (dataQubits, dataAux, scratchQubits, scratchAux) =
            (Qubit[secretLength], Qubit(), Qubit[2 * secretLength], Qubit[2]) {

            // Encode.
            let partitionArray = ConstantArray(secretLength, 2);
            let scratchQubitsPartitions = Partitioned(partitionArray, scratchQubits);
            let bitFlipCode = BitFlipCode();
            let (encode, decode, _) = bitFlipCode!;
            mutable logicalDataRegisters = new LogicalRegister[secretLength];
            let dataEncodingPairs = Zipped(dataQubits, scratchQubitsPartitions);
            for index in IndexRange(logicalDataRegisters) {
                let (data, scratchData) = dataEncodingPairs[index];
                set logicalDataRegisters w/= index <- encode!([data], scratchData);
            }

            let logicalAuxRegister = encode!([dataAux], scratchAux);

            // Prepare.
            //X(aux);
            //H(aux);
            ApplyLogicalGate(X, logicalAuxRegister);
            ApplyLogicalGate(H, logicalAuxRegister);

            //ApplyToEach(H, qubits);
            for logicalDataRegister in logicalDataRegisters {
                ApplyLogicalGate(H, logicalDataRegister);
            }

            //// Oracle.
            for index in IndexRange(logicalDataRegisters) {
                if (secret[index] == One){
                    ApplyLogicalControlled(X, logicalDataRegisters[index], logicalAuxRegister);
                }
            }

            //ApplyToEach(H, qubits);
            for logicalDataRegister in logicalDataRegisters {
                ApplyLogicalGate(H, logicalDataRegister);
            }

            // Recover and measure.
            let bitFlipCodeRecoveryFn = BitFlipRecoveryFn();
            mutable decodedResults = new Result[secretLength];
            for index in IndexRange(logicalDataRegisters) {
                Recover(bitFlipCode, bitFlipCodeRecoveryFn, logicalDataRegisters[index]);
                let (decodedData, decodedQubits) = decode!(logicalDataRegisters[index]);
                set decodedResults w/= index <- M(decodedData[0]);
            }

            Reset(dataAux);
            ResetAll(scratchAux);
            ResetAll(dataQubits);
            ResetAll(scratchQubits);
            Message($"Secret: {decodedResults}");
        }
    }

    operation ApplyLogicalGate(gate : (Qubit => Unit), (logicalRegister : LogicalRegister)) : Unit
    {
        ApplyToEach(gate, logicalRegister!);
    }

    operation ApplyLogicalControlled(gate : (Qubit => Unit is Ctl), controlRegister : LogicalRegister, targetRegister : LogicalRegister) : Unit
    {
        let controlTargetPairs = Zipped(controlRegister!, targetRegister!);
        for (control, target) in controlTargetPairs {
            Controlled gate([control], target);
        }
    }
}
