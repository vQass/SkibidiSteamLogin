# Raport z Audytu Kodu i Architektury

**Projekt:** SkibidiSteamLogin  
**Język / Framework:** C# / .NET 8, ASP.NET Core  
**Data audytu:** 2026-03-01  
**Audytor:** Principal Software Architect / Clean Code Expert

---

## 1. Podsumowanie (Executive Summary)

Projekt SkibidiSteamLogin to biblioteka (.NET 8) umożliwiająca programistyczny login do platformy Steam, wystawiona dodatkowo jako Web API. Architektura jest rozsądnie podzielona na trzy projekty (Core, Tests, WebApi) z wydzielonymi warstwami modeli, serwisów, wrapperów i mapowań. Zastosowane extension method do rejestracji modułu DI to dobra praktyka.

Jednak pod tą fasadą kryją się **krytyczne problemy bezpieczeństwa**, które dyskwalifikują projekt z jakiegokolwiek użycia produkcyjnego w obecnym stanie. Hasła użytkowników są przesyłane przez query string (HTTP GET), walidacja certyfikatów SSL jest całkowicie wyłączona, a pliki cookies są zapisywane na dysk jako plain-text JSON. Ponadto serwis `LoginHandler` jest zarejestrowany jako Singleton, ale przechowuje stan mutowalny (`_response`), co prowadzi do wyścigów wątków (race conditions) w środowisku wieloużytkownikowym.

Na poziomie kodu dominują: brak obsługi błędów (zwracanie `null` zamiast Result Pattern), zmienne o nieczytelnych nazwach (`result2`, `result3`, `result4`), niezaimplementowane metody (`SetCookies`), literówki w nazwach zmiennych, zduplikowane nagłówki HTTP, mieszanie dwóch bibliotek JSON (Newtonsoft + System.Text.Json) oraz całkowity brak logowania. Pokrycie testami obejmuje wyłącznie klasy pomocnicze (`HexDecoder`, `EncryptionHelper`) — brak testów dla logiki biznesowej i warstwy HTTP.

---

## 2. Ocena Architektury

### 2.1 Pozytywne aspekty
- Podział na trzy projekty (Core, Tests, WebApi) jest zasadny.
- Wydzielenie interfejsów `ILoginHandler` / `IHttpClientWrapper` umożliwia (teoretycznie) testowalność.
- Extension method `AddSteamLoginCoreModule()` — czysta rejestracja modułu DI.
- Wydzielenie mapowań (`HttpResultMapping`, `LoginResultMapping`) jako extension methods.
- Użycie `internal` dla klas implementacyjnych ogranicza publiczną powierzchnię API.

### 2.2 Problemy architektoniczne

| # | Problem | Opis | Propozycja rozwiązania |
|---|---------|------|------------------------|
| A1 | **Singleton z mutowalnym stanem** | `LoginHandler` jest rejestrowany jako Singleton (`TryAddSingleton`) ale przechowuje pole `_response` (stan sesji logowania). W środowisku wielowątkowym (np. WebAPI z wieloma użytkownikami) prowadzi to do wyścigów wątków i nadpisywania stanu jednego użytkownika przez drugiego. | Zmienić lifetime na `Scoped` lub `Transient`, albo usunąć stan z serwisu i przekazywać go jawnie między wywołaniami (np. przez token/id sesji). |
| A2 | **Brak `IHttpClientFactory`** | `HttpClientWrapper` tworzy własny `HttpClient` w konstruktorze. Jest to znany antywzorzec w .NET — prowadzi do wyczerpania socketów (socket exhaustion) przy dużym obciążeniu oraz uniemożliwia centralne zarządzanie konfiguracją HTTP. | Użyć `IHttpClientFactory` lub `AddHttpClient<>()` z DI. |
| A3 | **Naruszenie SRP** | `ILoginHandler` łączy trzy różne odpowiedzialności: logowanie do Steam, zarządzanie cookies w pamięci, oraz persystencję cookies na dysk (I/O plikowe). | Wydzielić `ICookiePersistenceService` do zapisu/odczytu cookies. |
| A4 | **Brak wzorca Result Pattern** | `LoginAsync` zwraca `null` w przypadku błędu zamiast obiektu wyniku z informacją o błędzie. Konsument API nie ma możliwości rozróżnienia rodzaju błędu. | Wprowadzić `Result<T>` / `OperationResult<T>` z enumem błędów i komunikatem. |
| A5 | **Brak warstwy abstrakcji dla logowania (logging)** | W całym projekcie nie ma ani jednego wywołania `ILogger`. Debugowanie problemów w środowisku produkcyjnym jest praktycznie niemożliwe. | Wstrzyknąć `ILogger<T>` do `LoginHandler` i `HttpClientWrapper`. |
| A6 | **Mieszanie bibliotek serializacji** | `SteamLoginResponse` i `SteamResponseWrapper` używają jednocześnie atrybutów `[JsonProperty]` (Newtonsoft) i `[JsonPropertyName]` (System.Text.Json). Tworzy to mylne wrażenie że oba serializatory są używane, podczas gdy kod korzysta wyłącznie z Newtonsoft. | Wybrać jedną bibliotekę serializacji i usunąć drugie atrybuty. |
| A7 | **Brak walidacji/autoryzacji w WebAPI** | Controller wystawia endpointy logowania bez żadnej autoryzacji, rate-limitingu, ani walidacji modelu. | Dodać middleware walidacyjny, rate-limiting, oraz rozważyć autentykację API key. |

---

## 3. Główne Code Smelle i Problemy z Czytelnością

### 3.1 Problemy krytyczne (bezpieczeństwo)

| Plik / Moduł | Nazwa problemu | Opis |
|---|---|---|
| `Wrappers/HttpClientWrapper.cs` L27 | **Wyłączona walidacja SSL** | `ServerCertificateCustomValidationCallback = (...) => true;` — akceptuje KAŻDY certyfikat, w tym sfałszowane (man-in-the-middle). Hasła i tokeny mogą być przechwycone. |
| `Controllers/LoginHandlerController.cs` L21 | **Credentials w query string (HTTP GET)** | `[HttpGet("Login")] Login(string username, string password)` — hasła i loginy trafiają do URL-a, logów serwera, logów proxy i historii przeglądarki. |
| `Services/LoginHandler.cs` L90-99 | **Plain-text cookie persistence** | `File.WriteAllText("cookies.json", serialized)` — cookies sesji Steam (zawierające tokeny uwierzytelniające) zapisywane jako nieszyfrowany plik JSON z hardkodowaną ścieżką. |
| `Wrappers/HttpClientWrapper.cs` L59 | **Credentials w query string (HTTP)** | `$"?account_name={username}&encrypted_password={...}&encryption_timestamp={timestamp}"` — dane logowania wysyłane jako query parameters zamiast w body POST. |

### 3.2 Problemy projektowe

| Plik / Moduł | Nazwa problemu | Opis |
|---|---|---|
| `Services/LoginHandler.cs` L18 | **Mutable state w Singleton** | Pole `_response` przetrzymuje stan sesji logowania w obiekcie Singleton — race condition w środowisku wielowątkowym. |
| `Services/LoginHandler.cs` L33, L41 | **`return null` zamiast Result Pattern** | Przy niepowodzeniu zwraca `null` z komentarzami `// TODO`. Konsument nie wie dlaczego operacja się nie powiodła. |
| `Services/LoginHandler.cs` L77 | **`return null` z `EnterSteamGuardCodeAsync`** | Metoda wykonuje cały flow logowania (guard → poll → finalize → set token) ale na końcu zwraca `null` zamiast wyniku. |
| `Wrappers/HttpClientWrapper.cs` L199-201 | **`NotImplementedException`** | `SetCookies()` rzuca `NotImplementedException`. Martwy kod w publicznym interfejsie. |
| `Wrappers/HttpClientWrapper.cs` L113, L144 | **Hardkodowany boundary** | `"----WebKitFormBoundarysMZXRB5xhtSNbrDh"` powtórzony dwukrotnie — powinien być generowany automatycznie przez `MultipartFormDataContent`. |

### 3.3 Code Smells

| Plik / Moduł | Nazwa problemu | Opis |
|---|---|---|
| `Services/LoginHandler.cs` L56-77 | **Zmienne `result`, `result2`, `result3`, `result4`** | Sekwencyjne numerowanie zmiennych drastycznie obniża czytelność. Nazwy powinny odzwierciedlać semantykę (np. `guardResult`, `pollResult`, `finalizeResult`). |
| `Services/LoginHandler.cs` L62 | **Magic number `Task.Delay(500)`** | Hardkodowane 500ms bez wyjaśnienia dlaczego. Brak stałej ani komentarza. |
| `Wrappers/HttpClientWrapper.cs` L66, L91, L106, L132 | **Literówka: `resonseString`** | Czterokrotnie powtórzona literówka „resonse" zamiast „response". |
| `Wrappers/HttpClientWrapper.cs` L162-186 | **Hardkodowane nagłówki HTTP** | Dwie metody (`ApplyHeaders`, `ApplyHeadersFinalize`) zawierają dziesiątki hardkodowanych nagłówków HTTP. Duplikacja User-Agent i Accept-Language. |
| `Controllers/LoginHandlerController.cs` L43-56 | **Nieużywane parametry** | `SaveCookies` i `LoadCookies` przyjmują `authcode` i `guardTypeEnum` ale ich nie używają. |
| `Controllers/LoginHandlerController.cs` L44, L52 | **Błędny `ProducesResponseType`** | `SaveCookies` i `LoadCookies` deklarują `typeof(LoginResult)` jako typ odpowiedzi ale zwracają `Ok()` bez ciała. |
| `Models/Consts/Endpoints.cs` L3 | **Niestylowa klasa stałych** | `Endpoints` jest `internal class` zamiast `internal static class`. Można niezamierzenie utworzyć instancję. |
| `Models/SteamResponses/FinalizeLoginResult.cs` | **Publiczne klasy wewnętrzne** | `FinalizeLoginResult`, `TransferInfo`, `Params` są `public` ale powinny być `internal` — wyciekają typy implementacyjne. |
| `SkibidiSteamLogin.Core.csproj` L11 | **Ścieżka do ikony z Desktop** | `..\..\..\..\Desktop\881163_bathroom_512x512.png` — hardkodowana ścieżka absolutna do pliku na pulpicie. Nie zadziała na innej maszynie. |
| `Wrappers/HttpClientWrapper.cs` L12 | **Singleton bez `IDisposable`** | `HttpClientWrapper` tworzy `HttpClient` i `HttpClientHandler` ale nie implementuje `IDisposable`. |

### 3.4 Pokrycie testami

| Obszar | Status |
|---|---|
| `HexDecoder` | ✅ 8 testów — dobre pokrycie edge cases |
| `EncryptionHelper` | ✅ 5 testów — poprawne |
| `LoginHandler` | ❌ Brak testów |
| `HttpClientWrapper` | ❌ Brak testów |
| `Mapping` | ❌ Brak testów |
| `LoginHandlerController` | ❌ Brak testów |
| **Brak mocking framework** | W projekcie testowym nie ma Moq/NSubstitute — utrudnia testowanie klas z zależnościami |

---

## 4. Przykłady Refaktoryzacji (Złote strzały)

### 4.1 `EnterSteamGuardCodeAsync` — czytelne nazwy zamiast `result1..result4`

**PRZED:**
```csharp
public async Task<LoginResult> EnterSteamGuardCodeAsync(string authcode, AuthGuardTypeEnum guardTypeEnum)
{
    var steamGuardRequest = new SteamGuardRequest
    {
        ClientId = _response.ClientId,
        SteamId = _response.SteamId,
        Code = authcode,
        CodeType = guardTypeEnum
    };

    var result = await _httpClientWrapper.EnterSteamGuardCodeAsync(steamGuardRequest);

    await Task.Delay(500);

    var result2 = await _httpClientWrapper.PollAuthSessionStatusAsync(_response.ClientId, _response.RequestId);

    var result3 = await _httpClientWrapper.FinalizeLoginAsync(result2.Data);

    var tokensToSet = result3.Data.TransferInfo.Where(x => _options.SetTokenDomains.Any(y => x.Url.Contains(y)));

    foreach (var token in tokensToSet)
    {
        var result4 = await _httpClientWrapper.SetToken(_response.SteamId, token.Params.Auth, token.Params.Nonce, token.Url);
    }

    return null;
}
```

**PO:**
```csharp
private const int SteamGuardPollDelayMs = 500;

public async Task<LoginResult> EnterSteamGuardCodeAsync(string authcode, AuthGuardTypeEnum guardTypeEnum)
{
    var steamGuardRequest = new SteamGuardRequest
    {
        ClientId = _response.ClientId,
        SteamId = _response.SteamId,
        Code = authcode,
        CodeType = guardTypeEnum
    };

    var guardResult = await _httpClientWrapper.EnterSteamGuardCodeAsync(steamGuardRequest);
    if (!guardResult.IsSuccess)
        return LoginResult.Failure("Steam Guard code submission failed.");

    // Steam API requires a short delay before polling for session status
    await Task.Delay(SteamGuardPollDelayMs);

    var pollResult = await _httpClientWrapper.PollAuthSessionStatusAsync(
        _response.ClientId, _response.RequestId);
    if (!pollResult.IsSuccess || pollResult.Data is null)
        return LoginResult.Failure("Polling auth session status failed.");

    var finalizeResult = await _httpClientWrapper.FinalizeLoginAsync(pollResult.Data);
    if (!finalizeResult.IsSuccess)
        return LoginResult.Failure("Login finalization failed.");

    var relevantTokens = finalizeResult.Data.TransferInfo
        .Where(t => _options.SetTokenDomains.Any(domain => t.Url.Contains(domain)));

    foreach (var token in relevantTokens)
    {
        await _httpClientWrapper.SetToken(
            _response.SteamId, token.Params.Auth, token.Params.Nonce, token.Url);
    }

    return _response; // or a proper success result
}
```

**Uzasadnienie:**
- Zmienne mają semantyczne nazwy (`guardResult`, `pollResult`, `finalizeResult`, `relevantTokens`).
- Każdy krok HTTP jest walidowany — fail-fast zamiast propagacji `NullReferenceException`.
- Magic number `500` zastąpiony nazwaną stałą z komentarzem wyjaśniającym.
- Metoda zwraca sensowną wartość zamiast `null`.

---

### 4.2 `LoginHandlerController` — POST zamiast GET dla logowania, usunięcie nieużywanych parametrów

**PRZED:**
```csharp
[HttpGet("Login")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
public async Task<IActionResult> Login(string username, string password)
{
    var result = await _loginHandler.LoginAsync(username, password);
    return Ok(result);
}

[HttpPost("SaveCookies")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
public IActionResult SaveCookies(string authcode, AuthGuardTypeEnum guardTypeEnum)
{
    _loginHandler.SaveCookies();
    return Ok();
}

[HttpPost("LoadCookies")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
public IActionResult LoadCookies(string authcode, AuthGuardTypeEnum guardTypeEnum)
{
    _loginHandler.LoadCookies();
    return Ok();
}
```

**PO:**
```csharp
public record LoginRequest(string Username, string Password);

[HttpPost("Login")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return BadRequest("Username and password are required.");

    var result = await _loginHandler.LoginAsync(request.Username, request.Password);

    if (result is null)
        return BadRequest("Login failed.");

    return Ok(result);
}

[HttpPost("SaveCookies")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public IActionResult SaveCookies()
{
    _loginHandler.SaveCookies();
    return NoContent();
}

[HttpPost("LoadCookies")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public IActionResult LoadCookies()
{
    _loginHandler.LoadCookies();
    return NoContent();
}
```

**Uzasadnienie:**
- Login zmieniony z GET na POST — credentials przesyłane w body (nie w URL).
- Dodano `[FromBody]` i walidację danych wejściowych.
- Usunięto nieużywane parametry z `SaveCookies` / `LoadCookies`.
- Poprawiono `ProducesResponseType` — `NoContent` zamiast `LoginResult` gdzie nie zwracamy ciała.

---

### 4.3 `HttpClientWrapper` — usunięcie wyłączenia SSL i centralizacja konfiguracji nagłówków

**PRZED:**
```csharp
public HttpClientWrapper()
{
    _cookieContainer = new CookieContainer();
    var httpHandler = new HttpClientHandler()
    { 
        CookieContainer = _cookieContainer
    };
    httpHandler.ServerCertificateCustomValidationCallback = 
        (sender, cert, chain, sslPolicyErrors) => { return true; };
    _httpClient = new HttpClient(httpHandler);
}

private void ApplyHeaders(HttpRequestMessage httpRequestMessage, bool encoded = true)
{
    httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7");
    httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; ...");
    if (encoded)
    {
        httpRequestMessage.Headers.TryAddWithoutValidation("Accept", "text/html,...");
        httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
        httpRequestMessage.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
    }
}

private void ApplyHeadersFinalize(HttpRequestMessage httpRequestMessage)
{
    // ...20 lines of hardcoded headers...
}
```

**PO:**
```csharp
private static class HttpHeaders
{
    public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
    public const string AcceptLanguage = "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7";
    public const string AcceptHtml = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
    public const string AcceptJson = "application/json, text/plain, */*";
    public const string AcceptEncoding = "gzip, deflate, br, zstd";
}

public HttpClientWrapper()
{
    _cookieContainer = new CookieContainer();
    var httpHandler = new HttpClientHandler
    {
        CookieContainer = _cookieContainer
        // REMOVED: Do NOT disable SSL validation in production
    };
    _httpClient = new HttpClient(httpHandler);
    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", HttpHeaders.UserAgent);
    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", HttpHeaders.AcceptLanguage);
}

private void ApplyHeaders(HttpRequestMessage request, bool encoded = true)
{
    if (encoded)
    {
        request.Headers.TryAddWithoutValidation("Accept", HttpHeaders.AcceptHtml);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", HttpHeaders.AcceptEncoding);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>());
    }
}
```

**Uzasadnienie:**
- Usunięto `ServerCertificateCustomValidationCallback = true` — krytyczna luka bezpieczeństwa.
- Stałe nagłówkowe wyekstrahowane do dedykowanej klasy — DRY i single source of truth.
- Wspólne nagłówki (User-Agent, Accept-Language) ustawione na `DefaultRequestHeaders` zamiast powtarzania w każdej metodzie.

---

## 5. Plan Naprawczy (Action Items)

### 🔴 Krytyczne (bezpieczeństwo — natychmiast)

1. **Usunąć wyłączenie walidacji SSL** (`HttpClientWrapper.cs` L27) — przywrócić domyślną walidację certyfikatów. Jeśli potrzebne do dewelopmentu, zabezpieczyć flagą konfiguracyjną (`#if DEBUG` lub `IHostEnvironment.IsDevelopment()`).

2. **Zmienić endpoint Login z GET na POST** (`LoginHandlerController.cs` L21) — przenieść credentials do request body (`[FromBody]`). Hasła NIGDY nie powinny trafiać do query string.

3. **Przenieść dane logowania HTTP do request body** (`HttpClientWrapper.cs` L59) — `username`, `encrypted_password`, `encryption_timestamp` powinny być przesyłane jako `FormUrlEncodedContent` zamiast query parameters.

4. **Zaszyfrować persystencję cookies** (`LoginHandler.cs` L90-99) — zastąpić plain-text JSON szyfrowaniem (np. `DataProtectionProvider`) lub użyć `IDataProtector`. Usunąć hardkodowaną ścieżkę `cookies.json`.

### 🟠 Wysokie (architektura — w ciągu 1-2 sprintów)

5. **Zmienić lifetime `LoginHandler` z Singleton na Scoped** — wyeliminować mutable state lub użyć wzorca sesji (session token) do zarządzania stanem logowania.

6. **Wprowadzić `IHttpClientFactory`** — zastąpić ręczne tworzenie `HttpClient` w `HttpClientWrapper` przez `IHttpClientFactory` zarejestrowane w DI.

7. **Wdrożyć Result Pattern** — zastąpić `return null` obiektem `Result<LoginResult>` z informacją o sukcesie/błędzie/typie błędu. Usunąć komentarze `// TODO`.

8. **Wydzielić `ICookiePersistenceService`** — przenieść `SaveCookies` / `LoadCookies` z `ILoginHandler` do osobnego interfejsu/serwisu. Respektować SRP.

9. **Dodać logowanie (`ILogger<T>`)** — wstrzyknąć logger do `LoginHandler` i `HttpClientWrapper`. Logować błędy HTTP, niepowodzenia logowania, oraz kroki flow-u.

10. **Zaimplementować `SetCookies()`** w `HttpClientWrapper` lub usunąć z interfejsu, jeśli nie jest potrzebne. `NotImplementedException` w kodzie produkcyjnym to poważny dług techniczny.

### 🟡 Średnie (jakość kodu — w ciągu 2-4 sprintów)

11. **Wybrać jedną bibliotekę JSON** — usunąć atrybuty `[JsonPropertyName]` (System.Text.Json) z modeli, jeśli używany jest wyłącznie Newtonsoft, lub przeprowadzić migrację na System.Text.Json i usunąć Newtonsoft.

12. **Poprawić nazwy zmiennych** — zmienić `result`, `result2`, `result3`, `result4` na semantyczne nazwy. Poprawić literówkę `resonseString` → `responseString` (4 wystąpienia).

13. **Usunąć nieużywane parametry** — `SaveCookies(string authcode, AuthGuardTypeEnum guardTypeEnum)` i `LoadCookies(...)` w kontrolerze przyjmują parametry, których nie używają.

14. **Poprawić `ProducesResponseType`** — `SaveCookies` i `LoadCookies` deklarują `typeof(LoginResult)` jako typ odpowiedzi ale zwracają pusty `Ok()`.

15. **Wyekstrahować stałe nagłówkowe HTTP** — wyciągnąć zduplikowane stringi (User-Agent, Accept-Language, boundary) do stałych lub konfiguracji.

16. **Oznaczyć `Endpoints` jako `static class`** — zapobiec niezamierzonemu tworzeniu instancji.

17. **Zmienić publiczne klasy wewnętrzne na `internal`** — `FinalizeLoginResult`, `TransferInfo`, `Params` wyciekają do publicznego API biblioteki bez potrzeby.

### 🟢 Niskie (dług techniczny — backlog)

18. **Zwiększyć pokrycie testami** — dodać testy dla `LoginHandler` (z mockiem `IHttpClientWrapper`), `HttpClientWrapper` (z mockiem HTTP), mapowań i kontrolera. Dodać framework mockujący (Moq lub NSubstitute).

19. **Usunąć hardkodowaną ścieżkę do ikony pakietu** (`SkibidiSteamLogin.Core.csproj` L11) — `..\..\..\..\Desktop\881163_bathroom_512x512.png` nie zadziała na innej maszynie. Przenieść plik do repozytorium.

20. **Zastąpić `RSACryptoServiceProvider` nowszym `RSA.Create()`** — `RSACryptoServiceProvider` jest przestarzałe na platformach innych niż Windows. `RSA.Create()` jest cross-platform.

21. **Zamienić `Task.Delay(500)` na nazwaną stałą** z komentarzem wyjaśniającym dlaczego opóźnienie jest potrzebne (lub lepiej: retry loop z exponential backoff na `PollAuthSessionStatus`).

22. **Dodać `IDisposable` do `HttpClientWrapper`** — jeśli klasa tworzy `HttpClient` / `HttpClientHandler`, powinna je poprawnie zwalniać.

23. **Rozważyć async I/O** — `SaveCookies` i `LoadCookies` używają synchronicznych `File.WriteAllText` / `File.ReadAllText`. Zamienić na `File.WriteAllTextAsync` / `File.ReadAllTextAsync`.

---

*Koniec raportu.*
