# Claim Type: Demand Charge Delta Estimate

## Identifier

`DemandChargeDeltaEstimate`

## Purpose

Proves that controlled charging reduced demand charge exposure relative to a
counterfactual baseline. Used by fleet operators to quantify savings in tariff
negotiations and by investors to assess utilization uplift.

## Metric

| Field | Value |
|-------|-------|
| `metricName` | `demand_charge_delta_pct` |
| `unit` | `%` |
| `value` | Percentage reduction vs baseline |

## Inputs (aggregated, never raw)

- Observation period (`periodStart` .. `periodEnd`)
- Baseline demand profile (lookback or counterfactual model)
- Actual demand profile (aggregated 15-minute intervals)

## Baseline Methodology

The baseline represents what demand would have been without controlled charging.
Two strategies are supported:

1. **Historical lookback**: average peak demand over a configurable lookback
   period (e.g. 30 days) before the optimization was active.
2. **Counterfactual model**: estimated uncontrolled load using session energy
   requirements and arrival/departure times, distributed without optimization.

The `baselineRef` field identifies which strategy and parameters were used.

## Computation

1. Compute the baseline peak demand `B` using the selected strategy.
2. Compute the actual peak demand `A` during the observation period.
3. `value = ((B - A) / B) * 100` (percentage reduction).

## Example Artifact Claim

```json
{
  "type": "DemandChargeDeltaEstimate",
  "metricName": "demand_charge_delta_pct",
  "value": 34.2,
  "unit": "%",
  "baselineRef": "lookback-30d-v1",
  "computationVersion": "1.0.0"
}
```
