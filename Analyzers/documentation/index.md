# GdUnit4Net Diagnostic Analyzer Overview

| Id                          | Severity | Title                                                   |
|-----------------------------|----------|---------------------------------------------------------|
| [GdUnit0201](GdUnit0201.md) | Error    | Multiple TestCase attributes not allowed with DataPoint |
| [GdUnit0202](GdUnit0202.md) | Error    | Test case names must be unique                          |
| [GdUnit0500](GdUnit0500.md) | Error    | Godot Runtime Required for Test Class                   |
| [GdUnit0501](GdUnit0501.md) | Error    | Godot Runtime Required for Test Method                  |

# Category Overview

All diagnostics in this analyzer focus on attribute usage and test configuration correctness:

## Attribute Usage

GdUnit0201: Ensures correct usage of TestCase attributes with DataPoint
GdUnit0202: Ensures uniqueness of TestCase names
GdUnit0501: Ensures proper test configuration for Godot engine dependencies

## General Guidelines

Use appropriate attributes for your test scenarios
Follow the correct testing patterns for different test types (standard vs Godot)
Pay attention to attribute combinations that might cause conflicts
