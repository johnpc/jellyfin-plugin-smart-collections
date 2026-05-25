#!/bin/bash
# CRAP Score Analysis
# Formula: CRAP = complexity^2 * (1 - coverage)^3 + complexity
# Threshold: 15
#
# Reads Coverlet JSON coverage output and computes CRAP per method.
# Requires: coverage/coverage.json (produced by dotnet test with Coverlet)

set -e

COVERAGE_FILE="${1:-coverage/coverage.json}"
THRESHOLD=15

if [ ! -f "$COVERAGE_FILE" ]; then
    echo "ERROR: Coverage file not found at $COVERAGE_FILE"
    echo "Run: dotnet test --collect:'XPlat Code Coverage' --results-directory coverage first"
    exit 1
fi

python3 << 'PYTHON'
import json
import sys
import os

threshold = 15
coverage_file = os.environ.get("COVERAGE_FILE", "coverage/coverage.json")

try:
    with open(coverage_file) as f:
        data = json.load(f)
except (FileNotFoundError, json.JSONDecodeError) as e:
    print(f"ERROR: Could not read coverage file: {e}")
    sys.exit(1)

violations = []
total_methods = 0

for module_name, module_data in data.items():
    for class_name, class_data in module_data.items():
        for method_name, method_data in class_data.items():
            total_methods += 1

            # Extract line coverage for this method
            lines = method_data.get("Lines", {})
            if not lines:
                continue

            total_lines = len(lines)
            covered_lines = sum(1 for hits in lines.values() if hits > 0)
            coverage = covered_lines / total_lines if total_lines > 0 else 0

            # Estimate cyclomatic complexity from branch data
            # Each branch point adds 1 to complexity (base = 1)
            branches = method_data.get("Branches", [])
            complexity = 1 + len(set((b.get("Line", 0), b.get("Offset", 0)) for b in branches))

            # CRAP = complexity^2 * (1 - coverage)^3 + complexity
            crap = (complexity ** 2) * ((1 - coverage) ** 3) + complexity

            if crap > threshold:
                violations.append({
                    "class": class_name,
                    "method": method_name,
                    "crap": round(crap, 2),
                    "complexity": complexity,
                    "coverage": round(coverage * 100, 1),
                })

print(f"\nCRAP Score Analysis (threshold: {threshold})")
print(f"{'=' * 60}")
print(f"Methods analyzed: {total_methods}")

if violations:
    print(f"\n{len(violations)} method(s) exceed CRAP threshold of {threshold}:\n")
    for v in sorted(violations, key=lambda x: x["crap"], reverse=True):
        print(f"  CRAP={v['crap']:>6.1f}  complexity={v['complexity']:>2}  "
              f"coverage={v['coverage']:>5.1f}%  {v['class']}.{v['method']}")
    print()
    sys.exit(1)
else:
    print(f"\nAll methods pass CRAP threshold of {threshold}")
    sys.exit(0)
PYTHON
