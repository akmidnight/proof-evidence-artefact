# Claim Type: Peak Window Compliance

## Identifier

`PeakWindowCompliance`

## Purpose

Proves that peak load at a depot remained below a contractual threshold during
defined tariff windows. Used by fleet operators to demonstrate demand discipline
to energy suppliers during tariff negotiations.

## Metric

| Field | Value |
|-------|-------|
| `metricName` | `peak_kw` |
| `unit` | `kW` |
| `value` | Maximum observed load during the window |

## Inputs (aggregated, never raw)

- Observation period (`periodStart` .. `periodEnd`)
- Tariff window definitions (time-of-use schedule)
- Maximum 15-minute average load reading during each tariff window
- Contractual threshold (kW)

## Computation

1. For each tariff window in the period, compute the maximum 15-minute average load.
2. The claim `value` is the highest of these maxima.
3. Compliance is asserted when `value <= contractual threshold`.

## Baseline Reference

Not required for compliance claims. If a before/after comparison is needed,
combine with a `DemandChargeDeltaEstimate` artifact.

## Example Artifact Claim

```json
{
  "type": "PeakWindowCompliance",
  "metricName": "peak_kw",
  "value": 142.5,
  "unit": "kW",
  "baselineRef": null,
  "computationVersion": "1.0.0"
}
```
