using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Task1_ResilientAutomation.Configuration;
using Task1_ResilientAutomation.Core;

namespace Task1_ResilientAutomation;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════════════════╗
║             CloudQA Self-Healing Resilient Automation                 ║
║   Field Automation: First Name (text), Gender (radio), State (select)  ║
╚════════════════════════════════════════════════════════════════════════╝");

        // Build configuration from appsettings.json, environment variables, and CLI args
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("CLOUDAUTOMATION_")
            .AddCommandLine(args);

        var config = builder.Build();
        var settings = new AutomationSettings();
        config.GetSection("AutomationSettings").Bind(settings);

        // CLI argument overrides
        bool pauseAtEnd = false;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();
            if ((arg == "--url" || arg == "-u") && i + 1 < args.Length)
            {
                settings.PageUrl = args[++i];
            }
            else if (arg == "--headless")
            {
                settings.Headless = true;
            }
            else if (arg == "--visible")
            {
                settings.Headless = false;
            }
            else if ((arg == "--firstname" || arg == "-fn") && i + 1 < args.Length)
            {
                settings.Values.FirstName = args[++i];
            }
            else if ((arg == "--gender" || arg == "-g") && i + 1 < args.Length)
            {
                settings.Values.Gender = args[++i];
            }
            else if ((arg == "--state" || arg == "-s") && i + 1 < args.Length)
            {
                settings.Values.State = args[++i];
            }
            else if (arg == "--pause" || arg == "--stay-open")
            {
                pauseAtEnd = true;
            }
        }

        bool runMutations = args.Any(a => a.Equals("--test-mutations", StringComparison.OrdinalIgnoreCase));

        if (runMutations)
        {
            return RunMutationSuite(settings);
        }

        // Standard Run against target URL
        Console.WriteLine($"[Config] Target URL   : {settings.PageUrl}");
        Console.WriteLine($"[Config] Headless Mode: {settings.Headless}");
        Console.WriteLine($"[Config] Values       : First Name='{settings.Values.FirstName}', Gender='{settings.Values.Gender}', State='{settings.Values.State}'\n");

        using var engine = new ResilientAutomationEngine(settings, Console.WriteLine);
        var report = engine.Execute();

        if (report.AllPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[RESULT] AUTOMATION COMPLETED SUCCESSFULLY! All 3 fields identified and populated.");
            Console.ResetColor();

            // If running with visible browser, provide observation time for recording
            if (!settings.Headless)
            {
                if (pauseAtEnd)
                {
                    Console.WriteLine("\n[Demo] Browser is paused. Press ENTER in this console to close the browser...");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("\n[Demo] Keeping browser open for 6 seconds for visual confirmation...");
                    for (int sec = 6; sec > 0; sec--)
                    {
                        Console.Write($"\rClosing browser in {sec}s... (Tip: use --pause to keep open)   ");
                        System.Threading.Thread.Sleep(1000);
                    }
                    Console.WriteLine();
                }
            }

            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[RESULT] AUTOMATION ENCOUNTERED FAILURES.");
            Console.ResetColor();

            if (!settings.Headless)
            {
                Console.WriteLine("\nPress ENTER to close the browser...");
                Console.ReadLine();
            }

            return 1;
        }
    }

    private static int RunMutationSuite(AutomationSettings baseSettings)
    {
        Console.WriteLine("\n=======================================================");
        Console.WriteLine("    RUNNING RESILIENCE MUTATION VERIFICATION SUITE     ");
        Console.WriteLine("=======================================================");
        Console.WriteLine("Testing the resilient locator engine against 5 structural permutations:");
        Console.WriteLine("1. Original Baseline (CloudQA structure)");
        Console.WriteLine("2. Scrambled Attributes & IDs (Randomized IDs/names, stripped classes)");
        Console.WriteLine("3. Reordered Fields (State first, Gender second, First Name last)");
        Console.WriteLine("4. Legacy Table Layout (No label 'for' attributes)");
        Console.WriteLine("5. Confusing & Similar Decoy Fields (Spouse First Name, Last Name, State of Mind)");
        Console.WriteLine("=======================================================\n");

        var fixtureDir = Path.Combine(Directory.GetCurrentDirectory(), "Fixtures");
        var fixtures = new[]
        {
            ("Original Baseline", "original.html"),
            ("Scrambled Attributes", "mutated_attributes.html"),
            ("Reordered Fields", "reordered_fields.html"),
            ("Legacy Table Layout", "table_layout.html"),
            ("Confusing Decoy Fields", "confusing_fields.html")
        };

        int passedCount = 0;
        baseSettings.Headless = true; // Run mutation suite swiftly in headless mode

        using var engine = new ResilientAutomationEngine(baseSettings, s => { /* quiet logger during mutation suite */ });

        foreach (var (title, file) in fixtures)
        {
            var fullPath = Path.Combine(fixtureDir, file);
            var fileUri = new Uri(fullPath).AbsoluteUri;

            Console.Write($"Testing [{title,-25}] ... ");
            try
            {
                var report = engine.Execute(fileUri);
                if (report.AllPassed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ PASSED (Self-Healed 3/3 Fields)");
                    Console.ResetColor();
                    passedCount++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ FAILED");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ ERROR: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n=======================================================");
        Console.WriteLine($"MUTATION SUITE SUMMARY: {passedCount}/{fixtures.Length} Passed ({(double)passedCount / fixtures.Length * 100:F0}%)");
        Console.WriteLine("=======================================================\n");

        return passedCount == fixtures.Length ? 0 : 1;
    }
}
