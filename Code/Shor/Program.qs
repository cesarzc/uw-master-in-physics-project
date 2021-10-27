namespace Shor {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Random;
    
    @EntryPoint()
    operation Shor(N : Int) : (Int, Int) {
        Message("Shor integer factorization algorithm");

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

    @EntryPoint()
    operation EstimatePeriod(N : Int, a : Int) : Int {
        // TODO: Maybe use multiply by modular integer.

        return 0;
    }

    function CalculateFactorsFromPeriod(N : Int, a : Int, period : Int) : (Bool, (Int, Int))
    {
        if (period % 2 != 0) {
            return (false, (1, 1));
        }

        // Calculate x = [a ^ (period / 2)] mod N.
        let x = ExpModI(a, period / 2, N);
        // TODO: Keep implementing.
        return (true, (1, 1));
    }
}
