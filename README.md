# Old Phone Pad

Decodes multi-tap keypad input from a classic mobile phone into text.

```
"4433555 555666#"  ->  "HELLO"
```

A .NET 8 library with no dependencies, plus a REST API that wraps it for non-.NET callers.

---

## Run it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet test
dotnet run --project src/OldPhonePad.Api
```

Then open <http://localhost:5080> for the interactive keypad, or <http://localhost:5080/swagger> for the API reference.

---

## Structure

Three projects. That is the whole thing.

```
OldPhonePad/
├── OldPhonePad.sln
├── src/
│   ├── OldPhonePad/            the library
│   └── OldPhonePad.Api/        REST wrapper + demo page
├── tests/
│   └── OldPhonePad.Tests/      unit tests + API tests
└── docs/
    ├── HOW-TO.md               customer integration guide
    ├── ARCHITECTURE.md         why it is shaped this way
    └── AI-PROMPT.md            AI tooling disclosure
```

**`src/OldPhonePad`** — seven files, no dependencies. This is the deliverable.

| File | Does |
|---|---|
| `OldPhone.cs` | The static entry point from the brief: `OldPhonePad(string)` |
| `OldPhonePadDecoder.cs` | The state machine that reads the input |
| `KeypadLayout.cs` | Which button maps to which characters |
| `KeypadKeys.cs` | The reserved `#` and `*` keys |
| `DecodeResult.cs` | A decode outcome, for the non-throwing path |
| `DecodeErrorKind.cs` | Why a decode failed, as an enum |
| `OldPhonePadFormatException.cs` | Thrown by `Decode` on bad input |

**`src/OldPhonePad.Api`** — the wrapper. `Program.cs` wires everything up; `DecodeEndpoints.cs` and `KeypadEndpoints.cs` hold the routes; `Contracts.cs` is the JSON shape.

**`tests/OldPhonePad.Tests`** — `DecoderTests.cs` and `KeypadLayoutTests.cs` test the library directly; `ApiTests.cs` runs real HTTP requests against the API in memory.

---

## Using the library

```csharp
using OldPhonePad;

OldPhone.OldPhonePad("33#");                 // "E"
OldPhone.OldPhonePad("227*#");               // "B"
OldPhone.OldPhonePad("4433555 555666#");     // "HELLO"
OldPhone.OldPhonePad("8 88777444666*664#");  // "TURING"
```

When the input comes from a user, decode without exceptions:

```csharp
var result = OldPhonePadDecoder.Default.TryDecode(userInput);

if (result.IsSuccess)
{
    Console.WriteLine(result.Value);
}
else
{
    Console.WriteLine($"{result.ErrorMessage} (at index {result.ErrorIndex})");
}
```

Custom keypads — any alphabet:

```csharp
var layout = new KeypadLayout(new Dictionary<char, string>
{
    ['2'] = "ΑΒΓ",
    ['3'] = "ΔΕΖ",
});

new OldPhonePadDecoder(layout).Decode("22 333#");   // "ΒΖ"
```

`OldPhonePadDecoder` and `KeypadLayout` are immutable, so `Default` is safe to share across threads.

---

## The rules

| Input | Meaning | Example |
|---|---|---|
| `0`–`9` | Press a button. Repeats cycle through its characters. | `222#` → `C` |
| space, tab, newline | Pause. Ends the current run. | `222 2 22#` → `CAB` |
| `*` | Backspace. Deletes the previous character. | `227*#` → `B` |
| `#` | Send. Returns, ignoring anything after it. | `2#xyz` → `A` |

Pressing past the last character wraps: `2222#` → `A`.

---

## The API

| Method | Route | |
|---|---|---|
| `POST` | `/v1/decode` | Decode one sequence |
| `POST` | `/v1/decode/batch` | Decode up to 100 at once |
| `GET` | `/v1/keypad` | Describe the keypad, for building a UI |
| `GET` | `/health` | Liveness probe |

```bash
curl -X POST http://localhost:5080/v1/decode \
  -H 'Content-Type: application/json' \
  -d '{"input":"4433555 555666#"}'
```

```json
{ "input": "4433555 555666#", "output": "HELLO", "keyPressCount": 14 }
```

Errors come back as [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem documents. Full integration guide: **[docs/HOW-TO.md](docs/HOW-TO.md)**.

### Docker

```bash
docker build -f src/OldPhonePad.Api/Dockerfile -t oldphonepad-api .
docker run -p 8080:8080 oldphonepad-api
```

## Licence

MIT. See [LICENSE](LICENSE).
