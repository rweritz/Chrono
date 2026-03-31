# Arithmetic Operations

The `TimeSeriesMath` static class provides element-wise arithmetic between two time series, as well as scalar operations. All operations return a **new** series — inputs are never mutated.

## Binary Operations

Combine two series of the same type and period, point by point:

```csharp
var a = new FixedSlotTimeSeries<double>(Period.Hour);
var b = new FixedSlotTimeSeries<double>(Period.Hour);

var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
var t1 = t0.AddHours(1);

a[t0] = 10; a[t1] = 20;
b[t0] = 3;  b[t1] = 7;

var sum  = TimeSeriesMath.Add(a, b);       // [13, 27]
var diff = TimeSeriesMath.Subtract(a, b);  // [7, 13]
var prod = TimeSeriesMath.Multiply(a, b);  // [30, 140]
var quot = TimeSeriesMath.Divide(a, b);    // [3.33..., 2.857...]
```

### Available Operations

| Method | Operation |
|---|---|
| `TimeSeriesMath.Add(left, right)` | `left + right` |
| `TimeSeriesMath.Subtract(left, right)` | `left - right` |
| `TimeSeriesMath.Multiply(left, right)` | `left * right` |
| `TimeSeriesMath.Divide(left, right)` | `left / right` |

All binary operations require both series to have the **same `Period`**. Mismatched periods throw `InvalidOperationException`.

## Scalar Operations

Apply a scalar to every value in a series:

```csharp
var series = new FixedSlotTimeSeries<double>(Period.FiveMinutes);
var t = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
series[t] = 10;
series[t.AddMinutes(5)] = 20;

var doubled  = TimeSeriesMath.Multiply(series, 2.0);  // [20, 40]
var shifted  = TimeSeriesMath.Add(series, 5.0);        // [15, 25]
var halved   = TimeSeriesMath.Divide(series, 2.0);     // [5, 10]
```

| Method | Operation |
|---|---|
| `TimeSeriesMath.Multiply(series, scalar)` | Each value × scalar |
| `TimeSeriesMath.Add(series, scalar)` | Each value + scalar |
| `TimeSeriesMath.Divide(series, scalar)` | Each value ÷ scalar |

Scalar operations work on `FixedSlotTimeSeries<T>`, `SortedArrayTimeSeries<T>`, and `DynamicSlotTimeSeries<T>`.

## Missing Value Policy

When two series don't have values at exactly the same timestamps, the `MissingValuePolicy` controls behavior:

```csharp
public enum MissingValuePolicy
{
    Intersection,    // Only compute where BOTH series have values (default)
    UnionWithZero,   // Compute everywhere, treating missing values as zero
    Throw            // Throw if either series is missing a value
}
```

### Intersection (Default)

Only timestamps where **both** series have a value produce a result. Other timestamps are omitted:

```csharp
var a = new SortedArrayTimeSeries<double>(Period.Hour);
var b = new SortedArrayTimeSeries<double>(Period.Hour);

a[t0] = 10;  a[t1] = 20;  // a has t0, t1
b[t1] = 4;   b[t2] = 8;   // b has t1, t2

var result = TimeSeriesMath.Add(a, b, MissingValuePolicy.Intersection);
// Result has only t1: value = 24
```

### UnionWithZero

Every timestamp from **either** series produces a result. Missing values are treated as zero:

```csharp
var result = TimeSeriesMath.Add(a, b, MissingValuePolicy.UnionWithZero);
// Result has t0, t1, t2:
//   t0 = 10 + 0  = 10
//   t1 = 20 + 4  = 24
//   t2 =  0 + 8  =  8
```

### Throw

Raises `InvalidOperationException` if any timestamp exists in one series but not the other:

```csharp
// Throws because t0 is in 'a' but not 'b'
var result = TimeSeriesMath.Add(a, b, MissingValuePolicy.Throw);
```

## SIMD Acceleration

For `double` and `int` types, `TimeSeriesMath` uses hardware-accelerated SIMD (`System.Numerics.Vector<T>`) for:

- **Add** (binary, element-wise)
- **Multiply** (scalar)

When `Vector.IsHardwareAccelerated` is `true` (nearly all modern x64/ARM CPUs), these operations process multiple elements per instruction. The fallback loop handles remaining elements that don't fill a full SIMD width.

This acceleration applies to the dense fast path in `FixedSlotTimeSeries` (when both series are fully dense and aligned) and to scalar operations on both series types.

## SortedArray Series Operations

All binary and scalar operations also work with `SortedArrayTimeSeries<T>`:

```csharp
var a = new SortedArrayTimeSeries<int>(Period.FiveMinutes);
var b = new SortedArrayTimeSeries<int>(Period.FiveMinutes);

a[t0] = 2;
b[t1] = 3;

var union = TimeSeriesMath.Add(a, b, MissingValuePolicy.UnionWithZero);
// t0 = 2, t1 = 3

var scaled = TimeSeriesMath.Multiply(a, 10);
// t0 = 20
```

The sparse merge algorithm uses a two-pointer walk over the sorted tick arrays, producing results in O(n + m) time where n and m are the sizes of the two input series.
