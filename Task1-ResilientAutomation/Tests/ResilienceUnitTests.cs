using System;
using System.IO;
using NUnit.Framework;
using Task1_ResilientAutomation.Configuration;
using Task1_ResilientAutomation.Core;

namespace Task1_ResilientAutomation.Tests;

[TestFixture]
public class ResilienceUnitTests
{
    private string _fixturesDir = null!;

    [SetUp]
    public void Setup()
    {
        _fixturesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fixtures");
        if (!Directory.Exists(_fixturesDir))
        {
            _fixturesDir = Path.Combine(Directory.GetCurrentDirectory(), "Fixtures");
        }
    }

    [TestCase("original.html", "Original Baseline")]
    [TestCase("mutated_attributes.html", "Scrambled Attributes & IDs")]
    [TestCase("reordered_fields.html", "Reordered Fields")]
    [TestCase("table_layout.html", "Legacy Table Layout")]
    [TestCase("confusing_fields.html", "Confusing & Similar Fields")]
    public void TestResilienceAcrossMutations(string htmlFile, string scenarioName)
    {
        var filePath = Path.Combine(_fixturesDir, htmlFile);
        Assert.That(File.Exists(filePath), Is.True, $"Fixture file {htmlFile} must exist.");

        var settings = new AutomationSettings
        {
            Headless = true,
            TimeoutSeconds = 10,
            ActionDelayMs = 100,
            TakeScreenshots = false
        };

        var uri = new Uri(Path.GetFullPath(filePath)).AbsoluteUri;

        using var engine = new ResilientAutomationEngine(settings, TestContext.WriteLine);
        var report = engine.Execute(uri);

        Assert.That(report.AllPassed, Is.True, $"Scenario '{scenarioName}' ({htmlFile}) failed to self-heal all 3 fields.");
        Assert.That(report.FieldResults.Count, Is.EqualTo(3));
    }
}

