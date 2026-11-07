using System;
using OpenQA.Selenium;

namespace Task1_ResilientAutomation.Core;

public class CandidateEvaluation
{
    public IWebElement Element { get; set; } = null!;
    public double TotalScore { get; set; }
    public string BestStrategy { get; set; } = string.Empty;
    public string ElementDescription { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

