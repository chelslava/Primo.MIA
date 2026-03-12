# Design Document: Browser Interface Modernization

## Overview

Данный дизайн описывает модернизацию XAML интерфейсов активностей Browser модуля в Primo Platform для приведения их к единому стандарту Compact Interface. Цель - устранить несоответствия в высоте дизайна, отступах и структуре макета, обеспечив визуальную консистентность всех Browser активностей.

### Проблема

В настоящее время Browser активности имеют следующие проблемы:
- Избыточное значение `d:DesignHeight="450"` в UserControl, создающее лишнее пространство в дизайнере
- Непоследовательные значения Margin и VerticalAlignment между активностями
- Лишние пробелы и переносы строк в Grid.ColumnDefinitions
- Отсутствие единого шаблона для создания новых Browser активностей

### Решение

Применить стандарт Compact Interface ко всем Browser активностям:
- **Удалить** избыточный атрибут `d:DesignHeight="450"` из всех Browser активностей
- **Стандартизировать** структуру макета с использованием Standard_Layout (Grid с 2 колонками)
- **Унифицировать** значения Margin и VerticalAlignment (Image Margin="5", StackPanel Margin="5", VerticalAlignment="Center")
- **Сохранить** специальную Container_Layout структуру для BrowserOpen
- **Очистить** форматирование Grid.ColumnDefinitions от лишних пробелов
- **Создать** шаблон CompactInterfaceTemplate.xaml для новых активностей

### Преимущества

- Единообразный визуальный интерфейс для всех Browser активностей
- Корректное отображение в дизайнере без лишнего пространства
- Упрощенная поддержка и модификация кода
- Быстрое создание новых активностей с использованием шаблона
- Соответствие стандарту Compact Interface из спецификации xaml-interface-unification

## Architecture

### Компоненты системы

```mermaid
graph TD
    A[Browser Activities] --> B[Standard Browser Activities]
    A --> C[Container Activity]
    B --> D[Standard_Layout]
    C --> E[Container_Layout]
    D --> F[Compact Interface]
    E --> G[Compact Interface + Container]
    
    F --> H[Grid: 2 columns]
    F --> I[Image: 32x32, Margin=5]
    F --> J[StackPanel: Margin=5, Center]
    F --> K[TextBlock: Bold/12 + Gray/10]
    
    G --> L[Grid.RowDefinitions]
    G --> M[Header Row: Standard_Layout]
    G --> N[Container Row: WFContainerBase]
```

### Классификация активностей

#### 1. Standard Browser Activities (Standard_Layout)
Активности работающие с браузером в целом, использующие стандартный Compact Interface:
- AlertHandle
- BrowserClose
- BrowserNavigate
- BrowserGetInfo
- BrowserScreenshot
- BrowserExecuteJavaScript
- BrowserWaitFor
- BrowserSwitchTo
- BrowserTabManage
- BrowserWindowManage
- BrowserManageCookies
- BrowserStorageManage
- BrowserGetLogs

#### 2. Container Activity (Container_Layout)
Активность-контейнер с вложенными активностями:
- BrowserOpen (специальная структура с Grid.RowDefinitions)

### Стандарты интерфейсов

#### Compact Interface Standard
Определен в спецификации xaml-interface-unification:
- Grid без Background атрибута
- Двухколоночная структура (Auto + *)
- Image 32x32 с Margin="5" и VerticalAlignment="Center"
- StackPanel с VerticalAlignment="Center" и Margin="5"
- TextBlock заголовок: FontWeight="Bold", FontSize="12"
- TextBlock параметры: FontSize="10", Foreground="Gray"

#### Extended Interface Standard
Определен в спецификации xaml-interface-unification (не применяется к Browser активностям):
- Grid с Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}"
- UserControl с d:DesignHeight="80" и Margin="0,0,0,5"
- Элементы управления (ComboBox, TextBox) для настройки локаторов

## Components and Interfaces

### Standard_Layout Structure (Compact Interface)

Стандартная структура для обычных Browser активностей:

```xaml
<UserControl x:Class="Primo.MIA.[ActivityName]"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:Primo.MIA"
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
               Source="pack://application:,,,/Primo.MIA;component/images/browser.png"/>

        <StackPanel Grid.Column="1" 
                    VerticalAlignment="Center" 
                    Margin="5">
            <TextBlock Text="[Название активности]" 
                       FontWeight="Bold" 
                       FontSize="12"/>
            <TextBlock FontSize="10" 
                       Foreground="Gray">
                <Run Text="[Параметр]: "/>
                <Run Text="{Binding Path=Prop_[PropertyName], Mode=OneWay}"/>
            </TextBlock>
        </StackPanel>
    </Grid>
</UserControl>
```

#### Ключевые характеристики Standard_Layout:
- **UserControl**: БЕЗ атрибута d:DesignHeight
- **Grid**: БЕЗ атрибута Background
- **Grid.ColumnDefinitions**: Одна строка без лишних пробелов
- **Image**: Width="32", Height="32", Margin="5", VerticalAlignment="Center"
- **StackPanel**: VerticalAlignment="Center", Margin="5"
- **TextBlock (заголовок)**: FontWeight="Bold", FontSize="12"
- **TextBlock (параметры)**: FontSize="10", Foreground="Gray"

### Container_Layout Structure (BrowserOpen)

Специальная структура для активности-контейнера BrowserOpen:

```xaml
<UserControl x:Class="Primo.MIA.BrowserOpen"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:local="clr-namespace:Primo.MIA"
             xmlns:WFItems="clr-namespace:LTools.Common.WFItems;assembly=LTools.Common"
             xmlns:ui="clr-namespace:LTools.Common.UIElements;assembly=LTools.Common"
             mc:Ignorable="d">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Заголовок с Standard_Layout -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Image Grid.Column="0"
                   Width="32"
                   Height="32"
                   Margin="5"
                   VerticalAlignment="Center"
                   Source="pack://application:,,,/Primo.MIA;component/images/browser.png"/>

            <StackPanel Grid.Column="1"
                        VerticalAlignment="Center"
                        Margin="5">
                <TextBlock Text="Открыть браузер"
                           FontWeight="Bold"
                           FontSize="12"/>
                <TextBlock FontSize="10"
                           Foreground="Gray">
                    <Run Text="Тип: "/>
                    <Run Text="{Binding Path=Prop_BrowserType, Mode=OneWay}"/>
                    <Run Text=" | Headless: "/>
                    <Run Text="{Binding Path=Prop_Headless, Mode=OneWay}"/>
                </TextBlock>
            </StackPanel>
        </Grid>

        <!-- Контейнер вложенных активностей -->
        <Grid Grid.Row="1" Margin="5">
            <Border BorderThickness="1"
                    BorderBrush="#CCCCCC"
                    CornerRadius="0"
                    Padding="{x:Static ui:WFElementBase.ContainerPadding}">
                <WFItems:WFContainerBase x:Name="cntBrowser"/>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

#### Ключевые характеристики Container_Layout:
- **UserControl**: БЕЗ атрибута d:DesignHeight
- **Grid**: С Grid.RowDefinitions (Auto + *)
- **Grid.Row="0"**: Заголовок с Standard_Layout структурой
- **Grid.Row="1"**: Контейнер с WFContainerBase для вложенных активностей
- **Заголовок**: Использует те же стандарты Margin и VerticalAlignment

### Изменения по активностям

#### Изменения для Standard Browser Activities

Для всех стандартных Browser активностей применяются следующие изменения:

1. **Удаление d:DesignHeight**:
   ```xaml
   <!-- ДО -->
   <UserControl ... d:DesignHeight="450" d:DesignWidth="800">
   
   <!-- ПОСЛЕ -->
   <UserControl ... mc:Ignorable="d">
   ```

2. **Очистка Grid.ColumnDefinitions**:
   ```xaml
   <!-- ДО (с лишними пробелами) -->
   <Grid.ColumnDefinitions>
   
            <ColumnDefinition Width="Auto"/>
            
            <ColumnDefinition Width="*"/>
            
        </Grid.ColumnDefinitions>
   
   <!-- ПОСЛЕ -->
   <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
   ```

3. **Стандартизация атрибутов**:
   - Image: Margin="5", VerticalAlignment="Center"
   - StackPanel: Margin="5", VerticalAlignment="Center"
   - TextBlock заголовок: FontWeight="Bold", FontSize="12"
   - TextBlock параметры: FontSize="10", Foreground="Gray"

#### Изменения для BrowserOpen

Для BrowserOpen применяются те же изменения, но с сохранением Container_Layout:

1. **Удаление d:DesignHeight** из UserControl
2. **Стандартизация заголовка** (Grid.Row="0") с теми же Margin и VerticalAlignment
3. **Сохранение контейнера** (Grid.Row="1") без изменений

## Data Models

### XAML File Mapping

| Activity | Current State | Target State | Changes |
|----------|--------------|--------------|---------|
| AlertHandle | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserClose | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserNavigate | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserGetInfo | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserScreenshot | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserExecuteJavaScript | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserWaitFor | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserSwitchTo | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserTabManage | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserWindowManage | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserManageCookies | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserStorageManage | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserGetLogs | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight |
| BrowserOpen | d:DesignHeight="450" | No DesignHeight | Remove DesignHeight + Container_Layout |

### Attribute Standards

#### Standard_Layout Attributes

| Element | Attribute | Value | Purpose |
|---------|-----------|-------|---------|
| UserControl | d:DesignHeight | (removed) | Устранение лишнего пространства |
| Grid | Background | (not set) | Compact Interface стандарт |
| Grid.ColumnDefinitions | - | Auto, * | Двухколоночная структура |
| Image | Width | 32 | Стандартный размер иконки |
| Image | Height | 32 | Стандартный размер иконки |
| Image | Margin | 5 | Консистентный отступ |
| Image | VerticalAlignment | Center | Вертикальное выравнивание |
| StackPanel | Margin | 5 | Консистентный отступ |
| StackPanel | VerticalAlignment | Center | Вертикальное выравнивание |
| TextBlock (title) | FontWeight | Bold | Выделение заголовка |
| TextBlock (title) | FontSize | 12 | Стандартный размер заголовка |
| TextBlock (params) | FontSize | 10 | Стандартный размер параметров |
| TextBlock (params) | Foreground | Gray | Визуальное отличие параметров |

#### Container_Layout Attributes

| Element | Attribute | Value | Purpose |
|---------|-----------|-------|---------|
| Grid.RowDefinitions | - | Auto, * | Заголовок + контейнер |
| Grid.Row="0" | - | Standard_Layout | Заголовок активности |
| Grid.Row="1" | - | WFContainerBase | Контейнер вложенных активностей |

### Template Structure

Шаблон CompactInterfaceTemplate.xaml будет содержать:

```xaml
<UserControl x:Class="Primo.MIA.[ACTIVITY_CLASS_NAME]"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:Primo.MIA"
             mc:Ignorable="d">
    <!-- 
        COMPACT INTERFACE TEMPLATE для Browser активностей
        
        ИНСТРУКЦИИ ПО ИСПОЛЬЗОВАНИЮ:
        1. Замените [ACTIVITY_CLASS_NAME] на имя класса активности (например, BrowserNavigate)
        2. Замените [ICON_PATH] на путь к иконке (обычно: pack://application:,,,/Primo.MIA;component/images/browser.png)
        3. Замените [ACTIVITY_TITLE] на название активности (например, "Навигация браузера")
        4. Замените [PARAMETER_LABEL] на метку параметра (например, "URL")
        5. Замените [PROPERTY_NAME] на имя свойства для привязки (например, Prop_Url)
        
        ПРИМЕРЫ:
        
        Простой заголовок без параметров:
        <TextBlock Text="Закрыть браузер" FontWeight="Bold" FontSize="12"/>
        
        Заголовок с одним параметром:
        <TextBlock FontSize="10" Foreground="Gray">
            <Run Text="URL: "/>
            <Run Text="{Binding Path=Prop_Url, Mode=OneWay}"/>
        </TextBlock>
        
        Заголовок с несколькими параметрами:
        <TextBlock FontSize="10" Foreground="Gray">
            <Run Text="Тип: "/>
            <Run Text="{Binding Path=Prop_BrowserType, Mode=OneWay}"/>
            <Run Text=" | Headless: "/>
            <Run Text="{Binding Path=Prop_Headless, Mode=OneWay}"/>
        </TextBlock>
    -->
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
               Source="[ICON_PATH]"/>

        <StackPanel Grid.Column="1" 
                    VerticalAlignment="Center" 
                    Margin="5">
            <TextBlock Text="[ACTIVITY_TITLE]" 
                       FontWeight="Bold" 
                       FontSize="12"/>
            <TextBlock FontSize="10" 
                       Foreground="Gray">
                <Run Text="[PARAMETER_LABEL]: "/>
                <Run Text="{Binding Path=Prop_[PROPERTY_NAME], Mode=OneWay}"/>
            </TextBlock>
        </StackPanel>
    </Grid>
</UserControl>
```

Плейсхолдеры для замены:
- `[ACTIVITY_CLASS_NAME]` - имя класса активности
- `[ICON_PATH]` - путь к иконке активности
- `[ACTIVITY_TITLE]` - название активности на русском
- `[PARAMETER_LABEL]` - метка параметра
- `[PROPERTY_NAME]` - имя свойства для привязки данных

