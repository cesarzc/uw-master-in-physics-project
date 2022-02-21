using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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

            public static Option Resource => new Option<string>(aliases: new string[] { "--resource"})
            {
                Description = "Resource to track.",
                IsRequired = true
            };
        }

        class FlameGraphOptions
        {
            public int N { get; set; }

            public int Generator { get; set; }

            public string Resource { get; set; }
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

            // Create flame graph command.
            var flameGraphCommand = new Command("flamegraph");
            flameGraphCommand.Add(CommandOption.N);
            flameGraphCommand.Add(CommandOption.Generator);
            flameGraphCommand.Add(CommandOption.Resource);
            flameGraphCommand.Handler = CommandHandler.Create((FlameGraphOptions options) =>
                FlameGraph(options.N, options.Generator, options.Resource));
            rootCommand.Add(flameGraphCommand);

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
            Console.WriteLine("Simulation ended");
            Console.WriteLine(estimator.ToTSV());
        }

        static void FlameGraph(int N, int generator, string resource)
        {
            Console.WriteLine($"Flame graph for estimate period with N={N} and generator={generator}...");

            PrimitiveOperationsGroups StringToPrimitiveOperationsGroup(string str) =>
                str switch
                {
                    "CNOT" => PrimitiveOperationsGroups.CNOT,
                    "QubitClifford" => PrimitiveOperationsGroups.QubitClifford,
                    "R" => PrimitiveOperationsGroups.R,
                    "Measure" => PrimitiveOperationsGroups.Measure,
                    "T" => PrimitiveOperationsGroups.T,
                    _ => throw new ArgumentException($"'{str}' is an invalid string for a primitive operations group.")
                };

            var config = FlameGraphResourcesEstimator.RecommendedConfig();
            var resourceToVisualize = StringToPrimitiveOperationsGroup(resource);
            var flameGraphEstimator = new FlameGraphResourcesEstimator(config, resourceToVisualize);
            EstimatePeriodInstance.Run(flameGraphEstimator, N, generator).Wait();
            Console.WriteLine("Simulation ended");
            var flameGraphFormatStr = string.Join(
                System.Environment.NewLine,
                flameGraphEstimator.FlameGraphData.Select(pair => $"{pair.Key} {pair.Value}"));

            //
            var flameGraphDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Stats");
            var flameGraphDataFilePath = Path.Combine(flameGraphDirectoryPath, "FlameGraph.txt");
            Console.WriteLine($"Saving flame graph data to {flameGraphDataFilePath}...");
            using var fileStream = File.CreateText(flameGraphDataFilePath);
            fileStream.Write(flameGraphFormatStr);
            fileStream.Flush();

            //
            var flameGraphSvgFilePath = Path.Combine(flameGraphDirectoryPath, "FlameGraph.svg");
            var flameGraphScriptPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "flamegraph.pl");
            var flameGraphScriptArgs = $"{flameGraphScriptPath} {flameGraphDataFilePath}".Replace("\\", "\\\\");
            var flameGraphScriptProcess = new System.Diagnostics.Process();
            flameGraphScriptProcess.StartInfo = new ProcessStartInfo("perl")
            { 
                Arguments = flameGraphScriptArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Console.WriteLine($"Generating flame graph visualization to {flameGraphSvgFilePath}...");
            flameGraphScriptProcess.Start();
            var svg = flameGraphScriptProcess.StandardOutput.ReadToEnd();
            using var svgStream = File.CreateText(flameGraphSvgFilePath);
            svgStream.Write(svg);
            svgStream.Flush();

            //
            var visualizationProcess = new System.Diagnostics.Process();
            visualizationProcess.StartInfo = new ProcessStartInfo(flameGraphSvgFilePath)
            { 
                UseShellExecute = true 
            };
            Console.WriteLine($"Openening visualization {flameGraphSvgFilePath}...");
            visualizationProcess.Start();
        }

        static void QcTrace(int N, int generator)
        {
            Console.WriteLine($"QC trace for estimate period with N={N} and generator={generator}...");

            //
            QCTraceSimulatorConfiguration tracerCoreConfiguration = new QCTraceSimulatorConfiguration();
            tracerCoreConfiguration.ThrowOnUnconstrainedMeasurement = false;
            tracerCoreConfiguration.UseDepthCounter = true;
            tracerCoreConfiguration.UseWidthCounter = true;
            var qcTraceSimulator = new QCTraceSimulator(tracerCoreConfiguration);
            EstimatePeriodInstance.Run(qcTraceSimulator, N, generator).Wait();
            Console.WriteLine("Simulation ended");

            //
            var csv = qcTraceSimulator.ToCSV(format: "G");
            var qcTraceStatsDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Stats");
            foreach (var item in csv)
            {
                var statsFilePath = Path.Combine(qcTraceStatsDirectoryPath, $"{item.Key}.csv");
                var fileStream = File.CreateText(statsFilePath);
                Console.WriteLine(item.Key);
                var commaSeparated = item.Value.Replace("\t", ",");
                fileStream.Write(commaSeparated);
                fileStream.Flush();
                fileStream.Close();
                Console.WriteLine($"Stats saved to {statsFilePath}");
            }
        }

        static void Simulate(int N)
        {
            Console.WriteLine($"Factoring semiprime integer {N} using a full state simulator...");
            using QuantumSimulator sim = new QuantumSimulator();
            (long factor1, long factor2) = FactorSemiprimeInteger.Run(sim, N).Result;
            Console.WriteLine("Simulation ended");
            Console.WriteLine($"Factors are {factor1} and {factor2}");
        }

        static void TrackLogicalResources(int N, int generator)
        {
            Console.WriteLine($"Tracking logical resources for estimate period with N={N} and generator={generator}...");
            LogicalTracker logicalTracker = new LogicalTracker();
            EstimatePeriodInstance.Run(logicalTracker, N, generator).Wait();
            Console.WriteLine("Simulation ended");
            logicalTracker.DisplayStats();
        }

        // TODO: Maybe take depth as a parameter too.
        static void Visualize(int N, int generator)
        {
            Console.WriteLine($"Generating visualization for estimate period with N={N} and generator={generator}...");

            //
            var config = QuantumVizEstimator.RecommendedConfig();
            var visualizationGenerator = new QuantumVizEstimator(config);
            EstimatePeriodInstance.Run(visualizationGenerator, N, generator).Wait();
            Console.WriteLine("Simulation ended");
            var jsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Visualization", "circuit.js");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(visualizationGenerator.Circuit);

            //
            using var fileStream = File.CreateText(jsFilePath);
            fileStream.Write($"const shorCircuit = {json};");
            fileStream.Flush();
            Console.WriteLine($"Visualization generated to {jsFilePath}");

            //
            var htmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Visualization", "index.html");
            Console.WriteLine(htmlFilePath);
            var visualizationProcess = new System.Diagnostics.Process();
            visualizationProcess.StartInfo = new ProcessStartInfo(htmlFilePath)
            { 
                UseShellExecute = true 
            };
            Console.WriteLine($"Opening visualization {htmlFilePath}...");
            visualizationProcess.Start();
        }
    }
}
