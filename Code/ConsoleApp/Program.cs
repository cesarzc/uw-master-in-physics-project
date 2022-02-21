using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;

using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Simulators.QCTraceSimulators;

using Tracking;
using Shor;

namespace ConsoleApp
{
    class Program
    {
        static class CommandOption
        {
            public static Option N => new Option<int>(aliases: new string[] { "-N"})
            {
                Description = "Semiprime integer to factor.",
                IsRequired = true
            };

            public static Option Generator => new Option<int>(aliases: new string[] { "--generator"})
            {
                Description = "Generator used to estimate the period.",
                IsRequired = true
            };
        }
        class QuantumSubroutineOptions
        {
            public int N { get; set; }

            public int Generator { get; set; }
        }

        class SimulateOptions
        {
            public int N { get; set; }
        }

        static int Main(string[] args)
        {
            Console.WriteLine("Console App");
            var rootCommand = new RootCommand();

            // Create estimate logical resources command.
            var estimateLogicalResourcesCommand = new Command("estimatelogicalresources");
            estimateLogicalResourcesCommand.AddOption(CommandOption.N);
            estimateLogicalResourcesCommand.AddOption(CommandOption.Generator);
            estimateLogicalResourcesCommand.Handler = CommandHandler.Create((QuantumSubroutineOptions options) =>
                EstimateLogicalResources(options.N, options.Generator));
            rootCommand.Add(estimateLogicalResourcesCommand);

            // Create QC trace command.
            var qcTraceCommand = new Command("qctrace");
            qcTraceCommand.AddOption(CommandOption.N);
            qcTraceCommand.AddOption(CommandOption.Generator);
            qcTraceCommand.Handler = CommandHandler.Create((QuantumSubroutineOptions options) =>
                QcTrace(options.N, options.Generator));
            rootCommand.Add(qcTraceCommand);

            // Create simulate command.
            var simulateCommand = new Command("simulate");
            simulateCommand.AddOption(CommandOption.N);
            simulateCommand.Handler = CommandHandler.Create((SimulateOptions options) => Simulate(options.N));
            rootCommand.Add(simulateCommand);

            // Create track logical operations commnad.
            var trackLogicalOperationsCommand = new Command("tracklogicalresources");
            trackLogicalOperationsCommand.AddOption(CommandOption.N);
            trackLogicalOperationsCommand.AddOption(CommandOption.Generator);
            trackLogicalOperationsCommand.Handler = CommandHandler.Create((QuantumSubroutineOptions options) => 
                TrackLogicalResources(options.N, options.Generator));
            rootCommand.Add(trackLogicalOperationsCommand);

            // Create visualize command.
            var visualizeCommand = new Command("visualize");
            visualizeCommand.AddOption(CommandOption.N);
            visualizeCommand.AddOption(CommandOption.Generator);
            visualizeCommand.Handler = CommandHandler.Create((QuantumSubroutineOptions options) =>
                Visualize(options.N, options.Generator));
            rootCommand.Add(visualizeCommand);

            // Invoke root command to handle command line arguments.
            rootCommand.Invoke(args);
            return 0;
        }

        static void EstimateLogicalResources(int N, int generator)
        {
            Console.WriteLine($"Calculating logical resources for estimate period with N={N} and generator={generator}...");
            var config = ResourcesEstimator.RecommendedConfig();
            config.CallStackDepthLimit = 3;
            var estimator = new ResourcesEstimator(config);
            EstimatePeriodInstance.Run(estimator, N, generator).Wait();
            Console.WriteLine(estimator.ToTSV());
        }

        static void QcTrace(int N, int generator)
        {
            Console.WriteLine($"QC trace for estimate period with N={N} and generator={generator}...");
            QCTraceSimulatorConfiguration tracerCoreConfiguration = new QCTraceSimulatorConfiguration();
            tracerCoreConfiguration.ThrowOnUnconstrainedMeasurement = false;
            tracerCoreConfiguration.UseDepthCounter = true;
            tracerCoreConfiguration.UseWidthCounter = true;
            var qcTraceSimulator = new QCTraceSimulator(tracerCoreConfiguration);
            EstimatePeriodInstance.Run(qcTraceSimulator, N, generator).Wait();
            var csv = qcTraceSimulator.ToCSV(format: "G");
            var qcTraceStatsPath = Path.Combine(Directory.GetCurrentDirectory(), "Stats");
            foreach (var item in csv)
            {
                var fileStream = File.CreateText(Path.Combine(qcTraceStatsPath, $"{item.Key}.csv"));
                Console.WriteLine(item.Key);
                var commaSeparated = item.Value.Replace("\t", ",");
                fileStream.Write(commaSeparated);
                fileStream.Flush();
                fileStream.Close();
            }
        }

        static void Simulate(int N)
        {
            Console.WriteLine($"Factoring semiprime integer {N} using a full state simulator...");
            using QuantumSimulator sim = new QuantumSimulator();
            (long factor1, long factor2) = FactorSemiprimeInteger.Run(sim, N).Result;
            Console.WriteLine($"Factors are {factor1} and {factor2}");
        }

        static void TrackLogicalResources(int N, int generator)
        {
            Console.WriteLine($"Tracking logical resources for estimate period with N={N} and generator={generator}...");
            LogicalTracker logicalTracker = new LogicalTracker();
            EstimatePeriodInstance.Run(logicalTracker, N, generator).Wait();
            logicalTracker.DisplayStats();
        }

        // TODO: Maybe take depth as a parameter too.
        static void Visualize(int N, int generator)
        {
            Console.WriteLine($"Generating visualization for estimate period with N={N} and generator={generator}...");
            var config = QuantumVizEstimator.RecommendedConfig();
            var visualizationGenerator = new QuantumVizEstimator(config);
            EstimatePeriodInstance.Run(visualizationGenerator, N, generator).Wait();
            var jsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Visualization", "circuit.js");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(visualizationGenerator.Circuit);
            using var fileStream = File.CreateText(jsFilePath);
            fileStream.Write($"const shorCircuit = {json};");
            fileStream.Flush();
            Console.WriteLine("Visualization generated.");
            var htmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Visualization", "index.html");
            Console.WriteLine(htmlFilePath);
            var p = new System.Diagnostics.Process();
            p.StartInfo = new ProcessStartInfo(htmlFilePath)
            { 
                UseShellExecute = true 
            };
            p.Start();
        }
    }
}
