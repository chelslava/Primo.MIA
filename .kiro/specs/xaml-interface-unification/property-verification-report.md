# Property Verification Report
## Task 1.3: Проверка существующих Back.cs файлов на наличие необходимых свойств

**Дата проверки:** 2026-03-13  
**Статус:** ✅ ЗАВЕРШЕНО

---

## Результаты проверки

### 1. Locator-Based Activities (13 активностей)

Все 13 активностей должны иметь свойства:
- `Prop_LocatorType` (ElementLocatorType)
- `Prop_LocatorValue` (string)

| # | Активность | Prop_LocatorType | Prop_LocatorValue | Статус |
|---|------------|------------------|-------------------|--------|
| 1 | ElementExists | ✅ Есть (строка 48) | ✅ Есть (строка 58) | ✅ OK |
| 2 | ElementIsVisible | ✅ Есть (строка 88) | ✅ Есть (строка 98) | ✅ OK |
| 3 | ElementSelect | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 4 | ElementSelectMultiple | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 5 | ElementGetComputedStyle | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 6 | ElementGetProperty | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 7 | ElementGetRect | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 8 | ElementGetScreenshot | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 9 | ElementSubmit | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 10 | ElementUploadFile | ✅ Есть (строка 68) | ✅ Есть (строка 78) | ✅ OK |
| 11 | ElementFind | ✅ Есть (строка 88) | ✅ Есть (строка 98) | ✅ OK |
| 12 | ElementWaitAndCheck | ✅ Есть (строка 88) | ✅ Есть (строка 98) | ✅ OK |
| 13 | ElementDragDrop | ⚠️ См. раздел 2 | ⚠️ См. раздел 2 | ⚠️ Специальный случай |

---

### 2. ElementDragDrop (Специальный случай)

ElementDragDrop должен иметь **два набора** свойств локаторов:

**Исходный элемент (Source):**
- `Prop_SourceLocatorType` (ElementLocatorType)
- `Prop_SourceLocatorValue` (string)

**Целевой элемент (Target):**
- `Prop_TargetLocatorType` (ElementLocatorType)
- `Prop_TargetLocatorValue` (string)

| Свойство | Наличие | Строка | Статус |
|----------|---------|--------|--------|
| Prop_SourceLocatorType | ✅ Есть | 68 | ✅ OK |
| Prop_SourceLocatorValue | ✅ Есть | 78 | ✅ OK |
| Prop_TargetLocatorType | ✅ Есть | 108 | ✅ OK |
| Prop_TargetLocatorValue | ✅ Есть | 118 | ✅ OK |

**Результат:** ✅ Все необходимые свойства присутствуют

---

## Итоговая сводка

### ✅ Все проверки пройдены успешно

- **13 Locator-Based активностей:** Все имеют `Prop_LocatorType` и `Prop_LocatorValue`
- **ElementDragDrop:** Имеет все 4 необходимых свойства для двух локаторов

### 📋 Список активностей с отсутствующими свойствами

**Пусто** — все активности имеют необходимые свойства.

---

## Детали реализации свойств

### Типичная реализация Prop_LocatorType

```csharp
private ElementLocatorType _propLocatorType;

[LTools.Common.Model.Serialization.StoringProperty]
[System.ComponentModel.Category(ActivityStrings.Category_Locator),
 System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorType)]
public ElementLocatorType Prop_LocatorType
{
    get => _propLocatorType;
    set { _propLocatorType = value; InvokePropertyChanged(this, "Prop_LocatorType"); }
}
```

### Типичная реализация Prop_LocatorValue

```csharp
private string _propLocatorValue;

[LTools.Common.Model.Serialization.StoringProperty]
[LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
[System.ComponentModel.Category(ActivityStrings.Category_Locator),
 System.ComponentModel.DisplayName(ActivityStrings.Field_LocatorValue)]
public string Prop_LocatorValue
{
    get => _propLocatorValue;
    set { _propLocatorValue = value; InvokePropertyChanged(this, "Prop_LocatorValue"); }
}
```

### Специальная реализация для ElementDragDrop

ElementDragDrop использует префиксы `Source` и `Target` для различения двух наборов локаторов:

- `Prop_SourceLocatorType` / `Prop_SourceLocatorValue` — для исходного элемента
- `Prop_TargetLocatorType` / `Prop_TargetLocatorValue` — для целевого элемента

---

## Рекомендации для следующих задач

1. ✅ Все XAML файлы могут быть обновлены без изменения Back.cs файлов
2. ✅ Привязки данных в XAML будут корректными (свойства существуют)
3. ✅ ElementDragDrop требует специальной обработки с двумя наборами элементов управления
4. ✅ Можно переходить к задачам группы 2 (обновление XAML файлов)

---

## Связанные требования

- **Requirements 2.1:** Locator_Based_Activity определена как активность с Prop_LocatorType и Prop_LocatorValue ✅
- **Requirements 2.3:** Активности с локаторами используют Extended_Interface ✅
- **Requirements 3.3, 3.4:** ElementDragDrop имеет Prop_SourceLocatorType и Prop_SourceLocatorValue ✅
- **Requirements 3.6, 3.7:** ElementDragDrop имеет Prop_TargetLocatorType и Prop_TargetLocatorValue ✅
- **Requirements 6.5:** Привязки данных будут соответствовать существующим свойствам ✅

---

**Заключение:** Все Back.cs файлы содержат необходимые свойства для унификации XAML интерфейсов. Можно приступать к обновлению XAML файлов.
