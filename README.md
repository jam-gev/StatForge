# StatForge

Allocation-free stat & modifier system for Unity, with project-defined channels.

## Install

Package Manager → **+** → **Add package from git URL…**

```
https://github.com/jam-gev/StatForge.git
```

Requires Unity 2022.3+. No other package dependencies.

## Usage

1. Create a `StandardStatAggregator` asset: **Create → StatForge → StandardStatAggregator**.
2. Create a `StatDefinition` asset for each stat (**Create → StatForge → StatDefinition**), set its Id, base value, Min/Max and assign the aggregator.

```csharp
var stats = new StatContainer();
stats.AddStat(healthDefinition);

var health = new StatId("Health");
StatModifierHandle handle = stats.AddModifier(
    StatModifier.Create(health, StandardStatChannel.Add, 10f));

float value = stats.GetValue(health); // base + 10, clamped to Min/Max

stats.RemoveModifier(handle);         // removes exactly that modifier; false if already removed
```

`StandardStatAggregator` computes `(base + ΣAdd) × (1 + ΣAddPercent) × ΠMultiply`; an `Override` (last added) replaces the result.

## Custom channels

Define your own int-backed enum (`0 = None`) and aggregator — no changes to the package:

```csharp
public enum GameChannel { None = 0, Add, Multiply, FinalAdd }

[CreateAssetMenu(menuName = "MyGame/GameAggregator")]
public sealed class GameAggregator : StatAggregator<GameChannel>
{
    public override float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries)
    {
        float add = 0f, multiply = 1f, finalAdd = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            switch (ChannelOf(entries[i]))
            {
                case GameChannel.Add:      add += entries[i].Value; break;
                case GameChannel.Multiply: multiply *= entries[i].Value; break;
                case GameChannel.FinalAdd: finalAdd += entries[i].Value; break;
            }
        }
        return (baseValue + add) * multiply + finalAdd;
    }
}
```

**Why int channels:** an enum shipped inside a package can't be extended by the game, and making the whole system generic over the channel type spreads `<T>` through every class and breaks ScriptableObject assets. So the core stores an `int`, and only the two edges that read or write the enum (`StatModifier.Create<T>`, `StatAggregator<T>.ChannelOf`) are generic — converted with `UnsafeUtility.As`, no boxing.

## Tests

Add the package to `testables` in your `Packages/manifest.json`, then run **EditMode** tests in the Test Runner:

```json
"testables": [ "com.jamgev.statforge" ]
```

Benchmarks compile only when `com.unity.test-framework.performance` is installed.
