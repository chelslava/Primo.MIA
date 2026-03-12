# Руководство по XAML интерфейсам активностей Browser модуля

## Введение

Данное руководство описывает правила выбора и использования типов XAML интерфейсов для активностей Browser модуля в Primo Platform. Цель унификации - обеспечить консистентный пользовательский опыт и упростить настройку активностей.

## Типы интерфейсов

### Extended Interface (Расширенный интерфейс)

Расширенный интерфейс предоставляет элементы управления для быстрой настройки локаторов элементов непосредственно в визуальном дизайнере.

**Визуальные характеристики:**
- Фоновый цвет (серый системный цвет ScrollBar)
- ComboBox для выбора типа локатора (CSS, XPath, ID и т.д.)
- TextBox для ввода значения локатора
- Высота: 80 пикселей (120 для ElementDragDrop)
- Вертикальное выравнивание: Top

**Когда использовать:**
- Активность работает с элементами веб-страницы
- Активность использует свойства `Prop_LocatorType` и `Prop_LocatorValue` для поиска элементов
- Пользователю нужно часто изменять локаторы

### Compact Interface (Компактный интерфейс)

Компактный интерфейс показывает только иконку и текстовое описание активности с основным параметром.

**Визуальные характеристики:**
- Без фонового цвета
- Только текстовое отображение параметров
- Компактный размер
- Вертикальное выравнивание: Center

**Когда использовать:**
- Активность работает с браузером в целом (не с конкретными элементами)
- Активность использует `Prop_ElementId` (ссылка на ранее найденный элемент)
- Параметры активности редко изменяются

## Правила выбора типа интерфейса

### Для новых активностей

При создании новой активности Browser модуля используйте следующую логику:

```
ЕСЛИ активность имеет Prop_LocatorType И Prop_LocatorValue
    → Использовать Extended Interface

ИНАЧЕ ЕСЛИ активность имеет Prop_ElementId (без локаторов)
    → Использовать Compact Interface

ИНАЧЕ ЕСЛИ активность работает с браузером в целом
    → Использовать Compact Interface

ИНАЧЕ ЕСЛИ активность имеет несколько наборов локаторов
    → Использовать Extended Interface с несколькими наборами элементов управления
```

### Специальные случаи

**ElementDragDrop**: Использует расширенный интерфейс с двумя наборами локаторов (исходный и целевой элемент).

## Примеры интерфейсов

### Пример Extended Interface

```xaml
<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:local="clr-namespace:Primo.Activities.Browser"
             xmlns:system="clr-namespace:System;assembly=mscorlib"
             mc:Ignorable="d"
             d:DesignHeight="80"
             Margin="0,0,0,5">
    
    <UserControl.Resources>
        <ObjectDataProvider x:Key="LocatorTypeValues" 
                            MethodName="GetValues" 
                            ObjectType="{x:Type system:Enum}">
            <ObjectDataProvider.MethodParameters>
                <x:Type TypeName="local:ElementLocatorType"/>
            </ObjectDataProvider.MethodParameters>
        </ObjectDataProvider>
    </UserControl.Resources>
    
    <Grid Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <Image Grid.Column="0" 
               Width="32" 
               Height="32" 
               Margin="5" 
               VerticalAlignment="Center" 
               Source="pack://application:,,,/Primo.Activities.Browser;component/Resources/element_find.png"/>
        
        <StackPanel Grid.Column="1" 
                    VerticalAlignment="Top" 
                    Margin="5,0,5,5">
            <TextBlock FontWeight="Bold" FontSize="12">
                Поиск элемента
            </TextBlock>
            
            <ComboBox ItemsSource="{Binding Source={StaticResource LocatorTypeValues}}"
                      SelectedItem="{Binding Prop_LocatorType}"
                      Margin="0,2,6,2" 
                      Height="22"/>
            
            <TextBox Text="{Binding Prop_LocatorValue}"
                     Margin="0,2,6,0" 
                     Height="22"/>
        </StackPanel>
    </Grid>
</UserControl>
```

### Пример Compact Interface

```xaml
<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d">
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <Image Grid.Column="0" 
               Width="32" 
               Height="32" 
               Margin="5"
               VerticalAlignment="Center" 
               Source="pack://application:,,,/Primo.Activities.Browser;component/Resources/alert_handle.png"/>
        
        <StackPanel Grid.Column="1" 
                    VerticalAlignment="Center" 
                    Margin="5">
            <TextBlock Text="Обработка диалогового окна" 
                       FontWeight="Bold" 
                       FontSize="12"/>
            <TextBlock FontSize="10" Foreground="Gray">
                <Run Text="Действие: "/>
                <Run Text="{Binding Path=Prop_Action, Mode=OneWay}"/>
            </TextBlock>
        </StackPanel>
    </Grid>
</UserControl>
```

## Классификация активностей

### Locator-Based Activities (Extended Interface)

Активности, использующие локаторы для поиска элементов на веб-странице:

| Активность | Описание | Свойства локатора |
|-----------|----------|-------------------|
| ElementExists | Проверка существования элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementIsVisible | Проверка видимости элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementSelect | Выбор опции в элементе | Prop_LocatorType, Prop_LocatorValue |
| ElementSelectMultiple | Множественный выбор опций | Prop_LocatorType, Prop_LocatorValue |
| ElementGetComputedStyle | Получение вычисленного стиля | Prop_LocatorType, Prop_LocatorValue |
| ElementGetProperty | Получение свойства элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementGetRect | Получение размеров элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementGetScreenshot | Скриншот элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementSubmit | Отправка формы | Prop_LocatorType, Prop_LocatorValue |
| ElementUploadFile | Загрузка файла | Prop_LocatorType, Prop_LocatorValue |
| ElementFind | Поиск элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementWaitAndCheck | Ожидание и проверка элемента | Prop_LocatorType, Prop_LocatorValue |
| ElementClick | Клик по элементу | Prop_LocatorType, Prop_LocatorValue |

### ElementId-Based Activities (Compact Interface)

Активности, работающие с ранее найденными элементами по идентификатору:

| Активность | Описание | Свойства |
|-----------|----------|----------|
| ElementHover | Наведение на элемент | Prop_ElementId |
| ElementInput | Ввод текста в элемент | Prop_ElementId |
| ElementScrollTo | Прокрутка к элементу | Prop_ElementId |

### Browser Activities (Compact Interface)

Активности, работающие с браузером в целом:

| Активность | Описание | Основные свойства |
|-----------|----------|-------------------|
| BrowserOpen | Открытие браузера | Prop_BrowserType, Prop_Url |
| BrowserClose | Закрытие браузера | Prop_BrowserId |
| BrowserNavigate | Навигация по URL | Prop_Url |
| BrowserGetInfo | Получение информации о браузере | Prop_InfoType |
| BrowserScreenshot | Скриншот страницы | Prop_FilePath |
| BrowserExecuteJavaScript | Выполнение JavaScript | Prop_Script |
| BrowserWaitFor | Ожидание условия | Prop_Condition |
| BrowserSwitchTo | Переключение контекста | Prop_Target |
| BrowserTabManage | Управление вкладками | Prop_Action |
| BrowserWindowManage | Управление окнами | Prop_Action |
| BrowserManageCookies | Управление cookies | Prop_Action |
| BrowserStorageManage | Управление хранилищем | Prop_Action |
| BrowserGetLogs | Получение логов браузера | Prop_LogType |
| AlertHandle | Обработка диалоговых окон | Prop_Action |

### Special Cases

| Активность | Описание | Интерфейс | Особенности |
|-----------|----------|-----------|-------------|
| ElementDragDrop | Перетаскивание элемента | Extended (специальный) | Два набора локаторов: Source (Prop_SourceLocatorType, Prop_SourceLocatorValue) и Target (Prop_TargetLocatorType, Prop_TargetLocatorValue) |

## Визуальные различия

### Extended Interface
- ✅ Фоновый цвет (серый)
- ✅ ComboBox для типа локатора
- ✅ TextBox для значения локатора
- ✅ Высота: 80px (или 120px для ElementDragDrop)
- ✅ VerticalAlignment="Top" для StackPanel
- ✅ Margin="0,0,0,5" для UserControl

### Compact Interface
- ❌ Без фонового цвета
- ❌ Без элементов управления для локаторов
- ✅ Только текстовое отображение параметров
- ✅ Компактный размер
- ✅ VerticalAlignment="Center" для StackPanel
- ✅ Без Margin для UserControl

## Технические детали

### Обязательные элементы Extended Interface

1. **ObjectDataProvider** для ElementLocatorType enum
2. **Grid** с двумя колонками (Auto, *)
3. **Image** 32x32 пикселя в Grid.Column="0"
4. **StackPanel** в Grid.Column="1"
5. **ComboBox** с привязкой к Prop_LocatorType
6. **TextBox** с привязкой к Prop_LocatorValue
7. **TextBlock** заголовок с FontWeight="Bold" и FontSize="12"

### Обязательные атрибуты Extended Interface

```
UserControl:
  - d:DesignHeight="80"
  - Margin="0,0,0,5"

Grid:
  - Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}"

Image:
  - Width="32"
  - Height="32"
  - Margin="5"
  - VerticalAlignment="Center"

StackPanel:
  - VerticalAlignment="Top"
  - Margin="5,0,5,5"

ComboBox:
  - Height="22"
  - Margin="0,2,6,2"

TextBox:
  - Height="22"
  - Margin="0,2,6,0"
```

### Обязательные элементы Compact Interface

1. **Grid** с двумя колонками (Auto, *)
2. **Image** 32x32 пикселя в Grid.Column="0"
3. **StackPanel** в Grid.Column="1"
4. **TextBlock** заголовок с FontWeight="Bold" и FontSize="12"
5. **TextBlock** параметр с FontSize="10" и Foreground="Gray"

### Обязательные атрибуты Compact Interface

```
Grid:
  - Без Background

Image:
  - Width="32"
  - Height="32"
  - Margin="5"
  - VerticalAlignment="Center"

StackPanel:
  - VerticalAlignment="Center"
  - Margin="5"

TextBlock (заголовок):
  - FontWeight="Bold"
  - FontSize="12"

TextBlock (параметр):
  - FontSize="10"
  - Foreground="Gray"
```

## Рекомендации по разработке

### При создании новой активности

1. Определите тип активности (Element, Browser)
2. Определите какие свойства будут использоваться для поиска элементов
3. Выберите тип интерфейса согласно правилам выше
4. Используйте соответствующий шаблон из `.kiro/specs/xaml-interface-unification/templates/`
5. Замените специфичные элементы (название, иконка, привязки)
6. Проверьте визуальное отображение в дизайнере

### При модификации существующей активности

1. Определите текущий тип интерфейса
2. Проверьте соответствие правилам выбора типа интерфейса
3. Если тип интерфейса нужно изменить, используйте соответствующий шаблон
4. Сохраните все существующие привязки данных
5. Проверьте что Back.cs файл содержит все необходимые свойства
6. Проверьте визуальное отображение в дизайнере

## Заключение

Следование данному руководству обеспечит консистентность пользовательского интерфейса активностей Browser модуля и упростит разработку и поддержку кода.

Для получения дополнительной информации см.:
- `.kiro/specs/xaml-interface-unification/requirements.md` - требования к унификации
- `.kiro/specs/xaml-interface-unification/design.md` - дизайн решения
- `.kiro/specs/xaml-interface-unification/CHANGES.md` - список измененных файлов
- `.kiro/specs/xaml-interface-unification/templates/` - шаблоны интерфейсов
