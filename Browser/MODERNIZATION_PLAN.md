# План модернизации Browser активностей

## Цель
Объединить однотипные активности в единые активности с выбором режима через enum, чтобы уменьшить количество файлов и упростить поддержку кода.

## Текущее состояние

В проекте уже есть успешный пример модернизации:
- **ElementClick** - объединяет 4 типа кликов через enum `ClickMode` (Click, DoubleClick, RightClick, ClickAndHold)

В файле `Enums.cs` уже определены следующие enum для Browser активностей:
- `CookieOperation` - операции с cookies (Get, GetAll, Set, Delete, DeleteAll)
- `TabOperation` - операции с вкладками (OpenNewTab, CloseCurrentTab, CloseTabByHandle, GetAllHandles, GetCurrentHandle, SwitchToTab)
- `StorageOperation` - операции с Web Storage (GetItem, SetItem, RemoveItem, Clear, GetAllKeys, GetLength)
- `WindowOperation` - операции с окном (Maximize, Minimize, FullScreen, SetSize, SetPosition, GetSize, GetPosition)
- `SwitchToType` - переключение контекста (Frame, Window, Alert, DefaultContent, ParentFrame)
- `AlertAction` - действия с алертами (Accept, Dismiss, GetText, SendKeys)
- `WaitConditionType` - условия ожидания (ElementExists, ElementVisible, ElementClickable, ElementInvisible, TextPresent, TitleContains, UrlContains, AlertPresent)
- `MultiSelectOperation` - операции с multiple select (SelectByText, SelectByValue, SelectByIndex, DeselectByText, DeselectByValue, DeselectByIndex, DeselectAll, GetAllOptions, GetSelectedOptions)

## Группы активностей для объединения

### 1. ✅ ElementClick (УЖЕ РЕАЛИЗОВАНО)
**Текущие активности:**
- ElementClick
- ElementDoubleClick
- ElementRightClick
- ElementClickAndHold

**Объединенная активность:** ElementClick
**Enum:** `ClickMode` (Click, DoubleClick, RightClick, ClickAndHold)
**Статус:** ✅ Реализовано

---

### 2. BrowserManageCookies (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** BrowserManageCookies
**Enum:** `CookieOperation` (Get, GetAll, Set, Delete, DeleteAll)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 3. BrowserTabManage (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** BrowserTabManage
**Enum:** `TabOperation` (OpenNewTab, CloseCurrentTab, CloseTabByHandle, GetAllHandles, GetCurrentHandle, SwitchToTab)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 4. BrowserStorageManage (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** BrowserStorageManage
**Enum:** `StorageOperation` (GetItem, SetItem, RemoveItem, Clear, GetAllKeys, GetLength)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 5. BrowserWindowManage (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** BrowserWindowManage
**Enum:** `WindowOperation` (Maximize, Minimize, FullScreen, SetSize, SetPosition, GetSize, GetPosition)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 6. BrowserSwitchTo (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** BrowserSwitchTo
**Enum:** `SwitchToType` (Frame, Window, Alert, DefaultContent, ParentFrame)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 7. AlertHandle (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** AlertHandle
**Enum:** `AlertAction` (Accept, Dismiss, GetText, SendKeys)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 8. BrowserWaitFor (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** BrowserWaitFor
**Enum:** `WaitConditionType` (ElementExists, ElementVisible, ElementClickable, ElementInvisible, TextPresent, TitleContains, UrlContains, AlertPresent)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 9. ElementSelectMultiple (УЖЕ ИСПОЛЬЗУЕТ ENUM)
**Текущая активность:** ElementSelectMultiple
**Enum:** `MultiSelectOperation` (SelectByText, SelectByValue, SelectByIndex, DeselectByText, DeselectByValue, DeselectByIndex, DeselectAll, GetAllOptions, GetSelectedOptions)
**Статус:** ✅ Уже использует enum, модернизация не требуется

---

### 10. 🔄 ElementHover - КАНДИДАТ НА ОБЪЕДИНЕНИЕ
**Текущие активности:**
- ElementHover
- ElementHoverWithOffset

**Предлагаемое решение:**
- Объединить в одну активность **ElementHover**
- Добавить enum `HoverMode`:
  ```csharp
  public enum HoverMode
  {
      /// <summary>Навести на центр элемента</summary>
      Center,
      /// <summary>Навести с указанием смещения от центра</summary>
      WithOffset
  }
  ```
- При режиме `WithOffset` использовать свойства `Prop_OffsetX` и `Prop_OffsetY`

**Файлы для изменения:**
- `Enums.cs` - добавить enum `HoverMode`
- `Browser/ElementHoverBack.cs` - объединить логику
- `Browser/ElementHover.xaml` - обновить UI
- `Browser/ElementHover.xaml.cs` - обновить code-behind
- Удалить: `ElementHoverWithOffset.*`

---

### 11. 🔄 ElementInput - НОВАЯ ОБЪЕДИНЕННАЯ АКТИВНОСТЬ
**Текущие активности:**
- ElementTypeText
- ElementSendKeys
- ElementClear

**Предлагаемое решение:**
- Создать новую активность **ElementInput**
- Добавить enum `InputMode`:
  ```csharp
  public enum InputMode
  {
      /// <summary>Ввести текст (очистить поле и ввести)</summary>
      TypeText,
      /// <summary>Отправить клавиши (включая специальные)</summary>
      SendKeys,
      /// <summary>Очистить поле ввода</summary>
      Clear
  }
  ```

**Файлы для создания:**
- Обновить `Enums.cs` - добавить enum `InputMode`
- Создать `Browser/ElementInput.xaml`
- Создать `Browser/ElementInput.xaml.cs`
- Создать `Browser/ElementInputBack.cs`

**Файлы для удаления (после миграции):**
- `ElementTypeText.*`
- `ElementSendKeys.*`
- `ElementClear.*`

---

### 12. 🔄 ElementGetInfo - НОВАЯ ОБЪЕДИНЕННАЯ АКТИВНОСТЬ
**Текущие активности:**
- ElementGetProperty
- ElementGetComputedStyle
- ElementGetRect

**Предлагаемое решение:**
- Создать новую активность **ElementGetInfo**
- Добавить enum `ElementInfoMode`:
  ```csharp
  public enum ElementInfoMode
  {
      /// <summary>Получить свойство элемента (атрибут, текст, value)</summary>
      Property,
      /// <summary>Получить вычисленный CSS-стиль</summary>
      ComputedStyle,
      /// <summary>Получить размеры и позицию элемента</summary>
      Rectangle
  }
  ```

**Файлы для создания:**
- Обновить `Enums.cs` - добавить enum `ElementInfoMode`
- Создать `Browser/ElementGetInfo.xaml`
- Создать `Browser/ElementGetInfo.xaml.cs`
- Создать `Browser/ElementGetInfoBack.cs`

**Файлы для удаления (после миграции):**
- `ElementGetProperty.*`
- `ElementGetComputedStyle.*`
- `ElementGetRect.*`

---

### 13. 🔄 BrowserNavigate - РАСШИРЕНИЕ СУЩЕСТВУЮЩЕЙ
**Текущие активности:**
- BrowserNavigate (только переход по URL)
- Отсутствуют: Back, Forward, Refresh

**Предлагаемое решение:**
- Расширить активность **BrowserNavigate**
- Добавить enum `NavigateMode`:
  ```csharp
  public enum NavigateMode
  {
      /// <summary>Перейти по URL</summary>
      ToUrl,
      /// <summary>Назад в истории</summary>
      Back,
      /// <summary>Вперед в истории</summary>
      Forward,
      /// <summary>Обновить страницу</summary>
      Refresh
  }
  ```

**Файлы для изменения:**
- `Enums.cs` - добавить enum `NavigateMode`
- `Browser/BrowserNavigateBack.cs` - добавить логику Back/Forward/Refresh
- `Browser/BrowserNavigate.xaml` - обновить UI

---

### 14. 🔄 BrowserGetInfo - РАСШИРЕНИЕ СУЩЕСТВУЮЩЕЙ
**Текущая активность:**
- BrowserGetInfo (получает Title, URL, PageSource)

**Предлагаемое решение:**
- Расширить enum для более явного выбора:
  ```csharp
  public enum BrowserInfoType
  {
      /// <summary>Заголовок страницы</summary>
      Title,
      /// <summary>Текущий URL</summary>
      CurrentUrl,
      /// <summary>HTML-код страницы</summary>
      PageSource,
      /// <summary>Все три параметра сразу</summary>
      All
  }
  ```

**Файлы для изменения:**
- `Enums.cs` - добавить enum `BrowserInfoType`
- `Browser/BrowserGetInfoBack.cs` - добавить режим выбора

---

## Приоритеты реализации

### Высокий приоритет (максимальная польза)
1. **ElementInput** - объединяет 3 часто используемые активности
2. **ElementGetInfo** - объединяет 3 активности получения информации
3. **ElementHover** - объединяет 2 активности

### Средний приоритет (улучшение UX)
4. **BrowserNavigate** - расширение функциональности
5. **BrowserGetInfo** - улучшение выбора данных

---

## Преимущества модернизации

1. **Уменьшение количества файлов** - с ~90 до ~70 файлов в папке Browser
2. **Упрощение навигации** - меньше активностей в палитре
3. **Единообразие** - все активности используют enum для выбора режима
4. **Легкость поддержки** - вся логика в одном месте
5. **Расширяемость** - легко добавлять новые режимы

---

## Этапы реализации

### Этап 1: Подготовка (1-2 часа)
- ✅ Анализ текущих активностей
- ✅ Создание плана модернизации
- Добавление всех необходимых enum в `Enums.cs`

### Этап 2: Объединение ElementHover (2-3 часа)
- Добавить enum `HoverMode`
- Модифицировать `ElementHoverBack.cs`
- Обновить XAML и code-behind
- Тестирование
- Удалить старые файлы

### Этап 3: Создание ElementInput (3-4 часа)
- Добавить enum `InputMode`
- Создать новую активность
- Перенести логику из трех активностей
- Тестирование
- Удалить старые файлы

### Этап 4: Создание ElementGetInfo (3-4 часа)
- Добавить enum `ElementInfoMode`
- Создать новую активность
- Перенести логику из трех активностей
- Тестирование
- Удалить старые файлы

### Этап 5: Расширение BrowserNavigate (2 часа)
- Добавить enum `NavigateMode`
- Добавить логику Back/Forward/Refresh
- Тестирование

### Этап 6: Улучшение BrowserGetInfo (1-2 часа)
- Добавить enum `BrowserInfoType`
- Добавить режим выбора данных
- Тестирование

### Этап 7: Документация и финализация (2-3 часа)
- Обновить документацию
- Обновить CHANGELOG.md
- Создать migration guide для пользователей
- Финальное тестирование

---

## Оценка трудозатрат

**Общее время:** 15-20 часов работы

**Результат:**
- Сокращение количества файлов: ~20 файлов
- Улучшение архитектуры
- Единообразный подход ко всем активностям

---

## Обратная совместимость

⚠️ **Важно:** Старые активности должны быть помечены как `[Obsolete]` с указанием на новые активности, чтобы дать пользователям время на миграцию.

Пример:
```csharp
[Obsolete("Используйте ElementInput с режимом InputMode.TypeText")]
public class ElementTypeTextBack : PrimoComponentTO<ElementTypeText>
{
    // ...
}
```

---

## Заключение

Проект уже находится на правильном пути - большинство активностей уже используют enum для выбора режима. Осталось модернизировать только несколько групп активностей, что значительно улучшит структуру и удобство использования библиотеки.
