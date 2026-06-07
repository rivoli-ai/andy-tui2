// Widget tests assert default colors seeded from the global ambient ThemeContext.
// ThemeContextWidgetTests mutates that global, so running test classes in parallel
// could let one class observe another's theme change mid-test. Serialize the assembly.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
