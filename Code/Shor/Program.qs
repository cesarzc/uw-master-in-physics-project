namespace Shor {

    open Microsoft.Quantum.Arithmetic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Characterization;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Oracles;
    open Microsoft.Quantum.Random;

    operation FactorSemiprimeInteger(N : Int) : (Int, Int) {
        Message("Shor's integer factorization algorithm");

        // Check the most trivial case where N is pair.
        if (N % 2 == 0) {
            return (2, N / 2);
        }

        mutable cycleCount = 1;
        mutable factors = (1, 1);
        mutable foundFactors = false;
        repeat {
            // Start by guessing a coprime to N.
            let coprimeGuess = DrawRandomInt(1, N - 1);
            Message($"Starting cycle {cycleCount} using {coprimeGuess} as a coprime guess...");

            // If the guess number is a coprime, use a quantum algorithm for period finding.
            // Otherwise, the GCD between N and the coprime guess number is one of the factors.
            if (IsCoprimeI(N, coprimeGuess)) {
                Message("Estimating period using a quantum algorithm...");
                let period = EstimatePeriod(N, coprimeGuess);
                Message($"Estimated period: {period}");
                set (foundFactors, factors) = CalculateFactorsFromPeriod(N, coprimeGuess, period);
            } else {
                Message($"{N} and {coprimeGuess} are not coprimes. Their GCD is a factor.");
                let gcd = GreatestCommonDivisorI(N, coprimeGuess);
                set (foundFactors, factors) = (true, (gcd, N / gcd));
            }
        }
        until foundFactors
        fixup {
            Message($"Factors not found using {coprimeGuess} as a coprime guess.");
            set cycleCount = cycleCount + 1;
        }

        return factors;
    }

    operation EstimatePeriodInstance(N : Int, a : Int) : (Bool, Int) {

        // Prepare eigenstate register.
        let bitSize = BitSizeI(N);
        use eigenstateRegister = Qubit[bitSize];
        let eigenstateRegisterLE = LittleEndian(eigenstateRegister);
        ApplyXorInPlace(1, eigenstateRegisterLE);

        // Prepare phase register.
        let bitsPrecision =  2 * bitSize + 1;
        use phaseRegister = Qubit[bitsPrecision];
        let phaseRegisterLE = LittleEndian(phaseRegister);

        // Prepare oracle for quantum phase estimation.
        let oracle = DiscreteOracle(ApplyOrderFindingOracle(N, a, _, _));

        // Execute quantum phase estimation.
        // TODO(?): Ask why it is encoded as little endian at the beginning and not as big endian since the beginning.
        QuantumPhaseEstimation(oracle, eigenstateRegisterLE!, LittleEndianAsBigEndian(phaseRegisterLE));
        // TODO(?): Why the phase estimate is different even for the same values of N & a?
        let phaseEstimate = MeasureInteger(phaseRegisterLE);
        Message($"Estimated phase: {phaseEstimate}");

        // Reset qubit registers 
        ResetAll(eigenstateRegister);
        // TODO(?): Should the phase register be reset?
        //ResetAll(phaseRegister);

        // Return period calculation based on estimated phase.
        return CalculatePeriodFromPhaseEstimate(N, bitsPrecision, phaseEstimate);
    }

    operation EstimatePeriod(N : Int, a : Int) : Int {
        mutable cycleCount = 1;
        mutable period = 1;
        // TODO(?): Ask why this cycle is needed. Robustness against noise?
        repeat {
            Message($"Starting estimate period cycle {cycleCount}");
            let (instanceSucceeded, instancePeriod) = EstimatePeriodInstance(N, a);
            if (instanceSucceeded) {
                set period = instancePeriod;
            }

            set cycleCount = cycleCount + 1;
        }
        until (ExpModI(a, period, N) == 1)
        fixup {
            Message($"Period estimate failed (period = {period}).");
        }

        return period;
    }

    operation ApplyOrderFindingOracle(
        N : Int, a : Int, power : Int, target : Qubit[]
    )
    : Unit
    is Adj + Ctl {
        MultiplyByModularInteger(ExpModI(a, power, N), N, LittleEndian(target));
    }

    function CalculateFactorsFromPeriod(N : Int, a : Int, period : Int) : (Bool, (Int, Int))
    {
        // TODO: Explain.
        if (period % 2 != 0) {
            return (false, (1, 1));
        }

        // TODO: Explain.
        let halfPower = ExpModI(a, period / 2, N);
        if (halfPower + 1 == N)
        {
            return (false, (1, 1));
        }

        let factor = MaxI(GreatestCommonDivisorI(halfPower - 1, N), GreatestCommonDivisorI(halfPower + 1, N));
        return (true, (factor, N / factor));
    }

    // TODO: Document the expected form of the phase estimate.
    function CalculatePeriodFromPhaseEstimate(N : Int, bitsPrecision : Int, phaseEstimate : Int) : (Bool, Int)
    {
        if (phaseEstimate == 0) {
            return (false, 1);
        }

        let (_, period) = (ContinuedFractionConvergentI(Fraction(phaseEstimate, 2 ^ bitsPrecision), N))!;
        // TODO(?): Ask why in the samples implementation additional math is done to get the period.
        return (true, AbsI(period));
    }
}
