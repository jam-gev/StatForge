# StatForge

[![Tests](https://github.com/jam-gev/StatForge/actions/workflows/test.yml/badge.svg)](https://github.com/jam-gev/StatForge/actions/workflows/test.yml)

Allocation-free stat & modifier system for Unity, with project-defined modifier types (channels).

[Problem](#problem) · [Concepts](#concepts) · [Design decisions](#design-decisions) · [Install](#install) · [Usage](#usage) · [Extending](#extending) · [Tests](#tests)

## Problem

RPGs and action games almost always have a **character stat system**: max health, attack, armor, attack speed… changing with equipment, buffs and debuffs. Every project needs one, and StatForge is made to be reused across projects.

It's a small, simple, tested foundation meant to be extended, replicated and reused. Each project only focuses on its own customization (which stats exist, which kinds of modifiers, how values are calculated) and extends the core where it needs to.

## Concepts

| Type | Kind | Role |
|---|---|---|
| `StatDefinition` | ScriptableObject | What a stat is: Id, base value, Min/Max, rounding, aggregator. Read-only at runtime |
| `StatContainer` | class | The stats of one entity. The only way to read and write values |
| `StatModifier` | struct | Something to apply: stat, channel, value |
| `StatModifierHandle` | struct | Receipt returned when a modifier is added; give it back to remove exactly that modifier |
| `StatAggregator` | ScriptableObject | How base value + modifiers become the final value. Swappable per stat |
| Channel | enum | The kind of modifier: whether its value is added, added as a percentage, multiplied or replaces the result. `StandardStatModifierChannel` is built in, or define your own |

```
StatDefinition ──(copied base value)──► StatContainer ──► value = Clamp(Aggregator(base, modifiers))
                                             ▲
                       AddModifier(modifier) │ returns StatModifierHandle
```

### What a channel is

**A channel is the kind of a modifier.** It tells the aggregator what to do with the modifier's value. The same `10` means +10 on `Add` and ×10 on `Multiply`.

The package ships `StandardStatModifierChannel`:

| Channel | The value means | Example |
|---|---|---|
| `Add` | Added to the base value | Sword: +10 attack |
| `AddPercent` | Percentage bonus; percentages add up | Buff: +0.2 = +20% attack |
| `Multiply` | Multiplier; multipliers multiply together | Rage: ×2 |
| `Override` | Replaces the result | Stunned: attack speed = 0 |

If a game needs other kinds (e.g. `FinalAdd`, added after multiplying), it defines its own channel enum plus an aggregator that handles each kind. See [Custom channels and aggregator](#custom-channels-and-aggregator).

## Design decisions

**Stats are ScriptableObjects, not an enum.** A stat is more than a name: it carries a base value, limits, rounding and an aggregator. An enum only gives the name, so that data would have to live in a separate table; adding a stat would need a code change; a package can't ship the game's enum; and Unity serializes enums as numbers, so inserting a value silently remaps existing assets. As assets, designers add and tune stats without code, and each stat picks its own formula.

**Definitions are never written at runtime.** `StatDefinition` has no runtime setters. Each container copies the base value into its own state, so containers sharing a definition can't affect each other or the asset.

**A fixed calculation order.** `StandardStatAggregator` computes `(base + ΣAdd) × (1 + ΣAddPercent) × ΠMultiply`; percentages add up, multipliers multiply; an `Override` (last added) replaces the result. Min/Max are applied to the final value, never to the base. The order in which modifiers were added doesn't change the result.

**Modifiers are removed by handle.** `AddModifier` returns a `StatModifierHandle` (container, stat, entry id). A handle removes exactly one modifier; removing it twice, or with a handle from another container, just returns `false`.

**Channels are stored as `int`.** An enum shipped inside a package can't be extended by the game, and making the whole system generic over the channel type spreads `<T>` through every class and breaks ScriptableObject assets. So the core stores an `int`, and only the two edges that read or write the enum (`StatModifier.Create<TStatModifierChannel>`, `StatAggregator<TStatModifierChannel>.ChannelOf`) are generic — converted with `UnsafeUtility.As`, no boxing. A non-int-backed enum is rejected on first use.

**Extension points can't skip the package's checks.** `StatDefinition.Validate()` is not virtual; subclasses add rules through `OnValidateDefinition`, which runs after the package's own checks.

**No allocations on the hot path.** Reading values, recalculating dirty stats, aggregating and comparing handles allocate nothing; allocation tests guard each of these.

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
    StatModifier.Create(health, StandardStatModifierChannel.Add, 10f));

float value = stats.GetValue(health); // base + 10, clamped to Min/Max

stats.RemoveModifier(handle);         // removes exactly that modifier; false if already removed
```

## Extending

| You want to… | Do this |
|---|---|
| Add a stat | Create a `StatDefinition` asset |
| Change how a stat is calculated | Subclass `StatAggregator<TStatModifierChannel>`, drag the asset onto the definition |
| Add your own modifier channels | Define an int-backed enum (`0 = None`) |
| Add data to stats (icon, display name…) | Subclass `StatDefinition`, add checks in `OnValidateDefinition` |

### Custom channels and aggregator

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

Aggregator assets are shared by every stat that uses them: keep them stateless.

### Custom definition data

```csharp
[CreateAssetMenu(menuName = "MyGame/GameStatDefinition")]
public sealed class GameStatDefinition : StatDefinition
{
    [field: SerializeField] public Sprite Icon { get; private set; }

    protected override void OnValidateDefinition()
    {
        if (Icon == null) throw new InvalidOperationException($"[{Id}] Icon is not assigned");
    }
}
```

### Stat ids: generated, not typed by hand

The one thing an enum does better is that the compiler checks names. **Tools → StatForge → Generate StatIds** scans every `StatDefinition` asset and writes a constants class to `Assets/Generated/StatIds.cs`:

```csharp
// <auto-generated/>
public static class StatIds
{
    public static readonly StatId AttackSpeed = new StatId("AttackSpeed");
    public static readonly StatId Health      = new StatId("Health");
}
```

| Problem | How generation prevents it |
|---|---|
| **Duplicate** ids across assets | Generation fails and names both asset paths |
| **Missing** stat (code uses an id with no asset) | The constant doesn't exist → compile error |
| **Misspelled** names (`"Heatlh"`) | `StatIds.Heatlh` doesn't compile; IDE autocompletes |
| **Invalid** C# names (spaces, leading digit, keywords) | Generation fails and names the asset |
| Generated file out of date | Call `StatIdGeneratorMenu.IsUpToDate()` from a project test |

## Tests

Add the package to `testables` in your `Packages/manifest.json`, then run **EditMode** tests in the Test Runner:

```json
"testables": [ "com.jamgev.statforge" ]
```

Benchmarks compile only when `com.unity.test-framework.performance` is installed.
