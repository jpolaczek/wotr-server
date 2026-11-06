#!/usr/bin/env python3
"""Merges the TRX files of the backend suite's parts into one, for the coverage scripts (scripts/test_parts.sh).

Usage: merge_trx.py <output> <expected parts> <part.trx>...
Fails if a part is missing, as an aborted test host writes none, or if a test appears in two parts.
"""
import sys
import xml.etree.ElementTree as ET

NAMESPACE = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
QUALIFIED = "{" + NAMESPACE + "}"
MERGED = ("TestDefinitions", "TestEntries", "Results")


def merge(parts):
    trees = [ET.parse(part).getroot() for part in parts]
    first = trees[0]
    seen = {result.attrib["testId"] for result in first.findall(QUALIFIED + "Results/" + QUALIFIED + "UnitTestResult")}
    for other in trees[1:]:
        for result in other.findall(QUALIFIED + "Results/" + QUALIFIED + "UnitTestResult"):
            if result.attrib["testId"] in seen:
                raise ValueError("A test ran in two parts: " + result.attrib.get("testName", result.attrib["testId"]))
            seen.add(result.attrib["testId"])
        for section in MERGED:
            first.find(QUALIFIED + section).extend(list(other.find(QUALIFIED + section)))
        add_counters(first, other)
    return first


def add_counters(into, other):
    counters = into.find(QUALIFIED + "ResultSummary/" + QUALIFIED + "Counters")
    more = other.find(QUALIFIED + "ResultSummary/" + QUALIFIED + "Counters")
    for name, value in more.attrib.items():
        counters.set(name, str(int(counters.attrib.get(name, "0")) + int(value)))
    outcome = other.find(QUALIFIED + "ResultSummary").attrib.get("outcome", "Failed")
    if outcome != "Completed":
        into.find(QUALIFIED + "ResultSummary").set("outcome", outcome)


def main(arguments):
    output, expected, parts = arguments[0], int(arguments[1]), arguments[2:]
    if len(parts) != expected:
        print(f"Expected {expected} parts and found {len(parts)}: a test host was aborted.", file=sys.stderr)
        return 1
    ET.register_namespace("", NAMESPACE)
    merged = merge(parts)
    ET.ElementTree(merged).write(output, encoding="utf-8", xml_declaration=True)
    counters = merged.find(QUALIFIED + "ResultSummary/" + QUALIFIED + "Counters").attrib
    print(f"Merged {len(parts)} parts: {counters['total']} tests, {counters['passed']} passed, {counters['failed']} failed.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
