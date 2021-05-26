namespace Quantum.BernsteinVazirani {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;

    function ArrayToString<'T> (array : 'T[]) : String
    {
        mutable first = true;
        mutable itemsString = "[";
        for item in array
        {
            if (first)
            {
                set first = false;
                set itemsString = itemsString + $"{item}";
            }
            else
            {
                set itemsString = itemsString + $", {item}";
            }
        }

        set itemsString = itemsString + "]";
        return itemsString;
    }

    @EntryPoint()
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
            Message(ArrayToString<Result>(results));
            ResetAll(qubits);
            Reset(aux);
        }
    }
}
