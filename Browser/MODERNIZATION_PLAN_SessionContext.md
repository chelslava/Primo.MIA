# План модернизации активностей браузера для поддержки BrowserSessionContext

## Обзор

Этот документ описывает план модернизации всех активностей браузера для поддержки автоматической передачи sessionId через ambient-контекст `BrowserSessionContext`.

## Текущее состояние

### Уже модернизировано ✅
- **BrowserOpenBack** — регистрирует сессию через `BrowserSessionContext.Push(sessionId)`
- **ElementInputBack** — использует `SessionResolver.Resolve()` для получения sessionId
- **SessionResolver** — реализован и готов к использованию
- **BrowserSessionContext** — реализован ambient-контекст со стеком сессий

### Требует модернизации ⚠️
28 активностей используют старый подход с прямым чтением `Prop_SessionId` и ручной проверкой на пустоту.

## Паттерн модернизации

### Было (старый подход):
```csharp
string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
if (string.IsNullOrWhiteSpace(sessionId))
    throw new ArgumentException("ID сессии не задан");
```

### Стало (новый подход):
```csharp
string sessionId = SessionResolver.Resolve(
    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
```

### Преимущества нового подхода:
1. **Автоматическое разрешение** — если `Prop_SessionId` пуст, берётся из `BrowserSessionContext.Current`
2. **Меньше кода** — не нужна ручная проверка на пустоту
3. **Единообразие** — все активности используют один механизм
4. **Понятные ошибки** — `SessionResolver` выдаёт информативное сообщение
5. **Поддержка двух способов** — явная переменная ИЛИ ambient-контекст

## Список активностей для модернизации

### Группа 1: Browser-уровень (9 активностей)

#### 1.1 AlertHandleBack.cs
- **Строка:** 138
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```
- **Удалить:** Проверку `if (string.IsNullOrWhiteSpace(sessionId))` (если есть)

#### 1.2 BrowserCloseBack.cs
- **Строка:** 84
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```
- **Дополнительно:** Добавить `BrowserSessionContext.Pop()` в блок finally после закрытия браузера

#### 1.3 BrowserExecuteJavaScriptBack.cs
- **Строка:** 134
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 1.4 BrowserGetInfoBack.cs
- **Строка:** 130
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 1.5 BrowserGetLogsBack.cs
- **Строка:** 143
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 1.6 BrowserManageCookiesBack.cs
- **Строка:** 158
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 1.7 BrowserNavigateBack.cs
- **Строка:** 118
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 1.8 BrowserScreenshotBack.cs
- **Строка:** 120
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 1.9 BrowserSwitchToBack.cs
- **Строка:** 146
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

### Группа 2: Browser Management (4 активности)

#### 2.1 BrowserStorageManageBack.cs
- **Строка:** 199
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 2.2 BrowserTabManageBack.cs
- **Строка:** 198
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 2.3 BrowserWaitForBack.cs
- **Строка:** 216
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 2.4 BrowserWindowManageBack.cs
- **Строка:** 225
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

### Группа 3: Element-уровень (15 активностей)

#### 3.1 ElementClickBack.cs
- **Строка:** 272
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.2 ElementDragDropBack.cs
- **Строка:** 238
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  if (string.IsNullOrWhiteSpace(sessionId))
      throw new ArgumentException("ID сессии не задан");
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.3 ElementExistsBack.cs
- **Строка:** 133
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.4 ElementFindBack.cs
- **Строка:** 247
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.5 ElementGetComputedStyleBack.cs
- **Строка:** 186
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.6 ElementGetInfoBack.cs
- **Строка:** 247
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.7 ElementGetPropertyBack.cs
- **Строка:** ~150 (требуется проверка)
- **Применить стандартный паттерн**

#### 3.8 ElementGetRectBack.cs
- **Строка:** 212
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.9 ElementGetScreenshotBack.cs
- **Строка:** 185
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.10 ElementHoverBack.cs
- **Строка:** 248
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.11 ElementIsVisibleBack.cs
- **Строка:** 197
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.12 ElementScrollToBack.cs
- **Строка:** 169
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.13 ElementSelectBack.cs
- **Строка:** 185
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.14 ElementSelectMultipleBack.cs
- **Строка:** 216
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.15 ElementSubmitBack.cs
- **Строка:** 150
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

#### 3.16 ElementUploadFileBack.cs
- **Строка:** 180
- **Текущий код:**
  ```csharp
  string sessionId = GetPropertyValue<string>(this.Prop_SessionId, "Prop_SessionId", sd);
  ```
- **Новый код:**
  ```csharp
  string sessionId = SessionResolver.Resolve(
      GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
  ```

## Порядок выполнения модернизации

### Этап 1: Подготовка (выполнено ✅)
- [x] Реализован `BrowserSessionContext`
- [x] Реализован `SessionResolver`
- [x] Модернизирован `BrowserOpenBack` (Push контекста)
- [x] Модернизирован `ElementInputBack` (пример использования)

### Этап 2: Модернизация Browser-уровня
**Приоритет:** Высокий  
**Порядок выполнения:**
1. BrowserCloseBack (важно — добавить Pop)
2. BrowserNavigateBack
3. BrowserGetInfoBack
4. BrowserExecuteJavaScriptBack
5. BrowserScreenshotBack
6. BrowserSwitchToBack
7. AlertHandleBack
8. BrowserManageCookiesBack
9. BrowserGetLogsBack

### Этап 3: Модернизация Browser Management
**Приоритет:** Средний  
**Порядок выполнения:**
1. BrowserWindowManageBack
2. BrowserTabManageBack
3. BrowserStorageManageBack
4. BrowserWaitForBack

### Этап 4: Модернизация Element-уровня
**Приоритет:** Средний  
**Порядок выполнения:**
1. ElementFindBack (часто используется)
2. ElementClickBack (часто используется)
3. ElementGetInfoBack
4. ElementExistsBack
5. ElementIsVisibleBack
6. ElementHoverBack
7. ElementScrollToBack
8. ElementSelectBack
9. ElementSelectMultipleBack
10. ElementSubmitBack
11. ElementUploadFileBack
12. ElementDragDropBack
13. ElementGetComputedStyleBack
14. ElementGetRectBack
15. ElementGetScreenshotBack
16. ElementGetPropertyBack

## Чек-лист для каждой активности

При модернизации каждой активности проверить:

- [ ] Заменить прямое чтение `Prop_SessionId` на `SessionResolver.Resolve()`
- [ ] Использовать `nameof(Prop_SessionId)` вместо строкового литерала
- [ ] Удалить ручную проверку `if (string.IsNullOrWhiteSpace(sessionId))`
- [ ] Удалить ручной `throw new ArgumentException` для sessionId
- [ ] Проверить, что нет других мест чтения sessionId в файле
- [ ] Убедиться, что using для `Primo.MIA` добавлен (для SessionResolver)
- [ ] Протестировать оба способа: с явной переменной и без

## Особые случаи

### BrowserCloseBack — дополнительная логика
Помимо замены на `SessionResolver.Resolve()`, необходимо добавить:
```csharp
try
{
    string sessionId = SessionResolver.Resolve(
        GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));
    
    // ... закрытие браузера ...
}
finally
{
    // Снимаем сессию из ambient-контекста
    BrowserSessionContext.Pop();
}
```

### ElementGetPropertyBack
Требуется проверка наличия метода Execute и точной строки чтения sessionId.

## Тестирование

После модернизации каждой группы необходимо протестировать:

### Тест 1: Автоматический режим (ambient-контекст)
```
[BrowserOpenBack]
    Prop_SessionId = (пусто)
  ┌─────────────────────────────┐
  │ [Модернизированная активность] │
  │     Prop_SessionId = (пусто)   │  ← должна работать
  └─────────────────────────────┘
```

### Тест 2: Явный режим (переменная)
```
[BrowserOpenBack]
    Prop_SessionId = mySession
  ┌─────────────────────────────┐
  │ [Модернизированная активность] │
  │     Prop_SessionId = mySession │  ← должна работать
  └─────────────────────────────┘
```

### Тест 3: Ошибка (нет контекста и переменной)
```
[Модернизированная активность]
    Prop_SessionId = (пусто)  ← должна выдать понятную ошибку
```

## Метрики прогресса

- **Всего активностей:** 29
- **Модернизировано:** 1 (ElementInputBack)
- **Осталось:** 28
- **Прогресс:** 3.4%

### По группам:
- Browser-уровень: 0/9 (0%)
- Browser Management: 0/4 (0%)
- Element-уровень: 1/16 (6.25%)

## Ожидаемые результаты

После завершения модернизации:

1. **Упрощение использования** — пользователи могут не заполнять `Prop_SessionId` в дочерних активностях
2. **Единообразие кода** — все активности используют один паттерн
3. **Меньше ошибок** — автоматическое разрешение sessionId снижает вероятность ошибок конфигурации
4. **Обратная совместимость** — старый способ с явной переменной продолжает работать
5. **Понятные ошибки** — `SessionResolver` выдаёт информативные сообщения

## Риски и митигация

### Риск 1: Нарушение обратной совместимости
**Митигация:** `SessionResolver` поддерживает оба способа — новый код не ломает существующие сценарии.

### Риск 2: Забыть Pop в BrowserCloseBack
**Митигация:** Использовать блок finally для гарантированного вызова Pop.

### Риск 3: Пропустить активность
**Митигация:** Использовать search_files для поиска всех вхождений `GetPropertyValue.*Prop_SessionId`.

## Следующие шаги

1. Начать с группы Browser-уровня (высокий приоритет)
2. Модернизировать по 3-5 активностей за раз
3. После каждой группы — запустить тесты
4. Обновлять метрики прогресса в этом документе
5. После завершения — обновить документацию пользователя

## Ссылки

- **HowTo_SessionTransfer.cs** — описание механизма передачи sessionId
- **BrowserSessionContext.cs** — реализация ambient-контекста
- **SessionResolver.cs** — вспомогательный класс разрешения
- **BrowserOpenBack.cs** — пример Push контекста
- **ElementInputBack.cs** — пример использования SessionResolver
