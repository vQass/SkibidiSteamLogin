# Raport Czytelności Kodu (Clean Code Audit)

## 1. Wskaźnik Obciążenia Poznawczego (Krótkie podsumowanie)

Kod jest **umiarkowanie czytelny**. Struktura folderów i podział na warstwy (Interfaces, Models, Services, Wrappers, Mapping) jest przejrzysty, a nazwy klas w większości trafnie oddają intencję. Nowy deweloper powinien stosunkowo szybko zrozumieć ogólny przepływ logowania do Steam.

Główne źródła obciążenia poznawczego to:
- **`HttpClientWrapper`** (~260 linii) — klasa-gigant, która pełni zbyt wiele ról jednocześnie: budowanie requestów, deserializacja odpowiedzi, zarządzanie ciasteczkami, aplikowanie nagłówków. Czytelnik musi przeskanować cały plik, by zrozumieć choćby jedną operację.
- **`EnterSteamGuardCodeAsync`** w `LoginHandler` — metoda orkiestruje 4 oddzielne kroki sieciowe (guard → delay → poll → finalize + pętla tokenów), co wymaga trzymania w pamięci wielu stanów pośrednich.
- **Powtarzalny wzorzec** deserializacji + sprawdzenia statusu + logowania ostrzeżenia w każdej metodzie `HttpClientWrapper` — ten sam schemat kopiowany 6 razy zwiększa szum kodu.

Krzywa wejścia: **~1-2h** na zrozumienie całego flow, co jest akceptowalne, ale mogłoby być znacznie lepsze po zastosowaniu poniższych refaktoryzacji.

---

## 2. Największe Przeszkody w Czytelności

- **Sufiks `Enum` w nazwach enumów** (`AuthGuardTypeEnum`, `ErrorTypeEnum`, `PlatformTypeEnum`) — w C# jest to antywzorzec, typ i tak jest wyraźny z kontekstu. Sufiks dodaje szum i wydłuża każde odwołanie.
- **`HttpClientWrapper` jest klasą-bogiem (God Class)** — łączy odpowiedzialności budowania HTTP requestów, deserializacji JSON, zarządzania nagłówkami i zarządzania ciasteczkami. Łamie SRP na poziomie klasy.
- **Metoda `EnterSteamGuardCodeAsync` robi za dużo** — wykonuje 4 kroki sieciowe + iterację po tokenach. Łamie SRP na poziomie funkcji. Czytelnik musi trzymać w głowie cały łańcuch operacji.
- **Wzorzec „deserializuj → sprawdź → zaloguj → zwróć" powtórzony 6 razy** w `HttpClientWrapper` bez ekstrakcji do wspólnej metody generycznej — czytelnik widzi ten sam boilerplate w kółko.
- **Magiczne stringi nagłówków HTTP** w `ApplyHeadersFinalize` — 12 linii hardkodowanych wartości (`"Sec-Ch-Ua"`, `"Origin"`, `"Referer"`) bez stałych, tłumaczą "co", ale nie "dlaczego".
- **Niespójność konwencji nazewniczej `Async`** — metoda `SetToken` jest asynchroniczna, ale nie ma sufiksu `Async`, podczas gdy wszystkie pozostałe metody go mają.
- **Martwy kod** — `ErrorTypeEnum` i `PlatformTypeEnum` nie są nigdzie używane w kodzie źródłowym.
- **Parametr `authcode`** w `EnterSteamGuardCodeAsync` łamie konwencję camelCase (powinno być `authCode`).
- **Komentarze szablonowe** w `Program.cs` (`// Learn more about configuring Swagger/OpenAPI...`) to szum.
- **Klasa `SetTokenDomains`** ma nazwę będącą frazą czasownikową — sugeruje akcję, a nie strukturę danych. Lepsza nazwa: `TokenDomains` lub `SteamDomains`.

---

## 3. Sala Operacyjna — Refaktoryzacje

### 3.1. Ekstrakcja generycznej metody HTTP w `HttpClientWrapper`

**Lokalizacja:** `HttpClientWrapper.cs` — metody `GetRsaDataAsync`, `LoginAsync`, `PollAuthSessionStatusAsync`, `FinalizeLoginAsync`

**Diagnoza:** Każda z 4 metod zwracających dane powtarza identyczny schemat: wyślij request → odczytaj response body → jeśli sukces, deserializuj → jeśli błąd, zaloguj ostrzeżenie → zwróć wynik. Ten kopiuj-wklej boilerplate powiela 20-30 linii na metodę i utrudnia czytelność.

**Kod PRZED:**

```csharp
public async Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username)
{
    _logger.LogDebug("Fetching RSA data for user {Username}.", username);
    var result = await _httpClient.GetAsync(
        Endpoints.SteamPoweredUrlBase + Endpoints.GetRsa + "?account_name=" + username);

    RsaData data = null;

    if (result.IsSuccessStatusCode)
    {
        var responseString = await result.Content.ReadAsStringAsync();
        var response = JsonConvert.DeserializeObject<SteamResponseWrapper<RsaData>>(responseString);
        data = response?.Data;
    }
    else
    {
        _logger.LogWarning("Failed to fetch RSA data. Status: {StatusCode}", result.StatusCode);
    }

    return result.ToHttpDataResult(data);
}

public async Task<HttpDataResult<SteamLoginResponse>> LoginAsync(
    string username, string encryptedPassword, long timestamp)
{
    _logger.LogDebug("Sending login request for user {Username}.", username);
    var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.CredentialsSessionStart);
    // ... budowanie contentu i headersów ...
    var result = await _httpClient.SendAsync(msg);
    var responseString = await result.Content.ReadAsStringAsync();

    SteamLoginResponse data = null;

    if (result.IsSuccessStatusCode)
    {
        var responseData = JsonConvert.DeserializeObject<SteamResponseWrapper<SteamLoginResponse>>(responseString);
        data = responseData?.Data;
    }
    else
    {
        _logger.LogWarning("Login request failed. Status: {StatusCode}", result.StatusCode);
    }

    return result.ToHttpDataResult(data);
}
```

**Kod PO refaktoryzacji:**

```csharp
private async Task<HttpDataResult<TResult>> SendAndDeserializeAsync<TResponse, TResult>(
    HttpResponseMessage response,
    Func<TResponse, TResult> selector,
    string operationName) where TResult : class
{
    TResult data = null;

    if (response.IsSuccessStatusCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        var deserialized = JsonConvert.DeserializeObject<TResponse>(json);
        data = deserialized is not null ? selector(deserialized) : null;
    }
    else
    {
        _logger.LogWarning("{Operation} failed. Status: {StatusCode}", operationName, response.StatusCode);
    }

    return response.ToHttpDataResult(data);
}

public async Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username)
{
    _logger.LogDebug("Fetching RSA data for user {Username}.", username);

    var response = await _httpClient.GetAsync(
        Endpoints.SteamPoweredUrlBase + Endpoints.GetRsa + "?account_name=" + username);

    return await SendAndDeserializeAsync<SteamResponseWrapper<RsaData>, RsaData>(
        response, wrapper => wrapper.Data, "Fetch RSA data");
}

public async Task<HttpDataResult<SteamLoginResponse>> LoginAsync(
    string username, string encryptedPassword, long timestamp)
{
    _logger.LogDebug("Sending login request for user {Username}.", username);
    // ... budowanie contentu i headersów (bez zmian) ...
    var response = await _httpClient.SendAsync(msg);

    return await SendAndDeserializeAsync<SteamResponseWrapper<SteamLoginResponse>, SteamLoginResponse>(
        response, wrapper => wrapper.Data, "Login request");
}
```

**Zysk:** Usunięcie ~60 linii zduplikowanego boilerplate'u. Każda metoda HTTP staje się 5-8-liniowa zamiast 20-liniowa. Nowa abstrakcja `SendAndDeserializeAsync` jest jedynym miejscem do modyfikacji logiki deserializacji.

---

### 3.2. Rozbicie `EnterSteamGuardCodeAsync` na mniejsze kroki

**Lokalizacja:** `LoginHandler.cs` → `EnterSteamGuardCodeAsync`

**Diagnoza:** Metoda orkiestruje 4 kroki sieciowe (submit guard code → poll → finalize → set tokens) z wieloma warunkami if-return. Czytelnik musi trzymać w głowie cały łańcuch. Każdy krok powinien być osobną, nazwaną metodą.

**Kod PRZED:**

```csharp
public async Task<OperationResult<LoginResult>> EnterSteamGuardCodeAsync(
    LoginResult loginSession, string authcode, AuthGuardTypeEnum guardTypeEnum)
{
    _logger.LogInformation("Submitting Steam Guard code for SteamId {SteamId}.", loginSession.SteamId);

    var steamGuardRequest = new SteamGuardRequest
    {
        ClientId = loginSession.ClientId,
        SteamId = loginSession.SteamId,
        Code = authcode,
        CodeType = guardTypeEnum
    };

    var guardResult = await _httpClientWrapper.EnterSteamGuardCodeAsync(steamGuardRequest);
    if (!guardResult.IsSuccess)
    {
        _logger.LogWarning("Steam Guard code submission failed.");
        return OperationResult<LoginResult>.Failure("Steam Guard code submission failed.");
    }

    await Task.Delay(SteamGuardPollDelayMs);

    var pollResult = await _httpClientWrapper.PollAuthSessionStatusAsync(
        loginSession.ClientId, loginSession.RequestId);
    if (!pollResult.IsSuccess || pollResult.Data is null)
    {
        _logger.LogWarning("Polling auth session status failed.");
        return OperationResult<LoginResult>.Failure("Polling auth session status failed.");
    }

    var finalizeResult = await _httpClientWrapper.FinalizeLoginAsync(pollResult.Data);
    if (!finalizeResult.IsSuccess || finalizeResult.Data is null)
    {
        _logger.LogWarning("Login finalization failed.");
        return OperationResult<LoginResult>.Failure("Login finalization failed.");
    }

    var relevantTokens = finalizeResult.Data.TransferInfo
        .Where(t => _options.SetTokenDomains.Any(domain => t.Url.Contains(domain)));

    foreach (var token in relevantTokens)
    {
        var tokenResult = await _httpClientWrapper.SetToken(
            loginSession.SteamId, token.Params.Auth, token.Params.Nonce, token.Url);

        if (!tokenResult.IsSuccess)
            _logger.LogWarning("Failed to set token for {Url}.", token.Url);
    }

    _logger.LogInformation("Steam Guard flow completed successfully.");
    return OperationResult<LoginResult>.Success(loginSession);
}
```

**Kod PO refaktoryzacji:**

```csharp
public async Task<OperationResult<LoginResult>> EnterSteamGuardCodeAsync(
    LoginResult loginSession, string authCode, AuthGuardTypeEnum guardType)
{
    _logger.LogInformation("Submitting Steam Guard code for SteamId {SteamId}.", loginSession.SteamId);

    var submitResult = await SubmitGuardCodeAsync(loginSession, authCode, guardType);
    if (!submitResult.IsSuccess)
        return submitResult;

    var refreshToken = await PollForRefreshTokenAsync(loginSession);
    if (!refreshToken.IsSuccess)
        return refreshToken;

    var finalizeResult = await FinalizeAndApplyTokensAsync(loginSession, refreshToken.Data);
    if (!finalizeResult.IsSuccess)
        return finalizeResult;

    _logger.LogInformation("Steam Guard flow completed successfully.");
    return OperationResult<LoginResult>.Success(loginSession);
}

private async Task<OperationResult<LoginResult>> SubmitGuardCodeAsync(
    LoginResult session, string authCode, AuthGuardTypeEnum guardType)
{
    var request = new SteamGuardRequest
    {
        ClientId = session.ClientId,
        SteamId = session.SteamId,
        Code = authCode,
        CodeType = guardType
    };

    var result = await _httpClientWrapper.EnterSteamGuardCodeAsync(request);
    if (!result.IsSuccess)
    {
        _logger.LogWarning("Steam Guard code submission failed.");
        return OperationResult<LoginResult>.Failure("Steam Guard code submission failed.");
    }

    return OperationResult<LoginResult>.Success(session);
}

private async Task<OperationResult<string>> PollForRefreshTokenAsync(LoginResult session)
{
    await Task.Delay(SteamGuardPollDelayMs);

    var pollResult = await _httpClientWrapper.PollAuthSessionStatusAsync(
        session.ClientId, session.RequestId);

    if (!pollResult.IsSuccess || pollResult.Data is null)
    {
        _logger.LogWarning("Polling auth session status failed.");
        return OperationResult<string>.Failure("Polling auth session status failed.");
    }

    return OperationResult<string>.Success(pollResult.Data);
}

private async Task<OperationResult<LoginResult>> FinalizeAndApplyTokensAsync(
    LoginResult session, string refreshToken)
{
    var finalizeResult = await _httpClientWrapper.FinalizeLoginAsync(refreshToken);
    if (!finalizeResult.IsSuccess || finalizeResult.Data is null)
    {
        _logger.LogWarning("Login finalization failed.");
        return OperationResult<LoginResult>.Failure("Login finalization failed.");
    }

    await ApplyRelevantTokensAsync(session.SteamId, finalizeResult.Data);
    return OperationResult<LoginResult>.Success(session);
}

private async Task ApplyRelevantTokensAsync(string steamId, FinalizeLoginResult finalizeData)
{
    var relevantTokens = finalizeData.TransferInfo
        .Where(t => _options.SetTokenDomains.Any(domain => t.Url.Contains(domain)));

    foreach (var token in relevantTokens)
    {
        var result = await _httpClientWrapper.SetTokenAsync(
            steamId, token.Params.Auth, token.Params.Nonce, token.Url);

        if (!result.IsSuccess)
            _logger.LogWarning("Failed to set token for {Url}.", token.Url);
    }
}
```

**Zysk:** Metoda publiczna `EnterSteamGuardCodeAsync` staje się 12-liniowym czytelnym przepisem (submit → poll → finalize). Każdy krok ma nazwę opisującą intencję. Nowy deweloper czyta nazwy metod jak spis treści zamiast analizować 40 linii imperatywnego kodu.

---

### 3.3. Zamiana muru magicznych stringów na stałe w `ApplyHeadersFinalize`

**Lokalizacja:** `HttpClientWrapper.cs` → `ApplyHeadersFinalize`

**Diagnoza:** 12 linii z hardkodowanymi magicznymi stringami nagłówków HTTP. Brak komentarza wyjaśniającego *dlaczego* te konkretne nagłówki są wymagane. Wartości typu `"Sec-Ch-Ua"` z wersją przeglądarki to kruche stringi, które mogą wymagać aktualizacji — a czytelnik nie wie, skąd się wzięły.

**Kod PRZED:**

```csharp
private static void ApplyHeadersFinalize(HttpRequestMessage httpRequestMessage)
{
    httpRequestMessage.Headers.TryAddWithoutValidation("Accept", HttpHeaderConstants.AcceptJson);
    httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Encoding", HttpHeaderConstants.AcceptEncoding);
    httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Language", HttpHeaderConstants.AcceptLanguageFinalize);
    httpRequestMessage.Headers.TryAddWithoutValidation("Connection", "keep-alive");
    httpRequestMessage.Headers.TryAddWithoutValidation("Host", "login.steampowered.com");
    httpRequestMessage.Headers.TryAddWithoutValidation("Origin", "https://steamcommunity.com");
    httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://steamcommunity.com/");
    httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Ch-Ua", "\"Microsoft Edge\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
    httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?0");
    httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"Windows\"");
    httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
    httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
    httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
}
```

**Kod PO refaktoryzacji:**

W `HttpHeaderConstants.cs` dodaj brakujące stałe:

```csharp
internal static class HttpHeaderConstants
{
    // Identyfikacja klienta
    internal const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
    internal const string AcceptLanguage = "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7";
    internal const string AcceptLanguageFinalize = "pl,en;q=0.9,en-GB;q=0.8,en-US;q=0.7";

    // Formaty odpowiedzi
    internal const string AcceptHtml = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
    internal const string AcceptJson = "application/json, text/plain, */*";
    internal const string AcceptEncoding = "gzip, deflate, br, zstd";

    // Nagłówki wymagane przez Steam login endpoint do emulacji przeglądarki
    internal const string FinalizeHost = "login.steampowered.com";
    internal const string FinalizeOrigin = "https://steamcommunity.com";
    internal const string FinalizeReferer = "https://steamcommunity.com/";
    internal const string SecChUa = "\"Microsoft Edge\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"";
    internal const string SecChUaMobile = "?0";
    internal const string SecChUaPlatform = "\"Windows\"";
}
```

W `HttpClientWrapper.cs`:

```csharp
/// <summary>
/// Steam's login finalization endpoint wymaga nagłówków emulujących przeglądarkę.
/// Bez nich serwer zwraca 403.
/// </summary>
private static void ApplyFinalizeHeaders(HttpRequestMessage request)
{
    var h = request.Headers;
    h.TryAddWithoutValidation("Accept", HttpHeaderConstants.AcceptJson);
    h.TryAddWithoutValidation("Accept-Encoding", HttpHeaderConstants.AcceptEncoding);
    h.TryAddWithoutValidation("Accept-Language", HttpHeaderConstants.AcceptLanguageFinalize);
    h.TryAddWithoutValidation("Connection", "keep-alive");
    h.TryAddWithoutValidation("Host", HttpHeaderConstants.FinalizeHost);
    h.TryAddWithoutValidation("Origin", HttpHeaderConstants.FinalizeOrigin);
    h.TryAddWithoutValidation("Referer", HttpHeaderConstants.FinalizeReferer);
    h.TryAddWithoutValidation("Sec-Ch-Ua", HttpHeaderConstants.SecChUa);
    h.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", HttpHeaderConstants.SecChUaMobile);
    h.TryAddWithoutValidation("Sec-Ch-Ua-Platform", HttpHeaderConstants.SecChUaPlatform);
    h.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
    h.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
    h.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
}
```

**Zysk:** Magiczne stringi przeniesione do jednego źródła prawdy (`HttpHeaderConstants`). Komentarz `/// <summary>` wyjaśnia *dlaczego* te nagłówki istnieją (emulacja przeglądarki, 403 bez nich). Zmienna `h` eliminuje powtarzanie `httpRequestMessage.Headers`. Nazwa `ApplyFinalizeHeaders` jest spójniejsza z `ApplyHeaders`.

---

### 3.4. Usunięcie sufiksu `Enum` z nazw typów wyliczeniowych

**Lokalizacja:** `AuthGuardTypeEnum.cs`, `ErrorTypeEnum.cs`, `PlatformTypeEnum.cs`

**Diagnoza:** Sufiks `Enum` w nazwie enuma jest redundantny — kompilator i IDE już informują, że to typ wyliczeniowy. Microsoft Naming Guidelines jednoznacznie odradzają ten wzorzec. Dodaje szum przy każdym użyciu (`AuthGuardTypeEnum guardTypeEnum` → podwójne „Enum").

**Kod PRZED:**

```csharp
public enum AuthGuardTypeEnum
{
    Unknown = 0,
    None = 1,
    EmailCode = 2,
    // ...
}

public enum ErrorTypeEnum
{
    TooManyRequests = 0,
    // ...
}

internal enum PlatformTypeEnum
{
    Unknown = 0,
    // ...
}

// Użycie:
public async Task<OperationResult<LoginResult>> EnterSteamGuardCodeAsync(
    LoginResult loginSession, string authcode, AuthGuardTypeEnum guardTypeEnum)
```

**Kod PO refaktoryzacji:**

```csharp
public enum AuthGuardType
{
    Unknown = 0,
    None = 1,
    EmailCode = 2,
    // ...
}

public enum ErrorType
{
    TooManyRequests = 0,
    // ...
}

internal enum PlatformType
{
    Unknown = 0,
    // ...
}

// Użycie:
public async Task<OperationResult<LoginResult>> EnterSteamGuardCodeAsync(
    LoginResult loginSession, string authCode, AuthGuardType guardType)
```

**Zysk:** Każde odwołanie do enuma staje się krótsze i bardziej naturalne. `AuthGuardType.EmailCode` czyta się jak zdanie. Parametry metod nie mają podwójnego `Enum` (`guardTypeEnum` → `guardType`). Przy okazji naprawiony `authcode` → `authCode` (konwencja camelCase).

---

### 3.5. Niespójny brak sufiksu `Async` oraz nazwa klasy `SetTokenDomains`

**Lokalizacja:** `IHttpClientWrapper.cs` → `SetToken`, `SetTokenDomains.cs`

**Diagnoza:** Metoda `SetToken` jest asynchroniczna (`Task<HttpResult>`), ale jako jedyna w całym interfejsie nie posiada sufiksu `Async`. Łamie to konwencję stosowaną konsekwentnie we wszystkich pozostałych metodach (`StartSessionAsync`, `LoginAsync`, `FinalizeLoginAsync` itd.), co dezorientuje czytelnika. Klasa `SetTokenDomains` ma nazwę brzmiącą jak akcja (czasownik + rzeczownik), podczas gdy jest statyczną kolekcją domen.

**Kod PRZED:**

```csharp
// IHttpClientWrapper.cs
internal interface IHttpClientWrapper
{
    Task<HttpResult> StartSessionAsync();
    Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username);
    Task<HttpDataResult<SteamLoginResponse>> LoginAsync(string username, string encryptedPassword, long timestamp);
    Task<HttpResult> EnterSteamGuardCodeAsync(SteamGuardRequest steamGuardRequest);
    Task<HttpDataResult<string>> PollAuthSessionStatusAsync(string clientId, string requestId);
    Task<HttpDataResult<FinalizeLoginResult>> FinalizeLoginAsync(string token);
    Task<HttpResult> SetToken(string steamId, string auth, string nonce, string url);  // ← brak Async
    CookieCollection GetCookies();
    void SetCookies(CookieCollection cookieCollection);
}

// SetTokenDomains.cs
public static class SetTokenDomains  // ← nazwa brzmi jak akcja
{
    public static List<string> AllDomains { get => [...]; }
}
```

**Kod PO refaktoryzacji:**

```csharp
// IHttpClientWrapper.cs
internal interface IHttpClientWrapper
{
    Task<HttpResult> StartSessionAsync();
    Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username);
    Task<HttpDataResult<SteamLoginResponse>> LoginAsync(string username, string encryptedPassword, long timestamp);
    Task<HttpResult> EnterSteamGuardCodeAsync(SteamGuardRequest steamGuardRequest);
    Task<HttpDataResult<string>> PollAuthSessionStatusAsync(string clientId, string requestId);
    Task<HttpDataResult<FinalizeLoginResult>> FinalizeLoginAsync(string token);
    Task<HttpResult> SetTokenAsync(string steamId, string auth, string nonce, string url);  // ✓ spójne
    CookieCollection GetCookies();
    void SetCookies(CookieCollection cookieCollection);
}

// SteamDomains.cs (zmieniona nazwa pliku i klasy)
public static class SteamDomains
{
    public const string Community = "steamcommunity.com";
    public const string Store = "store.steampowered.com";
    public const string Help = "help.steampowered.com";
    public const string Checkout = "checkout.steampowered";
    public const string Tv = "steam.tv";

    public static List<string> All => [Community, Store, Help, Checkout, Tv];
}
```

**Zysk:** `SetTokenAsync` jest spójne z resztą interfejsu — czytelnik nie zastanawia się, czy to celowe pominięcie. `SteamDomains.Community` czyta się naturalniej niż `SetTokenDomains.SteamCommunityDomain` — nazwa klasy nie sugeruje już akcji, a stałe nie powtarzają kontekstu klasy w swojej nazwie (unikamy `SteamDomains.SteamCommunityDomain`). Właściwość `AllDomains` uproszczona do `All` — kontekst klasy wystarczy.
