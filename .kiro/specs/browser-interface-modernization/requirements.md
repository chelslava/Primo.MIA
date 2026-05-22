# Requirements Document

## Introduction

Данный документ описывает требования к модернизации XAML интерфейсов активностей Browser модуля в Primo Platform. Цель - привести все Browser активности к единому стандарту Extended Interface, установленному в спецификации xaml-interface-unification, устранив несоответствия в высоте дизайна, отступах, фоновом цвете и структуре макета.

## Glossary

- **Browser_Activity**: Активность Browser модуля, работающая с браузером в целом (не с конкретными элементами страницы)
- **Extended_Interface**: Стандартный XAML интерфейс для Browser активностей с фоновым цветом, стандартной высотой и визуальным выделением
- **DesignHeight**: Атрибут d:DesignHeight в UserControl, определяющий высоту элемента в дизайнере
- **XAML_Interface**: Визуальный интерфейс активности в дизайнере Primo Platform
- **Container_Activity**: Активность-контейнер, содержащая вложенные активности (например, BrowserOpen)
- **Standard_Layout**: Стандартная структура макета с Grid, двумя колонками (Auto, *), Image и StackPanel
- **Container_Layout**: Специальная структура макета с Grid.RowDefinitions для активностей-контейнеров
- **Unified_Style**: Единообразный визуальный стиль с консистентными отступами, выравниванием, размерами и цветами

## Requirements

### Requirement 1: Установка стандартной высоты дизайна и отступов

**User Story:** Как разработчик, я хочу, чтобы все Browser активности имели единую высоту дизайна d:DesignHeight="80" и стандартные отступы Margin="0,0,0,5", чтобы интерфейс отображался консистентно в дизайнере.

#### Acceptance Criteria

1. WHEN Browser_Activity использует Extended_Interface, THE d:DesignHeight атрибут SHALL быть установлен в "80"
2. WHEN Browser_Activity является Container_Activity, THE d:DesignHeight атрибут SHALL быть установлен в "80"
3. THE список Browser_Activity SHALL включать: AlertHandle, BrowserClose, BrowserNavigate, BrowserGetInfo, BrowserScreenshot, BrowserExecuteJavaScript, BrowserWaitFor, BrowserSwitchTo, BrowserTabManage, BrowserWindowManage, BrowserManageCookies, BrowserStorageManage, BrowserGetLogs
4. THE BrowserOpen активность SHALL рассматриваться как Container_Activity
5. FOR ALL Browser_Activity XAML файлов, UserControl SHALL иметь Margin="0,0,0,5"

### Requirement 2: Добавление фонового цвета Grid

**User Story:** Как пользователь Primo Platform, я хочу видеть визуально выделенные Browser активности с фоновым цветом, чтобы легко отличать их от других элементов в дизайнере.

#### Acceptance Criteria

1. WHEN Browser_Activity не является Container_Activity, THE Grid SHALL иметь Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}"
2. WHEN Browser_Activity является Container_Activity, THE Grid.Row="0" (заголовок) SHALL иметь Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}"
3. THE фоновый цвет SHALL быть консистентным между всеми Browser активностями
4. THE Grid.Row="1" (контейнер) в BrowserOpen SHALL НЕ иметь Background атрибут

### Requirement 3: Стандартизация структуры макета для обычных Browser активностей

**User Story:** Как пользователь Primo Platform, я хочу видеть консистентный визуальный интерфейс для всех Browser активностей, чтобы легко ориентироваться в дизайнере.

#### Acceptance Criteria

1. WHEN Browser_Activity не является Container_Activity, THE XAML_Interface SHALL использовать Standard_Layout
2. THE Standard_Layout SHALL содержать Grid с Grid.ColumnDefinitions
3. THE Grid.ColumnDefinitions SHALL содержать ColumnDefinition с Width="Auto" и ColumnDefinition с Width="*"
4. THE Grid.ColumnDefinitions SHALL НЕ содержать лишних пробелов или переносов строк между определениями колонок
5. THE Image элемент SHALL находиться в Grid.Column="0" с атрибутами Width="32", Height="32", Margin="5", VerticalAlignment="Center"
6. THE StackPanel элемент SHALL находиться в Grid.Column="1" с атрибутами VerticalAlignment="Top", Margin="5,0,5,5"
7. THE StackPanel SHALL содержать TextBlock с FontWeight="Bold", FontSize="12" и Foreground="#FF0E0E0E" для заголовка
8. THE StackPanel SHALL содержать TextBlock с FontSize="10" и Foreground="Gray" для отображения параметров

### Requirement 4: Консистентность отступов и выравнивания

**User Story:** Как разработчик, я хочу, чтобы все Browser активности использовали единообразные значения Margin, чтобы обеспечить визуальную консистентность.

#### Acceptance Criteria

1. FOR ALL Browser_Activity с Standard_Layout, THE Image Margin SHALL быть "5"
2. FOR ALL Browser_Activity с Standard_Layout, THE StackPanel Margin SHALL быть "5,0,5,5"
3. FOR ALL Browser_Activity с Standard_Layout, THE StackPanel VerticalAlignment SHALL быть "Top"
4. FOR ALL Browser_Activity с Standard_Layout, THE Image VerticalAlignment SHALL быть "Center"
5. THE Margin значения SHALL быть консистентными между всеми Browser активностями

### Requirement 5: Сохранение специальной структуры BrowserOpen

**User Story:** Как пользователь Primo Platform, я хочу, чтобы BrowserOpen сохранил свою контейнерную структуру, но с улучшенной консистентностью, чтобы использовать вложенные активности.

#### Acceptance Criteria

1. THE BrowserOpen XAML_Interface SHALL использовать Container_Layout с Grid.RowDefinitions
2. THE BrowserOpen Grid.RowDefinitions SHALL содержать RowDefinition с Height="Auto" для заголовка
3. THE BrowserOpen Grid.RowDefinitions SHALL содержать RowDefinition с Height="*" для контейнера вложенных активностей
4. THE BrowserOpen заголовок (Grid.Row="0") SHALL использовать Standard_Layout структуру с Background
5. THE BrowserOpen контейнер (Grid.Row="1") SHALL содержать WFContainerBase элемент
6. THE BrowserOpen UserControl SHALL иметь d:DesignHeight="80" и Margin="0,0,0,5"
7. THE BrowserOpen заголовок SHALL иметь консистентные Margin значения с другими Browser активностями

### Requirement 6: Устранение лишних пробелов в Grid.ColumnDefinitions

**User Story:** Как разработчик, я хочу, чтобы XAML код был чистым и не содержал лишних пробелов, чтобы улучшить читаемость и поддерживаемость.

#### Acceptance Criteria

1. THE Grid.ColumnDefinitions элемент SHALL быть записан в одну строку или с минимальными переносами
2. THE Grid.ColumnDefinitions SHALL НЕ содержать лишних пустых строк между ColumnDefinition элементами
3. WHEN Grid.ColumnDefinitions содержит лишние пробелы, THE форматирование SHALL быть исправлено
4. THE форматирование SHALL быть консистентным между всеми Browser_Activity XAML файлами

### Requirement 7: Консистентность структуры TextBlock элементов

**User Story:** Как пользователь Primo Platform, я хочу видеть консистентное отображение заголовков и параметров активностей, чтобы быстро понимать назначение каждой активности.

#### Acceptance Criteria

1. FOR ALL Browser_Activity, THE заголовок TextBlock SHALL использовать FontWeight="Bold", FontSize="12" и Foreground="#FF0E0E0E"
2. FOR ALL Browser_Activity с отображением параметров, THE параметр TextBlock SHALL использовать FontSize="10" и Foreground="Gray"
3. THE параметр TextBlock SHALL содержать Run элементы с текстом метки и привязкой к свойству
4. THE формат отображения параметров SHALL быть консистентным: "<Run Text="Метка: "/><Run Text="{Binding Path=Prop_PropertyName, Mode=OneWay}"/>"
5. WHEN активность имеет несколько параметров, THE они SHALL быть разделены символом " | " в одном TextBlock

### Requirement 8: Сохранение функциональности без изменения кода

**User Story:** Как разработчик, я хочу, чтобы модернизация XAML интерфейсов не затрагивала функциональность активностей, чтобы избежать регрессии.

#### Acceptance Criteria

1. WHEN XAML файл изменен, THE соответствующий .xaml.cs файл SHALL оставаться без изменений
2. WHEN XAML файл изменен, THE соответствующий Back.cs файл SHALL оставаться без изменений
3. THE привязки данных (Binding) в XAML SHALL соответствовать существующим свойствам в Back.cs файлах
4. THE измененные XAML файлы SHALL компилироваться без ошибок
5. THE измененные XAML файлы SHALL корректно отображаться в дизайнере Primo Platform

### Requirement 9: Документирование изменений

**User Story:** Как разработчик, я хочу иметь документацию по изменениям XAML интерфейсов Browser активностей, чтобы понимать, какие файлы были изменены и какие улучшения были внесены.

#### Acceptance Criteria

1. THE документация SHALL содержать список всех измененных XAML файлов
2. THE документация SHALL содержать описание типа изменений для каждого файла
3. THE документация SHALL содержать сравнение "до" и "после" для ключевых атрибутов
4. THE документация SHALL содержать правила применения Extended Interface к новым Browser активностям
5. THE документация SHALL быть создана в формате Markdown
6. THE документация SHALL содержать статистику изменений (количество файлов, типы изменений)

### Requirement 10: Создание шаблона Extended Interface для Browser активностей

**User Story:** Как разработчик, я хочу иметь готовый шаблон Extended Interface для Browser активностей, чтобы быстро создавать новые активности с консистентным интерфейсом.

#### Acceptance Criteria

1. THE шаблон SHALL содержать полную структуру Extended_Interface для Browser_Activity
2. THE шаблон SHALL содержать комментарии с инструкциями по замене плейсхолдеров
3. THE шаблон SHALL включать примеры для простого заголовка и заголовка с параметрами
4. THE шаблон SHALL содержать правильные значения всех атрибутов (Margin, VerticalAlignment, FontSize, Foreground и т.д.)
5. THE шаблон SHALL быть сохранен в файле ExtendedInterfaceTemplate.xaml
6. THE шаблон SHALL содержать плейсхолдеры: [ACTIVITY_CLASS_NAME], [ICON_PATH], [ACTIVITY_TITLE], [PARAMETER_LABEL], [PROPERTY_NAME]

### Requirement 11: Применение изменений ко всем Browser активностям

**User Story:** Как пользователь Primo Platform, я хочу, чтобы все Browser активности имели единообразный интерфейс, чтобы получить консистентный пользовательский опыт.

#### Acceptance Criteria

1. THE модернизация SHALL быть применена к AlertHandle.xaml
2. THE модернизация SHALL быть применена к BrowserClose.xaml
3. THE модернизация SHALL быть применена к BrowserNavigate.xaml
4. THE модернизация SHALL быть применена к BrowserGetInfo.xaml
5. THE модернизация SHALL быть применена к BrowserScreenshot.xaml
6. THE модернизация SHALL быть применена к BrowserExecuteJavaScript.xaml
7. THE модернизация SHALL быть применена к BrowserWaitFor.xaml
8. THE модернизация SHALL быть применена к BrowserSwitchTo.xaml
9. THE модернизация SHALL быть применена к BrowserTabManage.xaml
10. THE модернизация SHALL быть применена к BrowserWindowManage.xaml
11. THE модернизация SHALL быть применена к BrowserManageCookies.xaml
12. THE модернизация SHALL быть применена к BrowserStorageManage.xaml
13. THE модернизация SHALL быть применена к BrowserGetLogs.xaml
14. THE модернизация SHALL быть применена к BrowserOpen.xaml (с сохранением Container_Layout)
