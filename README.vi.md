# StatForge

Hệ thống stat (chỉ số) và modifier cho Unity: không cấp phát bộ nhớ trên đường chạy chính, loại modifier (`TStatModifierChannel`) do project tự định nghĩa.

[Vấn đề](#vấn-đề) · [Yêu cầu bài toán](#yêu-cầu-bài-toán) · [Giải pháp và các quyết định thiết kế](#giải-pháp-và-các-quyết-định-thiết-kế) · [Cài đặt](#cài-đặt) · [Cách dùng](#cách-dùng) · [Mở rộng](#mở-rộng) · [Test](#test)

## Vấn đề

Game nhập vai và hành động (RPG, action RPG…) gần như luôn có **hệ thống chỉ số nhân vật**: máu tối đa, sức tấn công, giáp, tốc độ đánh… thay đổi theo trang bị, buff và debuff. Project nào cũng cần, và StatForge được làm ra để tái sử dụng nhiều lần cho các project.

StatForge là một nền tảng nhỏ, đơn giản và dễ mở rộng. Mỗi project chỉ cần tập trung vào phần tùy biến của riêng mình (chỉ số nào, chỉnh sửa chỉ số loại gì, phép toán tính ra chỉ số như thế nào) và mở rộng trên lõi đó.

## Yêu cầu bài toán

### Bản chất của hệ thống chỉ số

Trong các game RPG, Action hay MOBA, nhân vật luôn xoay quanh các con số: máu, sát thương, giáp, tốc độ đánh… **Hệ thống chỉ số (Stat System)** làm nhiệm vụ quản lý và biến đổi các con số này một cách thống nhất.

### 1. Cấu trúc cốt lõi

Một hệ thống chỉ số chuẩn không bao giờ lưu cứng một con số (như `int hp = 100`), vì rất khó để bóc tách khi nhân vật mặc thêm hàng chục đồ hay nhận hàng loạt buff. Nó luôn chia làm 3 phần:

| Phần | Ý nghĩa |
|---|---|
| **Giá trị gốc** (Base Value) | Chỉ số bẩm sinh của nhân vật (thường theo Level hoặc Class) |
| **Nguồn thay đổi** (Modifiers) | Các tác động từ bên ngoài như vũ khí, bùa lợi (buff), bùa hại (debuff)… |
| **Giá trị cuối** (Final Value) | Con số cuối cùng trả về cho game sử dụng, sau khi đã tính toán (Base + Modifiers) |

### 2. Các loại phép toán cơ bản

Các Modifier tác động lên chỉ số không chỉ có cộng trừ, mà thường chia thành các loại chính:

| Loại | Ví dụ |
|---|---|
| **Cộng/trừ thẳng** (Flat) | Kiếm +20 sát thương |
| **Cộng phần trăm** (%) | Áo +10% giáp |
| **Nhân** (Multiply) | Cuồng nộ ×2 sát thương |
| **Ghi đè** (Override) | Bị đóng băng: tốc độ = 0, bỏ qua mọi modifier khác |

Hệ thống cần phân loại được chúng để biết cái nào tính trước, cái nào tính sau để ra kết quả chính xác nhất.

### 3. Những yêu cầu bắt buộc của hệ thống chỉ số

Nếu chỉ là cộng trừ thì rất dễ, nhưng khi số lượng trang bị và hiệu ứng nhiều lên, hệ thống phải đáp ứng 3 yêu cầu sau:

1. **Sự ổn định của công thức tính toán.** Đảm bảo kết quả cuối cùng luôn chính xác và nhất quán, bất kể thứ tự người chơi trang bị vật phẩm hay nhận các hiệu ứng đan xen phức tạp đến đâu.

2. **An toàn khi biến đổi Modifier.** Quá trình thêm, bớt hay hết hạn của các nguồn thay đổi chỉ số phải được quản lý chặt chẽ, không để lại sai số hay làm "mắc kẹt" dữ liệu trạng thái của nhân vật.

3. **Tối ưu hiệu năng cho Hot Code.** Việc đọc chỉ số diễn ra liên tục mỗi frame (trong combat, di chuyển…). Hệ thống phải có cơ chế trả về kết quả tức thì, không gây nghẽn CPU dù chịu tải với tần suất cao.

## Giải pháp và các quyết định thiết kế

### Các thành phần

**Nhóm 1: Định nghĩa chỉ số**

| Thành phần | Vai trò |
|---|---|
| `StatDefinition` | Định nghĩa chỉ số và các thông tin, cài đặt của 1 chỉ số |
| `StatId` | Định danh của chỉ số, nằm trong `StatDefinition` |

**Nhóm 2: Trạng thái runtime**

| Thành phần | Vai trò |
|---|---|
| `StatContainer` | Là nơi chứa các chỉ số (`StatNode`) của 1 đối tượng, cũng là nơi duy nhất để đọc và ghi giá trị |
| `StatNode` | Đơn vị quản lý độc lập của một chỉ số |

**Nhóm 3: Modifier**

| Thành phần | Vai trò |
|---|---|
| `StatModifier` | Một tác động lên chỉ số |
| `StatModifierEntry` | Modifier đã được thêm, nằm trong `StatNode` |
| `StatModifierHandle` | Biên nhận khi thêm modifier, dùng để gỡ đúng modifier đó |

**Nhóm 4: Tính toán**

| Thành phần | Vai trò |
|---|---|
| `StatAggregator` | Công thức gộp giá trị gốc và các modifier thành giá trị cuối, gắn trong `StatDefinition` |
| `StatAggregator<TStatModifierChannel>` | Lớp cha để project viết công thức riêng theo `TStatModifierChannel` của mình |
| `TStatModifierChannel` | Loại của modifier, mỗi project tự khai báo enum riêng; `StatModifier` lưu dưới dạng `int ModifierChannel` |
| `StandardStatModifierChannel` | Bộ channel có sẵn: Add, AddPercent, Multiply, Override |
| `StandardStatAggregator` | Công thức có sẵn cho `StandardStatModifierChannel` |
| `StatChannelCast` | Chuyển `TStatModifierChannel` giữa enum và `int` |

### Sơ đồ luồng

**Tạo chỉ số**

```
StatDefinition ──AddStat──► StatContainer ──tạo──► StatNode
                                                   (bản sao giá trị gốc)
```

**Thay đổi chỉ số**

```
StatContainer.AddModifier(StatModifier)
    └─► StatNode thêm StatModifierEntry ──► trả về StatModifierHandle

StatContainer.RemoveModifier(StatModifierHandle)
    └─► StatNode gỡ đúng entry đó

StatContainer.SetBase(id, value)
    └─► StatNode đổi giá trị gốc
```

Cả 3 thao tác đều đánh dấu chỉ số cần tính lại.

**Đọc chỉ số**

```
StatContainer.GetValue(id)
    └─► StatNode: có thay đổi từ lần đọc trước?
          ├─ có    ──► Clamp(Aggregator(giá trị gốc, các entry)) ──► lưu lại ──► trả về
          └─ không ──► trả về giá trị đã lưu
```

### Giải quyết các vấn đề

#### Công thức tính ổn định

StatForge không cố định các loại modifier hay công thức tính. Hai phần này do từng project định nghĩa:

| Phần | Project định nghĩa bằng | Quyết định điều gì |
|---|---|---|
| Cấu trúc modifier | Enum `TStatModifierChannel` | Có những loại modifier nào (cộng, phần trăm, nhân…) |
| Thực hiện phép tính | Kế thừa `StatAggregator<TStatModifierChannel>` | Loại nào tính trước, loại nào tính sau, gộp lại thế nào |

Lõi chỉ làm phần chung cho mọi project: lưu modifier kèm loại của nó (`int ModifierChannel`), gọi aggregator của chỉ số để tính, rồi áp Min/Max lên giá trị cuối. Mỗi lần tính lại đều đi từ giá trị gốc và danh sách modifier hiện có, nên kết quả chỉ phụ thuộc vào những modifier đang áp, không bị cộng dồn sai số qua các lần thêm/gỡ.

Mọi aggregator đều tính qua một hàm duy nhất:

```csharp
public abstract float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries);
```

| | Ý nghĩa |
|---|---|
| `baseValue` | Giá trị gốc hiện tại của chỉ số, chưa áp Min/Max |
| `entries` | Các modifier đang áp lên chỉ số; mỗi entry có `ModifierChannel` (loại) và `Value` (giá trị). Đọc loại bằng `ChannelOf(entry)` |
| Giá trị trả về | Kết quả tính từ giá trị gốc và các modifier; lõi áp Min/Max lên kết quả này để ra giá trị cuối |

Min/Max chỉ áp lên giá trị cuối, không áp lên giá trị gốc, nên gỡ modifier ra thì chỉ số về đúng giá trị cũ.

**Có sẵn để dùng ngay: `StandardStatModifierChannel` + `StandardStatAggregator`**

| `StandardStatModifierChannel` | Giá trị nghĩa là | Ví dụ |
|---|---|---|
| `Add` | Cộng vào giá trị gốc | Kiếm +20 sát thương |
| `AddPercent` | Phần trăm cộng thêm; các phần trăm cộng dồn với nhau | Áo +0.1 = +10% giáp |
| `Multiply` | Hệ số nhân; các hệ số nhân với nhau | Cuồng nộ ×2 |
| `Override` | Thay thế kết quả | Choáng: tốc độ đánh = 0 |

`StandardStatAggregator` tính như sau (rút gọn từ `Runtime/Aggregation/StandardStatAggregator.cs`):

```csharp
public sealed class StandardStatAggregator : StatAggregator<StandardStatModifierChannel>
{
    public override float Calculate(float baseValue, IReadOnlyList<StatModifierEntry> entries)
    {
        float sumAddChannel = 0, sumAddPercentChannel = 0, productMultiply = 1, overrideValue = 0;
        bool hasOverride = false;

        for (int index = 0; index < entries.Count; index++)
        {
            switch (ChannelOf(entries[index]))
            {
                case StandardStatModifierChannel.Add:        sumAddChannel += entries[index].Value; break;
                case StandardStatModifierChannel.AddPercent: sumAddPercentChannel += entries[index].Value; break;
                case StandardStatModifierChannel.Multiply:   productMultiply *= entries[index].Value; break;
                case StandardStatModifierChannel.Override:   overrideValue = entries[index].Value; hasOverride = true; break;
            }
        }

        if (hasOverride) return overrideValue;

        return (baseValue + sumAddChannel) * (1 + sumAddPercentChannel) * productMultiply;
    }
}
```

Kết quả trả về được lõi áp Min/Max (`Clamp`) để ra giá trị cuối.

Trong cùng một loại, thứ tự thêm không làm đổi kết quả. Riêng `Override`: có nhiều cái thì cái được thêm sau cùng thắng.

Ví dụ: sát thương gốc 100, Max 300, kiếm `Add +20`, hai buff `AddPercent +0.1` và `+0.2`, cuồng nộ `Multiply ×2`:

```
(100 + 20) × (1 + 0.1 + 0.2) × 2 = 312  ──Clamp──►  300
```

Project cần loại modifier khác (ví dụ `FinalAdd`, cộng sau khi nhân) thì khai báo enum `TStatModifierChannel` và aggregator riêng, xem [Mở rộng](#mở-rộng).

#### An toàn khi biến đổi Modifier

**Gỡ modifier bằng handle.** `AddModifier` trả về một `StatModifierHandle` gồm container, chỉ số và id của entry. Mỗi entry trong `StatNode` có id riêng, nên handle chỉ gỡ đúng modifier nó đã thêm, kể cả khi có nhiều modifier giống hệt nhau (hai thanh kiếm cùng +10).

| Trường hợp | `RemoveModifier` trả về |
|---|---|
| Handle hợp lệ, modifier còn đó | `true`, modifier bị gỡ |
| Gỡ lần thứ 2 bằng cùng handle | `false` |
| Handle của container khác | `false` |
| Handle mặc định (`default`) | `false` |

Các trường hợp `false` không phải lỗi, nên buff hết hạn hay trang bị bị tháo nhiều lần cũng không làm sai chỉ số. Handle so sánh theo giá trị, dùng làm key trong `Dictionary`/`HashSet` được (ví dụ một buff giữ danh sách các handle nó đã áp).

**Definition không bị ghi khi runtime.** `StatDefinition` không có setter lúc runtime. Mỗi `StatNode` giữ bản sao giá trị gốc riêng, nên `SetBase` trên một đối tượng không ảnh hưởng đến đối tượng khác dùng chung definition, cũng không làm đổi asset.

#### Tối ưu hiệu năng cho Hot Code

**Chỉ tính lại khi có thay đổi.** `StatNode` lưu sẵn giá trị cuối. `AddModifier`, `RemoveModifier` và `SetBase` chỉ đánh dấu chỉ số cần tính lại; aggregator chỉ chạy ở lần đọc kế tiếp. Đọc nhiều lần mỗi frame mà không có thay đổi thì chỉ là trả về một số đã lưu. `SetBase` với giá trị y như cũ thì không đánh dấu gì.

**Không cấp phát bộ nhớ khi đọc và tính.** Các thao tác chạy liên tục không tạo rác cho GC (Garbage Collector – bộ thu gom rác):

| Thao tác | Ghi chú |
|---|---|
| `GetValue`, `GetRoundedValue`, `TryGetValue` | Cả khi đã lưu sẵn lẫn khi phải tính lại, có hay không có modifier |
| `SetBase` | |
| Tra cứu theo `StatId` | `StatId` cài `IEquatable<StatId>`, `Dictionary<StatId, StatNode>` so sánh trực tiếp, không boxing; hash bằng `StringComparer.Ordinal` |
| So sánh `StatModifierHandle` | `IEquatable`, không boxing khi làm key trong `Dictionary`/`HashSet` |

Mỗi dòng trên có một test allocation (`StatAllocationTests`) canh giữ; ngoài ra có benchmark (`StatContainerBenchmarks`) đo tốc độ đọc trên 1.000 và 10.000 đối tượng.

**`ModifierChannel` lưu dạng `int`.** `StatModifier` và `StatModifierEntry` chỉ giữ `int ModifierChannel`, chỉ hai đầu đọc/ghi enum (`StatModifier.Create<TStatModifierChannel>`, `StatAggregator<TStatModifierChannel>.ChannelOf`) là generic. `StatChannelCast` chuyển enum ↔ `int` bằng `UnsafeUtility.As`, không boxing. Enum không nền `int` bị từ chối ngay lần dùng đầu.

### Các quyết định khác

#### 1. StatType không phải enum

`StatDefinition` (loại chỉ số) là ScriptableObject, `StatId` là struct. `StatDefinition` là asset chứa `StatId` cùng các cài đặt; `StatId` chỉ là định danh gọn nhẹ để tra cứu. Một chỉ số không chỉ có tên mà còn mang giá trị gốc, Min/Max, cách làm tròn và aggregator; enum chỉ cho được cái tên, phần dữ liệu còn lại phải để ở một bảng riêng.

Điểm mạnh:

- Designer thêm và chỉnh chỉ số ngay trong Editor, không cần sửa code.
- Mỗi chỉ số tự chọn aggregator (công thức) riêng.
- Package không phải biết trước danh sách chỉ số của game.
- `StatId` cài `IEquatable<StatId>`, tra cứu trong `StatContainer` không cấp phát bộ nhớ.
- Tránh lỗi của enum: Unity lưu enum dưới dạng số, chèn thêm giá trị vào giữa làm asset cũ trỏ nhầm chỉ số mà không báo gì.

Điểm yếu:

- `StatId` là chuỗi: gõ sai tên trong code thì đến lúc chạy mới báo lỗi, không được IDE gợi ý.
- Đổi Id trong asset thì các chỗ code dùng chuỗi cũ không còn khớp.

Cách giải quyết: sinh code cho `StatId` thay vì gõ tay. Menu **Tools → StatForge → Generate StatIds** quét mọi asset `StatDefinition` và sinh ra class hằng số `Assets/Generated/StatIds.cs`:

```csharp
// <auto-generated/>
public static class StatIds
{
    public static readonly StatId AttackSpeed = new StatId("AttackSpeed");
    public static readonly StatId Health      = new StatId("Health");
}
```

Code chỉ dùng `StatIds.Health`, không gõ chuỗi nữa. Cách này chạy được là nhờ `StatId` so sánh theo giá trị: `Equals` và `GetHashCode` dựa trên chuỗi (`StringComparison.Ordinal`), nên `StatId` tạo trong class sinh ra và `StatId` lưu trong asset tuy là hai bản khác nhau vẫn bằng nhau và trỏ đúng cùng một chỉ số trong `StatContainer`.

Các trường hợp cần xử lý:

- **Gõ sai tên** (`StatIds.Heatlh`): không compile được, IDE gợi ý đúng tên.
- **Trùng tên** (hai asset cùng Id `Health`): dừng sinh code, báo lỗi kèm đường dẫn của cả hai asset.
- **Id không đặt được làm tên biến C#** (có dấu cách, bắt đầu bằng số, trùng từ khoá như `event`): dừng sinh code, báo lỗi kèm asset bị sai.
- **Đổi hoặc xoá Id trong asset**: sinh lại thì hằng số cũ biến mất, mọi chỗ code còn dùng tên cũ báo lỗi compile ngay.
- **Code dùng Id không có asset**: hằng số không tồn tại, lỗi compile.
- **Quên sinh lại sau khi sửa asset**: `StatIdGeneratorMenu.IsUpToDate()` sinh lại từ asset hiện tại và so với file đang có; project gọi hàm này trong một test, lệch thì test fail (xem [Test](#test)).

#### 2. Phần kiểm tra của package không bị bỏ qua

`StatDefinition.Validate()` không phải `virtual`. Lớp con thêm luật kiểm tra riêng qua `OnValidateDefinition`, hàm này chạy sau các bước kiểm tra của package (Id hợp lệ, Min ≤ Max, có aggregator…).

Điểm mạnh:

- Luật của package luôn chạy, lớp con không thể vô tình bỏ qua.
- Lỗi cấu hình bị phát hiện ngay khi thêm chỉ số vào `StatContainer`, không đợi đến lúc đọc giá trị trong gameplay.

Điểm yếu:

- Lớp con chỉ thêm được luật, không nới lỏng hay thay luật của package.

#### 3. `StatAggregator` là ScriptableObject

Công thức tính (`StatAggregator`) là ScriptableObject gắn vào từng `StatDefinition`, nên mỗi chỉ số có thể định nghĩa công thức tính của mình hoặc tái sử dụng để cân bằng. Công thức này được mở rộng tùy thuộc vào bài toán của project.

Điểm mạnh:

- Mỗi chỉ số chọn công thức riêng, đổi công thức chỉ cần kéo asset khác vào `StatDefinition`.
- Project viết công thức riêng bằng cách kế thừa `StatAggregator<TStatModifierChannel>`, không phải sửa lõi.

Điểm yếu:

- Một asset aggregator dùng chung cho nhiều chỉ số, nên aggregator không được giữ trạng thái.

#### 4. `TStatModifierChannel` do project khai báo, lõi chỉ lưu `int`

Loại modifier (`TStatModifierChannel`) là enum do project tự khai báo. Package không thể mang sẵn một enum mà game mở rộng được, còn nếu làm cả hệ thống generic theo enum thì `<T>` lan ra mọi class và ScriptableObject không tạo asset được. Vì vậy lõi chỉ lưu `int ModifierChannel`, chỉ hai đầu là generic: lúc tạo modifier (`StatModifier.Create<TStatModifierChannel>`) và lúc aggregator đọc ra (`StatAggregator<TStatModifierChannel>.ChannelOf`).

```csharp
// Lõi chỉ lưu int
public readonly struct StatModifier
{
    public readonly StatId StatId;
    public readonly int ModifierChannel;
    public readonly float Value;

    // Đầu ghi: enum → int
    // Ví dụ: StatModifier.Create(health, StandardStatModifierChannel.Add, 10f) → ModifierChannel = 1
    public static StatModifier Create<TStatModifierChannel>(StatId statId, TStatModifierChannel channel, float value)
        where TStatModifierChannel : unmanaged, Enum
    {
        return new StatModifier(statId, StatChannelCast<TStatModifierChannel>.ToInt(channel), value);
    }
}

// Đầu đọc: int → enum
public abstract class StatAggregator<TStatModifierChannel> : StatAggregator
    where TStatModifierChannel : unmanaged, Enum
{
    // Ví dụ trong StandardStatAggregator: ChannelOf(entry) với ModifierChannel = 1 → StandardStatModifierChannel.Add
    protected static TStatModifierChannel ChannelOf(in StatModifierEntry entry)
    {
        return StatChannelCast<TStatModifierChannel>.FromInt(entry.ModifierChannel);
    }
}
```

Ví dụ project tự định nghĩa loại modifier và công thức đọc nó, thêm `FinalAdd` (cộng sau khi nhân), thứ mà `StandardStatModifierChannel` không có:

```csharp
// 1. Khai báo loại modifier của game (nền int, 0 = None)
public enum GameChannel { None = 0, Add, Multiply, FinalAdd }

// 2. Viết aggregator đọc đúng enum đó
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

// 3. Tạo modifier bằng enum của game
StatModifier.Create(health, GameChannel.FinalAdd, 5f); // lưu vào StatModifier dưới dạng int 3
```

Tạo asset `GameAggregator` rồi kéo vào các `StatDefinition` dùng `GameChannel`.

Điểm mạnh:

- Mỗi project tự khai báo loại modifier riêng mà không phải sửa lõi.
- Lõi không phụ thuộc enum nào, `StatContainer`, `StatNode`, `StatModifier` không cần `<T>`.
- Đổi enum ↔ `int` qua `StatChannelCast` không boxing; enum không nền `int` bị từ chối ngay lần dùng đầu.

Điểm yếu:

- Lõi chỉ thấy số `int`, không biết modifier được tạo từ enum nào. Tạo modifier bằng enum của game nhưng gắn vào chỉ số dùng `StandardStatAggregator` thì không báo lỗi, aggregator đọc số đó theo nghĩa của `StandardStatModifierChannel` và tính sai:

  ```csharp
  // health dùng StandardStatAggregator, gốc 100
  stats.AddModifier(StatModifier.Create(health, GameChannel.Multiply, 2f)); // lưu ModifierChannel = 2
  stats.GetValue(health); // StandardStatAggregator đọc 2 thành AddPercent → 300, không phải 200
  ```

- Quy ước `0 = None` chỉ nằm trong tài liệu, code không kiểm tra.

## Cài đặt

Package Manager → **+** → **Add package from git URL…**

```
https://github.com/jam-gev/StatForge.git
```

Yêu cầu Unity 2022.3 trở lên. Không phụ thuộc package nào khác.

## Cách dùng

**1. Chọn aggregator để tính toán.** Mỗi chỉ số cần một aggregator, chọn một trong hai:

| Lựa chọn | Cách làm |
|---|---|
| Dùng công thức có sẵn | Tạo asset `StandardStatAggregator`: **Create → StatForge → StandardStatAggregator** |
| Dùng công thức riêng của project | Kế thừa `StatAggregator<TStatModifierChannel>` rồi tạo asset, xem [Mở rộng](#mở-rộng) |

Một asset aggregator dùng chung được cho nhiều chỉ số.

**2. Tạo chỉ số.** Với mỗi chỉ số, tạo asset `StatDefinition` (**Create → StatForge → StatDefinition**), đặt Id, giá trị gốc, Min/Max và kéo aggregator ở bước 1 vào.

**3. Dùng trong code.**

```csharp
var stats = new StatContainer();
stats.AddStat(healthDefinition);

var health = new StatId("Health");
StatModifierHandle handle = stats.AddModifier(
    StatModifier.Create(health, StandardStatModifierChannel.Add, 10f));

float value = stats.GetValue(health); // gốc + 10, đã áp Min/Max

stats.RemoveModifier(handle);         // gỡ đúng modifier đó; trả về false nếu đã gỡ rồi
```

## Mở rộng

| Muốn | Làm |
|---|---|
| Thêm chỉ số | Tạo asset `StatDefinition` mới, rồi chạy **Tools → StatForge → Generate StatIds** |
| Đổi công thức tính của một chỉ số | Kế thừa `StatAggregator<TStatModifierChannel>`, tạo asset rồi kéo vào `StatDefinition` |
| Thêm loại modifier riêng | Khai báo enum nền `int`, giá trị `0` là `None`; ví dụ `GameChannel` ở [quyết định 4](#4-tstatmodifierchannel-do-project-khai-báo-lõi-chỉ-lưu-int) |
| Thêm dữ liệu cho chỉ số (icon, tên hiển thị…) | Kế thừa `StatDefinition`, thêm luật kiểm tra trong `OnValidateDefinition`, xem [quyết định 2](#2-phần-kiểm-tra-của-package-không-bị-bỏ-qua) |
| Sinh `StatIds` vào chỗ khác hoặc trong namespace riêng | Gọi `StatIdCodeGenerator.Generate(StatIdGeneratorMenu.FindSources(), "StatIds", "MyGame")` rồi tự ghi file |

## Test

Thêm package vào `testables` trong `Packages/manifest.json`, rồi chạy **EditMode** trong Test Runner:

```json
"testables": [ "com.jamgev.statforge" ]
```

| Nhóm test | Kiểm tra |
|---|---|
| `StatContainerTests` | Thêm chỉ số, đọc và ghi giá trị gốc, thêm và gỡ modifier bằng handle, Min/Max, làm tròn |
| `StatAggregatorTests` | Thứ tự tính Add → AddPercent → Multiply, Override, kẹp Min/Max |
| `CustomChannelTests` | Enum và aggregator do project khai báo |
| `DerivedDefinitionTests` | Lớp con của `StatDefinition` và `OnValidateDefinition` |
| `StatIdCodeGeneratorTests` | Sinh class `StatIds`: sắp xếp, trùng Id, Id không đặt được làm tên biến |
| `StatAllocationTests` | Đọc chỉ số không cấp phát bộ nhớ |
| `StatContainerBenchmarks` | Đo tốc độ, chỉ compile khi project có `com.unity.test-framework.performance` |

Để bắt lỗi quên sinh lại `StatIds`, project thêm một test (asmdef của test tham chiếu `StatForge.Editor`):

```csharp
[Test]
public void StatIds_MatchesAssets()
{
    Assert.IsTrue(StatIdGeneratorMenu.IsUpToDate(), "Chạy Tools → StatForge → Generate StatIds");
}
```
