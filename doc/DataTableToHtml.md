# DataTable в HTML

Активность для конвертации DataTable в красиво оформленную HTML таблицу с гибкими настройками стилизации.

## Назначение

Компонент преобразует DataTable в HTML-код таблицы с поддержкой:
- 4 предустановленных темы оформления + пользовательская тема
- Нумерации строк
- Обработки null-значений
- Ограничения количества выводимых строк
- Двух режимов вывода: только таблица или полный HTML-документ

## Свойства

### Основные

| Свойство | Тип | Описание |
|----------|-----|----------|
| **Таблица данных** | DataTable | Исходная таблица данных для конвертации. Обязательное поле. |
| **Режим вывода HTML** | HtmlOutputMode | Формат выходного HTML. Варианты: `TableOnly` (только таблица), `FullDocument` (полный HTML-документ). По умолчанию: `TableOnly`. |
| **Нумерация строк** | Boolean | Добавить колонку с порядковыми номерами строк. По умолчанию: `false`. |
| **Текст для null** | String | Текст для отображения null-значений в ячейках. По умолчанию: пустая строка. |
| **Макс. строк** | Int32 | Ограничение количества выводимых строк. `0` = без ограничений. По умолчанию: `0`. |

### Стиль

| Свойство | Тип | Описание |
|----------|-----|----------|
| **Тема оформления** | HtmlTableTheme | Предустановленная тема. Варианты: `Light`, `Dark`, `Blue`, `Green`, `Custom`. По умолчанию: `Light`. |
| **Цвет заголовка** | String | Цвет фона заголовка в формате HEX (например, `#1565C0`). Используется при теме `Custom`. |
| **Цвет строк** | String | Цвет фона нечётных строк в формате HEX. Используется при теме `Custom`. |
| **Цвет чётных строк** | String | Цвет фона чётных строк в формате HEX. Используется при теме `Custom`. |
| **Цвет границ** | String | Цвет границ таблицы в формате HEX. Используется при теме `Custom`. |

### Выходные данные

| Свойство | Тип | Описание |
|----------|-----|----------|
| **HTML-код** | String | Сформированный HTML-код таблицы или документа. |
| **Количество строк** | Int32 | Количество строк в исходной таблице. |
| **Количество столбцов** | Int32 | Количество столбцов в исходной таблице. |

## Темы оформления

### Light (Светлая)
Классическая светлая тема с белым фоном и серыми границами.
- Фон заголовка: `#F5F5F5`
- Текст заголовка: `#333333`
- Фон строк: `#FFFFFF` / `#F9F9F9` (чётные)
- Границы: `#DDDDDD`

### Dark (Тёмная)
Тёмная тема для работы в условиях низкой освещённости.
- Фон заголовка: `#2D2D2D`
- Текст заголовка: `#FFFFFF`
- Фон строк: `#1E1E1E` / `#2A2A2A` (чётные)
- Границы: `#404040`

### Blue (Синяя)
Профессиональная синяя тема.
- Фон заголовка: `#1565C0`
- Текст заголовка: `#FFFFFF`
- Фон строк: `#FFFFFF` / `#E3F2FD` (чётные)
- Границы: `#BBDEFB`

### Green (Зелёная)
Свежая зелёная тема.
- Фон заголовка: `#2E7D32`
- Текст заголовка: `#FFFFFF`
- Фон строк: `#FFFFFF` / `#E8F5E9` (чётные)
- Границы: `#C8E6C9`

### Custom (Пользовательская)
Позволяет задать собственные цвета для всех элементов таблицы.

## Примеры использования

### Пример 1: Базовая конвертация

```
Входные данные:
  Prop_DataTable = myDataTable
  Prop_Theme = Light
  Prop_OutputMode = TableOnly

Результат:
  HTML-код таблицы с белым фоном и серыми границами
```

### Пример 2: Полный HTML-документ с нумерацией

```
Входные данные:
  Prop_DataTable = myDataTable
  Prop_Theme = Blue
  Prop_OutputMode = FullDocument
  Prop_ShowRowNumbers = true

Результат:
  Полный HTML-документ с синей темой и колонкой с номерами строк
```

### Пример 3: Пользовательская тема

```
Входные данные:
  Prop_DataTable = myDataTable
  Prop_Theme = Custom
  Prop_HeaderColor = "#8B0000"
  Prop_RowColor = "#FFFFFF"
  Prop_AltRowColor = "#FFE4E1"
  Prop_BorderColor = "#CD5C5C"

Результат:
  Таблица с красно-розовой цветовой схемой
```

### Пример 4: Ограничение строк и обработка null

```
Входные данные:
  Prop_DataTable = largeDataTable
  Prop_MaxRows = 100
  Prop_NullDisplay = "N/A"

Результат:
  Таблица с максимум 100 строками, null-значения отображаются как "N/A"
```

## Пример выходного HTML

### Режим TableOnly

```html
<table style="border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; border: 1px solid #BBDEFB;">
  <thead>
    <tr style="background-color: #1565C0; color: #FFFFFF;">
      <th style="padding: 10px; border: 1px solid #BBDEFB; text-align: left; font-weight: bold;">Имя</th>
      <th style="padding: 10px; border: 1px solid #BBDEFB; text-align: left; font-weight: bold;">Возраст</th>
    </tr>
  </thead>
  <tbody>
    <tr style="background-color: #FFFFFF;">
      <td style="padding: 8px; border: 1px solid #BBDEFB;">Иван</td>
      <td style="padding: 8px; border: 1px solid #BBDEFB;">25</td>
    </tr>
    <tr style="background-color: #E3F2FD;">
      <td style="padding: 8px; border: 1px solid #BBDEFB;">Мария</td>
      <td style="padding: 8px; border: 1px solid #BBDEFB;">30</td>
    </tr>
  </tbody>
</table>
```

### Режим FullDocument

```html
<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>DataTable Export</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; background-color: #FAFAFA; }
    table { border-collapse: collapse; width: 100%; background-color: #FFFFFF; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    th { background-color: #1565C0; color: #FFFFFF; padding: 12px; text-align: left; border: 1px solid #BBDEFB; }
    td { padding: 10px; border: 1px solid #BBDEFB; }
    tbody tr:nth-child(odd) { background-color: #FFFFFF; }
    tbody tr:nth-child(even) { background-color: #E3F2FD; }
    tbody tr:hover { background-color: #F5F5F5; }
  </style>
</head>
<body>
  <table>
    <!-- содержимое таблицы -->
  </table>
</body>
</html>
```

## Особенности

- **Экранирование HTML**: Все специальные символы в данных автоматически экранируются (`<`, `>`, `&`, `"`, `'`)
- **Чередование строк**: Чётные строки имеют альтернативный цвет фона для лучшей читаемости
- **Адаптивность**: В режиме FullDocument таблица занимает 100% ширины контейнера
- **Шрифт**: Используется Arial как безопасный шрифт для всех платформ

## Ошибки

| Ошибка | Причина | Решение |
|--------|---------|---------|
| "Исходная таблица данных не указана" | Prop_DataTable = null | Передайте DataTable в свойство Prop_DataTable |
| "Максимальное количество строк не может быть отрицательным" | Prop_MaxRows < 0 | Установите Prop_MaxRows >= 0 |
| "При выборе пользовательской темы необходимо указать хотя бы один цвет" | Prop_Theme = Custom, но все цвета пустые | Укажите хотя бы один цвет при использовании темы Custom |
