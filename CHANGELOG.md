# Primo.MIA - Selenium Browser Activities

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
