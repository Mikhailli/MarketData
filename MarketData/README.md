# MarketData

MarketData - это .NET Worker Service для сбора, нормализации, дедупликации и сохранения real-time биржевых тиков из нескольких WebSocket-источников.

Проект реализован как тестовое задание для Senior C# Developer. Основной акцент сделан на асинхронной обработке, параллельной работе с WebSocket-подключениями, backpressure, batch-записи в БД, отказоустойчивости и расширяемой архитектуре для добавления новых бирж.

## Что реализовано

- Параллельные WebSocket-клиенты для Binance, Bybit и Kraken.
- Автоматическое переподключение при обрывах соединения.
- Bounded `Channel<T>` для backpressure и защиты памяти.
- Нормализация разных JSON-форматов к единой доменной модели `Tick`.
- In-memory дедупликация с TTL cleanup.
- Batch-запись тиков в PostgreSQL.
- Автоматическое создание таблицы и индексов при старте приложения.
- Структурированное логирование через Serilog.
- Метрики в логах: raw messages, processed ticks/sec, persisted ticks/sec, duplicates, parsing errors, DB errors, reconnects.
- Unit-тесты для normalizers, normalizer factory и deduplication.
- Docker Compose для локального запуска.

## Технологии

- .NET 8
- Worker Service / Generic Host
- System.Net.WebSockets
- System.Threading.Channels
- PostgreSQL 16
- Dapper
- Npgsql
- Serilog
- xUnit
- FluentAssertions
- Docker / Docker Compose

## Архитектура

Приложение построено как background processing pipeline. WebSocket-клиенты только получают raw-сообщения и пишут их в очередь. Отдельные processing workers нормализуют сообщения, удаляют дубликаты и передают готовые тики в очередь на сохранение. Persistence worker пишет данные в PostgreSQL батчами.

```text
Binance WebSocket
Bybit WebSocket
Kraken WebSocket
       |
       v
Channel<RawExchangeMessage>
       |
       v
Normalizer workers
       |
       v
Deduplicator
       |
       v
Channel<Tick>
       |
       v
Batch persistence worker
       |
       v
PostgreSQL
```

### Почему Worker Service

Задача не требует HTTP API, controllers, Swagger или MVC. Это долгоживущий background-процесс с постоянными WebSocket-подключениями, поэтому `Microsoft.NET.Sdk.Worker` и hosted services подходят лучше, чем Web API.

### Почему Channels

`System.Threading.Channels` хорошо подходит для async producer-consumer pipeline:

- несколько WebSocket-клиентов могут писать сообщения параллельно;
- несколько processing workers могут читать сообщения параллельно;
- bounded capacity защищает приложение от неконтролируемого роста памяти;
- backpressure работает явно: если очередь заполнена, producer ожидает свободное место.

### Почему batch-запись

Приложение не делает `INSERT` на каждый тик. Persistence worker буферизует данные и сбрасывает их в БД по размеру батча или по таймеру:

- `BatchSize`: по умолчанию `250`;
- `FlushIntervalMilliseconds`: по умолчанию `500`.

Это снижает количество round-trip в PostgreSQL и хорошо покрывает требуемую нагрузку 50-100 тиков/сек.

## Структура проекта

```text
MarketData/
├── Application/
│   └── Abstractions/              # Контракты pipeline
├── Domain/
│   └── Entities/                  # Tick и RawExchangeMessage
├── Infrastructure/
│   ├── Exchanges/                 # WebSocket-клиенты
│   ├── Metrics/                   # In-memory счетчики
│   ├── Normalizers/               # Парсинг форматов разных бирж
│   ├── Options/                   # Options из appsettings.json
│   ├── Persistence/               # PostgreSQL schema и repository
│   └── Services/                  # Hosted services и workers
├── tests/
│   └── MarketData.Tests/          # Unit-тесты
├── Program.cs                     # Composition root и DI
├── appsettings.json               # Конфигурация приложения
├── docker-compose.yml             # PostgreSQL + worker
├── Dockerfile                     # Docker image для worker
└── MarketData.sln
```

## Доменная модель

```csharp
public sealed record Tick
{
    public required string Exchange { get; init; }
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Volume { get; init; }
    public required DateTime TimestampUtc { get; init; }
}
```

Raw-сообщение хранит имя биржи и исходный JSON payload:

```csharp
public sealed record RawExchangeMessage
{
    public required string Exchange { get; init; }
    public required string Payload { get; init; }
    public DateTime ReceivedUtc { get; init; } = DateTime.UtcNow;
}
```

## База данных

Схема создается приложением при старте через `PostgresDatabaseInitializer`. Отдельный `init.sql` не используется, чтобы не было двух источников истины.

Основная таблица:

```sql
CREATE TABLE IF NOT EXISTS ticks
(
    id BIGSERIAL PRIMARY KEY,
    exchange TEXT NOT NULL,
    symbol TEXT NOT NULL,
    price NUMERIC(18,8) NOT NULL,
    volume NUMERIC(18,8) NOT NULL,
    timestamp_utc TIMESTAMPTZ NOT NULL,
    received_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Индексы:

- unique index для дедупликации: `exchange, symbol, price, volume, timestamp_utc`;
- query index для выборок по инструменту и времени: `symbol, timestamp_utc DESC`.

## Конфигурация

Основная конфигурация находится в `appsettings.json`.

Важные секции:

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=marketdata;Username=postgres;Password=postgres"
  },
  "Pipeline": {
    "RawChannelCapacity": 10000,
    "TickChannelCapacity": 10000,
    "NormalizerWorkerCount": 2
  },
  "Persistence": {
    "BatchSize": 250,
    "FlushIntervalMilliseconds": 500
  },
  "Deduplication": {
    "TtlSeconds": 60,
    "CleanupIntervalSeconds": 5
  }
}
```

Биржи настраиваются в секции `Exchanges`:

```json
{
  "Exchanges": {
    "Binance": {
      "Enabled": true,
      "Url": "wss://stream.binance.com:9443/ws/btcusdt@trade",
      "ReconnectDelaySeconds": 5
    }
  }
}
```

## Запуск через Docker Compose

Из папки проекта:

```powershell
cd C:\Users\mikhail\source\repos\MarketData\MarketData
docker compose up --build
```

Ожидаемые сообщения в логах:

```text
Initializing database schema
Database schema is ready
Starting 3 websocket exchange clients
Connected to Binance
Connected to Bybit
Connected to Kraken
Metrics: raw=..., processed=..., persisted=...
```

Остановить контейнеры:

```powershell
docker compose down
```

Остановить контейнеры и удалить PostgreSQL volume:

```powershell
docker compose down -v
```

Если нужно пересобрать worker без cache:

```powershell
docker compose build --no-cache marketdata
docker compose up
```

## Локальный запуск без контейнера приложения

Поднять только PostgreSQL:

```powershell
docker compose up -d postgres
```

Запустить worker локально:

```powershell
dotnet run --project MarketData.csproj
```

## Как проверить, что данные пишутся

Количество записей:

```powershell
docker compose exec postgres psql -U postgres -d marketdata -c "select count(*) from ticks;"
```

Последние тики:

```powershell
docker compose exec postgres psql -U postgres -d marketdata -c "select exchange, symbol, price, volume, timestamp_utc from ticks order by id desc limit 10;"
```

Структура таблицы:

```powershell
docker compose exec postgres psql -U postgres -d marketdata -c "\d ticks"
```

## Сборка и тесты

Собрать solution:

```powershell
dotnet build MarketData.sln
```

Запустить все тесты:

```powershell
dotnet test MarketData.sln
```

Запустить только тестовый проект:

```powershell
dotnet test tests\MarketData.Tests\MarketData.Tests.csproj
```

Покрытые сценарии:

- нормализация Binance trade message;
- нормализация Bybit trade batch;
- нормализация Kraken trade message;
- поиск normalizer по названию биржи;
- обнаружение дубликатов.

## Как добавить новую биржу

1. Добавить новый WebSocket-клиент в `Infrastructure/Exchanges`.
2. Реализовать `IWebSocketExchangeClient` напрямую или унаследоваться от `WebSocketExchangeClientBase`.
3. Добавить normalizer в `Infrastructure/Normalizers`.
4. Реализовать `IMessageNormalizer` и указать уникальное имя `Exchange`.
5. Зарегистрировать client и normalizer в `Program.cs`.
6. Добавить настройки в `appsettings.json` в секцию `Exchanges:<ExchangeName>`.

Ядро pipeline при этом менять не нужно.

## Отказоустойчивость

- Ошибка одного WebSocket-клиента не останавливает остальные подключения.
- При обрыве соединения клиент делает reconnect с настраиваемой задержкой.
- Ошибки парсинга считаются в метриках и пропускаются без падения процесса.
- Ошибки записи в БД логируются, после чего worker пробует сохранить батч снова.
- При остановке приложения persistence worker пытается сбросить накопленный буфер.

## Troubleshooting

### Docker пишет, что не найден `Microsoft.AspNetCore.App`

Нужно пересобрать image без cache:

```powershell
docker compose build --no-cache marketdata
docker compose up
```

Worker использует `Microsoft.NETCore.App` и должен запускаться на `mcr.microsoft.com/dotnet/runtime:8.0`.

### Тики не появляются в БД

Проверить логи и состояние контейнеров:

```powershell
docker compose logs marketdata
docker compose ps
docker compose exec postgres psql -U postgres -d marketdata -c "select count(*) from ticks;"
```

Если сеть блокирует WebSocket-подключения к биржам, приложение не должно падать. В логах будут ошибки подключения и попытки reconnect.

### Порт 5432 уже занят

Можно изменить внешний порт PostgreSQL в `docker-compose.yml`:

```yaml
ports:
  - "5433:5432"
```

Если worker запускается локально через `dotnet run`, после смены порта нужно также обновить connection string в `appsettings.json`.

## Ключевые проектные решения

- `Worker Service`, потому что это background data pipeline, а не HTTP API.
- `Channels`, потому что нужны async producer-consumer очереди и backpressure.
- `Dapper`, потому что для batch insert нужен простой контроль над SQL без overhead ORM.
- `Bounded channels`, потому что при всплесках нагрузки память не должна расти бесконечно.
- `Batch persistence`, потому что БД не должна получать отдельный round-trip на каждый тик.
- Отдельный client/normalizer на каждую биржу, потому что это упрощает добавление новых источников и сохраняет Open/Closed Principle.
