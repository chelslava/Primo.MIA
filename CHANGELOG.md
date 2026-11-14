# Primo.MIA - Selenium Browser Activities

## Что нового в версии 1.8.0

Реализация issues [#34–#37](https://github.com/chelslava/Primo.MIA/issues?q=is%3Aissue+milestone%3A1.8.0) и части [#24](https://github.com/chelslava/Primo.MIA/issues/24), [#26](https://github.com/chelslava/Primo.MIA/issues/26).
**+11 активностей (150 → 162), +139 тестов (1435 → 1574). Mail-модуль завершён (SMTP/IMAP/POP3/EML).**

### ✨ Новый модуль: Mail (4 активности) — MailKit (#37)

- **SmtpSend** — отправка писем по SMTP: вложения (пути/DataTable), TLS/
  STARTTLS, HTML-тело, SecureString-пароль (PasswordBox)
- **ImapReceive** — выборка писем с серверными фильтрами (тема/отправитель/
  SINCE/UNSEEN), выдача от новых к старым, «Отметить прочитанными», вложения
  в папку (подпапка «Письмо_N» на письмо), тело в переменную/файл
- **Pop3Receive** — простая выборка для legacy-ящиков: POP3 не умеет серверные
  поиск и флаги, поэтому фильтры применяются клиентски, выдача сортируется
  «от новых к старым» по дате; дедупликация при опросе — по колонке UID
  (UIDL); режим «Удалить после загрузки» (коммит при отключении)
- **EmlParse** — разбор локального .eml: тема/от/кому/дата/тело, вложения
  в папку
- Общий вывод: DataTable со сводкой писем, количество, тело последнего
  письма, пути вложений
- MailCommon — общие чистые операции (дата «С даты», тело письма, сохранение
  вложений) без сетевой части
- MailKit 4.18.2 (managed-only, MimeKit транзитивно; >= 4.16.0 закрывает
  STARTTLS-инъекцию GHSA-9j88-vvj5-vhgr/NU1902)

### 🔗 DataTable: объединение и развороты (#36)

- **DataTableJoin** — Inner/Left/Right/Full по ключевым колонкам
- **Pivot / Unpivot** — разворот строк в колонки и обратно

### 🌐 HtmlTableToDataTable (#36)

- Извлечение таблиц из HTML: rowspan/colspan, вложенные таблицы
- HtmlAgilityPack 1.12.2 (managed-only)

### 📋 JSON Schema: контрактный контроль (#34)

- **JsonSchemaValidate** — валидация JSON по схеме (NJsonSchema)
- **JsonSchemaGenerate** — генерация схемы из образца JSON

### 🔐 Crypto: PGP-файлы (#35)

- **Шифровать файл / Расшифровать файл** — OpenPGP (BouncyCastle.Cryptography
  2.7.0, уже приходил через SSH.NET): ASCII-armor, passphrase-ключи, связки

### 🔐 Crypto: аутентифицированное шифрование строки (AES-256-GCM)

- «Шифрование/Дешифрование строки» переведены с AES-CBC (без аутентификации)
  на **AES-256-GCM**: неверный пароль или изменение данных всегда обнаруживаются
  тегом аутентификации (старый формат давал «успешную» расшифровку мусора
  с вероятностью ~1/256 — нестабильный тест в CI)
- Новый формат `MIA2.<Base64([соль][nonce][шифртекст+тег])>`; строки старого
  CBC-формата продолжают расшифровываться (совместимость на чтение)
- BouncyCastle.Cryptography (уже в зависимостях через PGP/SSH.NET), новая
  зависимость не добавляется

## Что нового в версии 1.7.0

Реализация issues [#20–#33](https://github.com/chelslava/Primo.MIA/issues?q=is%3Aissue+milestone%3A1.7.0) и части #38.
**+14 активностей (≈120 → 134), +135 тестов (1300 → 1435), 4 новых категории в палитре Studio.**

### 🔒 Надёжность браузерных сессий (#20)

- **Целевой `Pop(sessionId)`**: контейнер закрывает СВОЮ сессию, а не вершину
  стека ambient-контекста — вложенные/чередующиеся контейнеры больше не снимают
  записи друг друга; закрытие по явному ID не портит текущий контекст
- Хранилище переведено с `ConcurrentStack` на lock-стек: стресс-тест выявил
  транзиентное окно «размотай-и-верни», где параллельный Pop не находил свою
  сессию
- **`BrowserSessionRegistry`**: TTL-выселение (24 ч) «заброшенных» сессий
  WebDriver по образцу `DatabaseTransactionManager` — при аварийной смерти
  робота между «Открыть» и «Закрыть браузер» драйвер больше не висит в системе
  навсегда (SDK не даёт хука на abort)
- `BrowserCloseBack`: Quit/Dispose через `SafeDispose.Quiet`

### 🧪 CI и тесты (#21)

- **Integration-гейтинг настоящий**: тесты с внешними системами помечаются
  `[Trait("Category", "Integration")]`; стандартный CI исключает их, новый
  workflow `integration.yml` (ручной/еженедельный) выполняет отдельно
- `run-tests-vs.ps1`: параметр `-AllowEmptyRun` для integration-джобы
- Три «Integration»-теста оказались чистыми сценариями — переименованы в
  `Scenario_*` и остались в быстром прогоне

### ✨ Новый модуль: Архивы (2 активности) — System.IO.Compression, без зависимостей (#28)

- **ZipArchiveCreate** — файлы/папки (рекурсивно, с именем папки как корнем
  записи) и маски через `;`; дедупликация записей; уровни сжатия
- **ZipArchiveExtract** — политики перезаписи (Пропускать/Перезаписывать/
  Ошибка), маска извлечения, **защита от Zip Slip**
- Ограничение честно задокументировано: пароли/AES стандартный .NET не
  поддерживает

### ✨ CSV ↔ DataTable (2 активности) — RFC 4180, без зависимостей (#29)

- **CsvToDataTable / DataTableToCsv**: кавычкованные поля с разделителями и
  переводами строк внутри, удвоение кавычек, границы CRLF (§2.1)/LF/CR на
  чтении; настраиваемый разделитель (`,`/`;`/`|`/таб, многосимвольные)
- Типизация колонок int → double → bool → DateTime (InvariantCulture) по
  образцу JsonDataTable; mixed-колонка остаётся string
- Кодировки: utf-8, windows-1251, utf-16; BOM определяется автоматически

### ✨ Текст: Regex (1 активность) (#30)

- **Match** → DataTable (Match, Index, группы 1..N, именованные группы
  отдельными колонками) и **Replace** с подстановками `$1`/`${name}`
- **Обязательный match timeout** (0 = 1000 мс, max 60 с) — катастрофический
  бэктрекинг прерывается, а не подвешивает робота (класс защиты по #24)
- Опции IgnoreCase/Multiline/Singleline

### ✨ Новый модуль: FTP (5 активностей) — FtpWebRequest, explicit TLS, без зависимостей (#31)

- **FtpsUpload / FtpsDownload / FtpsList / FtpsDelete / FtpsCreateFolder**
- Всегда: EnableSsl (explicit TLS), passive, binary; валидация сертификатов —
  стандартная (fail-closed). Опция «принять всё» сознательно не реализована:
  FtpWebRequest даёт её только через глобальный callback — гонки параллельных
  сессий могли бы принять чужой сертификат
- WebException маппится в понятные сообщения с FTP-статусом (530/550)

### ✨ HTTP: Ожидание условия + Пагинация (2 активности) (#32)

- **WaitForHttp** — poll-эндпоинта до условия (ожидаемый статус 0=любой 2xx,
  опциональная подстрока тела) с общим таймаутом и интервалом; сетевые ошибки
  не прерывают ожидание; для long-running API-задач
- **HttpPaginate** — итератор REST-пагинации: режимы **По номеру** ({page}) и
  **По курсору** ({cursor} из ответа по JSONPath), остановки: пустая страница /
  курсор не найден / лимит страниц; агрегация — сырой JSON элементов
- Обе логики — с инъекцией fetch-делегата: unit-тесты без сети и `Thread.Sleep`

### ✨ Очереди сообщений: батч Kafka + топология RabbitMQ (3 активности) (#33)

- **KafkaConsumeBatch** — до MaxMessages или истечения общего таймаута пакета
  (не на сообщение); явный коммит offset'ов при `AutoCommit=False` +
  `CommitAfterBatch`; выходы: тела `List<string>`, метаданные DataTable
- **RabbitMqDeclareQueue** — durable/exclusive/autoDelete + привязка к exchange
  по routing key; **RabbitMqPurgeQueue** — очистка с числом удалённых
- По образцу #19: общие базы подключения, маппинг ошибок брокера, SASL/TLS

### 🧹 Утилиты и документация (#38)

- **`SafeTry.Result`** — пара к `SafeDispose`: «попробуй, при ошибке верни
  fallback» с Debug-логом; 5 анонимных `catch { }` из аудита конвертированы
- Styles.xaml: канонические ключи конвертеров — одни на конвертер (длинные
  алиасы удалены)
- AGENTS.md актуализирован: устаревший пункт Console.WriteLine помечен FIXED,
  добавлена секция про единый UI (палитра, паттерн заголовков, Safe helpers)

### 🧪 Тесты

- **+135 тестов**: жизненный цикл сессий (13, вложенные/чередующиеся
  контейнеры, параллельный Pop 8×100), ZIP round-trip (20), CSV RFC 4180 (28),
  Regex + ReDoS (16), FTPS URI/листинг/конфигурация (15), HTTP poll/пагинация
  (26), Kafka батч + Rabbit топология на моках (11), SafeTry (6) —
  итого **1435 теста**
- Тестовые классы, трогающие общий ключ стека браузера, объединены в
  xUnit-коллекцию (раньше SessionIdTests рвал стек параллельному
  AmbientContextConcurrencyTests)

## Что нового в версии 1.6.0

### ✨ Новый модуль: Kafka (2 активности) — [Confluent.Kafka 2.12.0](https://www.nuget.org/packages/Confluent.Kafka) (issue #19)

Закрывает пробел официальной экосистемы Primo: пакет «Очереди сообщений» содержит
только MSMQ; RabbitMQ добавлен в Primo.MIA 1.3.0.

- **KafkaPublish** — публикация в топик: синхронная доставка с ack брокера,
  ключ → партиция, формат Строка/JSON (ContentType-заголовок), выходные
  Партиция/Offset
- **KafkaConsume** — ожидание одного сообщения с таймаутом; по таймауту
  `HasMessage=False` без исключения (удобно для опроса в цикле); Group ID,
  начальное смещение (Earliest/Latest) для новой группы; Auto Commit=False —
  режим «прочитать без удаления» (сообщение перечитается группой при следующем
  запуске); выходные Тело/Ключ/Offset (`топик#партиция@offset`)
- **Безопасность**: Plaintext / TLS / SASL (без TLS) / SASL+TLS; механизмы
  PLAIN, SCRAM-SHA-256, SCRAM-SHA-512 — все опциональны, валидация SASL-настроек
  до подключения
- Паттерн 3 файлов на активность, зеркально RabbitMQ-модулю;
  `KafkaLogic` — чистая логика (валидация, конфиги, маппинги), покрыта тестами
  без живого брокера

### 🧪 Тесты

- **+38 тестов KafkaLogic**: конфигурации Producer/Consumer (SASL/TLS-ключи),
  валидация (пустые bootstrap, SASL без пользователя/механизма, пустой Group ID),
  маппинги enum'ов, ContentType, обрезка описаний, ожидание сообщения через
  мок `IConsumer` (таймаут → null, unsubscribe при ошибке) — итого **1300 тестов**

## Что нового в версии 1.5.0

Реализация issues [#11–#18](https://github.com/chelslava/Primo.MIA/issues).

### 🔒 Безопасность (#11)

- **Обход TLS-валидации больше не «заражает» пул**: handler с
  `IgnoreSslErrors` не кэшируется (scoped, свой пул соединений), факт обхода
  пишется в лог робота (`HttpClientPool`, `WebhookBack`)
- **SFTP fail-closed**: пустой `ExpectedFingerprint` без явного
  `AllowAnyHostKey` теперь запрещает соединение (`SftpException`) — раньше
  молча принимался любой host key (дефолт SSH.NET `CanTrust=true`)
- **OAuth2**: убран дефолт-плейсхолдер `"client_secret"`; незамещённый
  плейсхолдер отсекается валидацией до запроса (`HttpHelper.IsOAuth2PlaceholderSecret`)

### 🐛 Исправления (#12, #13)

- **Случайные числа**: `new Random()` на каждый вызов (дубли значений при
  быстрых вызовах) → потокобезопасный `RandomHelper` (`ThreadLocal<Random>`,
  уникальный seed на поток) — Генераторы, Перемешивание списков
- **Sync-over-async**: `.ConfigureAwait(false)` добавлен в 6 мест
  (CalendarLoad, Http, OAuth2 Refresh) — единый стиль с остальной кодовой базой

### ✨ Русские имена режимов в UI (#14)

- **92 enum-типа (459 значений) получили атрибут `[RuName]`** — комбобоксы и
  заголовки показывают «Одиночный», «По возрастанию», «Равно» вместо
  `Single`, `Ascending`, `Equal`
- Новая инфраструктура: `RuNameAttribute`, `EnumValuesExtension`
  (замена ObjectDataProvider), `EnumDisplayNameConverter`
- **87 комбобоксов в 64 файлах мигрированы**; сериализация не изменена —
  атрибут влияет только на отображение

### 🎨 UX-доработки (#15)

- **Тултипы для ~207 полей** в 41 файле: Database ×15, MongoDB ×6, Redis ×9,
  SFTP ×5, RabbitMQ ×2, Json/Xml ×4 — раньше модули были без подсказок
- **Валидация числовых полей**: `NumericValidationRule` + `ValidatingFieldBox`
  на 31 поле (таймауты, размеры страниц/пакетов) — нечисловой ввод подсвечивается
  красным до запуска робота
- **Кнопка обзора файла** для локальных путей SFTP Download/Upload

### 🔧 Рефакторинг (#17)

- **`ActivityTimeoutParser`**: 33 копии парсинга таймаута заменены одним
  хелпером (Database ×12, MongoDB ×6, Redis ×9, SFTP ×5, RabbitMQ ×1)
- **`HttpHelper.SendWithRetryAsync`**: retry-цикл HttpRetry переведён на
  общий хелпер (Http и Webhook оставлены: иная семантика повторов)
- **`SafeDispose.Quiet`**: guarded-dispose блоки SFTP/RabbitMQ на одном хелпере
- **Guard**: 16 inline-throw в 14 Browser-файлах на `Guard.AnyNullOrWhiteSpace`/`Guard.That`

### 🧪 Тесты (#16)

- **+131 тест**: OAuth2-парсинг токена, SQL-генерация Database (BulkInsert/
  Upsert/Command), ExtractEntities, LogMessage-шаблоны — итого **1262 теста**
- **Integration-гейтинг**: 7 Calendar-тестов больше не висят в вечном Skip —
  включаются переменной окружения `PRIMO_INTEGRATION=1`

### 📦 Инфраструктура (#18)

- **PackageIcon**: NuGet-пакет публикуется с иконкой
- **Вытеснение кэшей**: брошенные DB-транзакции (TTL 24 ч, rollback+close при
  следующем Begin) и неактивные Redis-мультиплексоры (idle 30 мин, dispose
  через grace 30 мин) больше не живут вечно
- **ISO-неделя**: токен `{week}` в LogMessage — культуронезависимый
  (FirstFourDayWeek+Monday, InvariantCulture), одинаковый на ru-RU и en-US

## Что нового в версии 1.4.0

### 🔒 Безопасность

- **SSH.NET 2024.2.0 → 2026.0.0** — закрыты две уязвимости высокого уровня
  ([GHSA-mggc-4xg6-vcxf](https://github.com/advisories/GHSA-mggc-4xg6-vcxf),
  [GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284));
  конфликт `System.ValueTuple` решён `ExcludeAssets="all"`, добавлен bindingRedirect
  `System.Text.Encoding.CodePages`
- **Пароли больше не видны на экране**: во всех активностях с паролем
  (SFTP ×5, RabbitMQ ×2, AES-шифрование ×2) поле ввода переведено с `TextBox`
  на `PasswordBox` (символы скрыты; привязка через новый `PasswordBoxHelper`,
  сериализация свойств не изменилась)

### 🎨 Единый стиль всех активностей (UI/UX)

- **43 активности переведены на общий словарь стилей** `Common/Styles.xaml`
  (Browser ×21, HttpWeb ×7, Json ×2, Xml ×2 и дочистка заголовков Run) —
  убраны ~200 копий инлайновых `FontSize`/`FontWeight`/`Foreground`
- **Конвертеры централизованы**: `EnumToVisibilityConverter`,
  `MultiEnumToVisibilityConverter`, `BoolToVisibilityConverter`,
  `InverseBoolConverter` объявлены в Styles.xaml с каноническими ключами —
  локальные переобъявления с 5 вариантами имён больше не нужны
- **Локализация модуля «База данных»**: 15 XAML-файлов переведены на русский
  (`Provider invariant name:` → «Поставщик (invariant name):» и т.д.);
  все лейблы таймаутов унифицированы: «Таймаут (сек):»
- Мёртвые стили удалены, `EditableComboBox` и `ModePanel` больше не вводят в заблуждение

### 🔧 Версионость и инфраструктура

- **csproj-гигиена**: убраны дубли `GeneratePackageOnBuild` и избыточные
  Debug/Release-свойства; путь к SDK параметризован (`/p:PrimoSdkPath=...`)
- **`scripts/bump-version.ps1`**: автоматизирует шаги 1–2 релизной процедуры
  (валидация SemVer → `<Version>` в csproj → секция CHANGELOG)
- Прекомпиляция XAML проходит без ошибок, сборка: 0 warnings NuGet-графа

### 🧪 Тесты

- 1131 тест: регрессионный прогон после миграции UI и bump'а SSH.NET —
  без отказов (SFTP/Crypto-наборы 44/44 на SSH.NET 2026.0.0)

## Что нового в версии 1.3.0

### ✨ Новый модуль: SFTP (5 активностей) — SSH.NET 2024.2.0

- **SftpUpload / SftpDownload / SftpList / SftpDelete / SftpCreateFolder**
- Аутентификация: пароль **или** приватный ключ (OpenSSH/PEM) + парольная фраза
- Проверка host key по SHA256-отпечатку (`ssh-keygen -lf`) либо отключение для тестовых сред
- Таймауты подключения/операции, рекурсивное удаление каталогов
- Закрывает пробел официальной экосистемы: FTP-элемент Primo SSH не поддерживает

### ✨ Новый модуль: Криптография (5 активностей) — без новых зависимостей

- **HashCalculate** — MD5/SHA1/SHA256/SHA384/SHA512, строка или файл (потоково), hex/Base64
- **EncryptString / DecryptString** — AES-256-CBC, ключ из пароля через PBKDF2-SHA256
  (100 000 итераций), случайная соль; формат Base64([salt][IV][ciphertext])
- **Base64Encode / Base64Decode**

### ✨ Новый модуль: Очереди сообщений — RabbitMQ (2 активности)

- **RabbitMqPublish** — exchange/routing key, persistent-доставка, TLS, vhost
- **RabbitMqConsume** — ожидание одного сообщения с таймаутом; по таймауту
  `HasMessage=False` без исключения (удобно для опроса в цикле);
  AutoAck=False = режим «прочитать без удаления»

### 🔧 Надёжность и качество

- **Пул HttpClient**: HttpClientHandler кэшируется по конфигурации с TTL-ротацией
  10 мин и grace-периодом для in-flight запросов — устранён антипаттерн
  «HttpClient на каждый запрос» (исчерпание сокетов под нагрузкой)
- **Потокобезопасность ambient-контекстов**: `BrowserSessionContext` и
  `DatabaseTransactionContext` переведены на ConcurrentStack — Push/Pop безопасны
  из параллельных веток робота
- **Таймауты**: настраиваемые таймауты в 4 Database-активностях метаданных
  (ListTables/ListColumns/TableExists/GetTableSchema), 5 MongoDB-активностях
  и 9 Redis-активностях
- **Файловый логгер** `ActivityFileLogger`: лог активностей в файл с ротацией 10 МБ
  (включение `PRIMO_MIA_LOG_FILE=1`); Console.WriteLine больше не единственный sink
- Гигиена исключений: сужены пустые catch, голые `throw new Exception` заменены
  на `CalendarParseException`

### 🧪 Тесты

- 71 новый тест: крипто (23, эталонные векторы), SFTP (21), RabbitMQ (12),
  конкурентность ambient-контекстов (4), Browser Wait/Interaction (11), файловый
  логгер (5+) — итог: **1131 тест**

### 📦 Версионирование

- Единый источник версии в csproj: `AssemblyVersion`/`FileVersion` наследуются
  от `<Version>`, prerelease-суффиксы тегов отсекаются автоматически
- CI проверяет отсутствие хардкода версий и наличие секции CHANGELOG;
  релиз по тегу блокируется без записи в CHANGELOG

## Что нового в версии 1.2.0

### ✨ Новая активность: DataTable: Фильтрация (DataTableFilter)

Фильтрация строк DataTable с **сохранением исходного порядка строк** (LINQ,
без `DataTable.Select`). Возвращает две таблицы: прошедшие и не прошедшие
фильтр + счётчики. Исходная таблица не изменяется.

#### Два режима задания условий

- **SqlQuery** — SQL-подобное выражение (аналог WHERE):
  ```
  Age >= 18 AND Name LIKE 'Ив%' OR (City IN ('Москва', 'Казань') AND Active = true)
  ```
  Операторы: `=`, `==`, `<>`, `!=`, `>`, `<`, `>=`, `<=`, `LIKE` (`%`, `_`),
  `IN (...)`, `IS NULL` / `IS NOT NULL`.
  Логика: `AND` / `И`, `OR` / `ИЛИ`, `NOT` / `НЕ`, скобки.
  Литералы: `'строка'`, `"строка"`, числа, `true/false`;
  имена колонок с пробелами/ключевые слова — `[в скобках]`.

- **ConditionBuilder** — форма в самой активности: строки
  «Колонка + Оператор + Значение» со связкой И/ИЛИ, добавление/удаление строк.
  Операторы: `=`, `<>`, `>`, `>=`, `<`, `<=`, `Contains`, `StartsWith`,
  `EndsWith`, `Like`, `In`, `IsNull`, `IsNotNull`.

#### Прочее
- Сравнение с приведением к типу колонки (числа, DateTime, bool, строки)
- Регистронезависимое сравнение строк по умолчанию (флаг «Учитывать регистр»)
- Ошибки выражений — с позицией в тексте (`UtilityException`)
- Тесты: 61 новый сценарий (порядок строк, операторы, типы, ошибки, DSL)
- Обновление зависимостей: Selenium.WebDriver/Support 4.44.0 → 4.52.0,
  FluentAssertions 7.2.0 (Apache-2.0; ранее 8.x — платная Xceed License),
  MongoDB.Driver 2.30.0, StackExchange.Redis 2.8.41, Npgsql 7.0.8 в явных зависимостях

## Что нового в версии 1.1.0

### ✨ Новая категория: Браузер (22 активности)

Полная поддержка автоматизации веб-браузеров через Selenium WebDriver.

#### Управление браузером
- **BrowserOpen** — открытие браузера (Chrome, Firefox, Edge)
- **BrowserClose** — закрытие браузера
- **BrowserNavigate** — переход по URL
- **BrowserGetInfo** — получение информации о странице
- **BrowserScreenshot** — создание скриншотов

#### Поиск элементов
- **ElementFind** — поиск элемента с ожиданием
- **ElementFindAll** — поиск всех элементов
- **ElementExists** — проверка существования
- **ElementGetProperty** — получение свойств элемента
- **ElementIsVisible** — проверка состояния

#### Взаимодействие
- **ElementClick** — клик по элементу
- **ElementTypeText** — ввод текста с эмуляцией печати
- **ElementClear** — очистка поля
- **ElementSelect** — работа с выпадающими списками
- **ElementSubmit** — отправка формы

#### Продвинутые функции
- **BrowserExecuteJavaScript** — выполнение JS кода
- **ElementScrollTo** — прокрутка к элементу
- **BrowserWaitFor** — универсальное ожидание (8 типов условий)
- **BrowserManageCookies** — управление cookies
- **AlertHandle** — обработка алертов
- **ElementHover** — наведение курсора
- **BrowserSwitchTo** — переключение контекста (frames, windows)

### 🎯 Ключевые возможности

- **8 типов локаторов**: Id, Name, ClassName, TagName, LinkText, PartialLinkText, CssSelector, XPath
- **Headless режим**: фоновое выполнение без GUI
- **Режим инкогнито**: приватный режим браузера
- **Эмуляция печати**: посимвольный ввод текста
- **Явные ожидания**: 8 типов условий ожидания
- **Управление cookies**: получение, установка, удаление
- **JavaScript выполнение**: полный доступ к DOM
- **Переключение контекста**: работа с iframe и popup окнами

### 📦 Зависимости

```xml
<package id="Selenium.WebDriver" version="4.16.2" />
<package id="Selenium.Support" version="4.16.2" />
```

### 🚀 Быстрый старт

```
1. BrowserOpen (Chrome) → sessionId
2. BrowserNavigate ("https://example.com")
3. ElementFind (Id: "search") → searchBox
4. ElementTypeText (searchBox, "Primo RPA")
5. ElementFind (Id: "submit") → submitBtn
6. ElementClick (submitBtn)
7. BrowserClose (sessionId)
```

### 📚 Документация

- [Browser/README.md](Browser/README.md) — полная документация
- [Browser/QUICK_START.md](Browser/QUICK_START.md) — примеры использования
- [Browser/IMPLEMENTATION_SUMMARY.md](Browser/IMPLEMENTATION_SUMMARY.md) — технические детали

### ⚙️ Требования

- .NET Framework 4.6.1+
- Установленный браузер (Chrome/Firefox/Edge)
- Соответствующий WebDriver в PATH

### 🔧 Установка WebDriver

**Chrome:**
```powershell
# Скачать с https://chromedriver.chromium.org/
# Или через Chocolatey:
choco install chromedriver
```

**Firefox:**
```powershell
# Скачать с https://github.com/mozilla/geckodriver/releases
# Или через Chocolatey:
choco install selenium-gecko-driver
```

**Edge:**
```powershell
# Скачать с https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/
```

### 💡 Примеры сценариев

#### Авторизация на сайте
```
BrowserOpen → Navigate → Find(username) → TypeText → 
Find(password) → TypeText → Find(submit) → Click → 
WaitFor(UrlContains: "/dashboard") → Close
```

#### Парсинг данных
```
BrowserOpen → Navigate → FindAll(ClassName: "product") → 
ForEach(element → GetProperty("text")) → Close
```

#### Заполнение формы
```
BrowserOpen → Navigate → Find(name) → TypeText → 
Find(email) → TypeText → Find(country) → Select(ByText) → 
Find(submit) → Click → Close
```

### 🐛 Известные проблемы

1. **WebDriver версия**: Убедитесь что версия драйвера соответствует версии браузера
2. **Headless детекция**: Некоторые сайты блокируют headless браузеры
3. **Stale элементы**: Элементы становятся недействительными после обновления страницы

### 🔄 Миграция с других решений

Если вы использовали другие библиотеки для автоматизации браузера:

**Из UiPath:**
- UiPath.Core.Activities.OpenBrowser → BrowserOpen
- UiPath.Core.Activities.Click → ElementClick
- UiPath.Core.Activities.TypeInto → ElementTypeText

**Из Selenium напрямую:**
- driver.FindElement() → ElementFind
- element.Click() → ElementClick
- element.SendKeys() → ElementTypeText

### 📈 Производительность

- Headless режим: **+40% скорость**
- Отключение изображений: **+25% скорость**
- Явные ожидания: **-60% ложных ошибок**

### 🎓 Обучение

1. Начните с [QUICK_START.md](Browser/QUICK_START.md)
2. Изучите примеры в [README.md](Browser/README.md)
3. Экспериментируйте с разными локаторами
4. Используйте BrowserScreenshot для отладки

---

## Остальные категории

### Словари (11 активностей)
Работа с Dictionary<string, string>: создание, фильтрация, слияние, конвертация.

### Списки (9 активностей)
Обработка List<string>: фильтрация, сортировка, группировка, агрегация, срезы.

### Кортежи (9 активностей)
Работа с Tuple: создание, деструктуризация, конвертация, сортировка списков кортежей.

### HTTP/Web (1 активность)
HTTP запросы с поддержкой всех методов, заголовков и клиентских сертификатов.

### Файлы (2 активности)
Поиск файлов и ожидание появления файлов с фильтрацией.

### Утилиты (4 активности)
Логирование, чтение TOML конфигураций, генераторы, пересчёт Excel ячеек.

---

## Поддержка

- **Issues**: [GitLab Issues](https://gitlab.com/chelslava/Primo.MIA/issues)
- **Документация**: См. папку `doc/`
- **Примеры**: См. README файлы в каждой категории

## Лицензия

MIT License

## Авторы

Primo.MIA Team
