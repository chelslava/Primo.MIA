# Implementation Plan: Browser Interface Modernization

## Overview

Данный план описывает пошаговую реализацию модернизации XAML интерфейсов активностей Browser модуля для приведения их к единому стандарту Extended Interface. Задачи включают установку стандартной высоты дизайна d:DesignHeight="80", добавление отступов Margin="0,0,0,5", добавление фонового цвета Grid Background, стандартизацию структуры макета, унификацию отступов и выравнивания, установку цвета текста Foreground="#FF0E0E0E", специальную обработку BrowserOpen как активности-контейнера, создание шаблона для новых активностей и документирование результатов.

## Tasks

- [x] 1. Создание шаблона Extended Interface
  - [x] 1.1 Создать файл ExtendedInterfaceTemplate.xaml
    - Создать файл `.kiro/specs/browser-interface-modernization/ExtendedInterfaceTemplate.xaml`
    - Включить полную структуру Standard_Layout из design.md с Extended Interface
    - Добавить подробные комментарии с инструкциями по использованию
    - Включить примеры для простого заголовка и заголовка с параметрами
    - Добавить плейсхолдеры: [ACTIVITY_CLASS_NAME], [ICON_PATH], [ACTIVITY_TITLE], [PARAMETER_LABEL], [PROPERTY_NAME]
    - Включить d:DesignHeight="80", Margin="0,0,0,5", Grid Background, Foreground="#FF0E0E0E"
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

- [x] 2. Модернизация стандартных Browser активностей (группа 1)
  - [x] 2.1 Модернизировать Browser/AlertHandle.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.1_

  - [x] 2.2 Модернизировать Browser/BrowserClose.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 11.2_

  - [x] 2.3 Модернизировать Browser/BrowserNavigate.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.3_

  - [x] 2.4 Модернизировать Browser/BrowserGetInfo.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.4_

  - [x] 2.5 Модернизировать Browser/BrowserScreenshot.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.5_

- [x] 3. Модернизация стандартных Browser активностей (группа 2)
  - [x] 3.1 Модернизировать Browser/BrowserExecuteJavaScript.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.6_

  - [x] 3.2 Модернизировать Browser/BrowserWaitFor.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.7_

  - [x] 3.3 Модернизировать Browser/BrowserSwitchTo.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.8_

  - [x] 3.4 Модернизировать Browser/BrowserTabManage.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.9_

- [x] 4. Модернизация стандартных Browser активностей (группа 3)
  - [x] 4.1 Модернизировать Browser/BrowserWindowManage.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.10_

  - [x] 4.2 Модернизировать Browser/BrowserManageCookies.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.11_

  - [x] 4.3 Модернизировать Browser/BrowserStorageManage.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.12_

  - [x] 4.4 Модернизировать Browser/BrowserGetLogs.xaml
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.5, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.13_

- [x] 5. Специальная обработка BrowserOpen (активность-контейнер)
  - [x] 5.1 Модернизировать Browser/BrowserOpen.xaml с сохранением Container_Layout
    - Установить атрибут d:DesignHeight="80" в UserControl
    - Добавить атрибут Margin="0,0,0,5" в UserControl
    - Сохранить Grid.RowDefinitions (Auto + *) для контейнерной структуры
    - Добавить Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" в Grid.Row="0" (заголовок)
    - Модернизировать заголовок (Grid.Row="0") с применением Standard_Layout
    - Очистить Grid.ColumnDefinitions в заголовке от лишних пробелов
    - Стандартизировать Image в заголовке: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel в заголовке: Margin="5,0,5,5", VerticalAlignment="Top"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12", Foreground="#FF0E0E0E"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - Сохранить контейнер (Grid.Row="1") с WFContainerBase без изменений
    - _Requirements: 1.2, 1.5, 2.2, 2.4, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 11.14_

- [x] 6. Checkpoint - Проверка модернизации всех активностей
  - Ensure all tests pass, ask the user if questions arise.

- [ ]* 7. Валидация изменений
  - [ ]* 7.1 Проверить XAML синтаксис всех измененных файлов
    - Использовать getDiagnostics для проверки всех 14 измененных XAML файлов
    - Проверить отсутствие ошибок компиляции
    - Проверить отсутствие предупреждений о привязках данных
    - _Requirements: 8.1, 8.4_

  - [ ]* 7.2 Проверить соответствие привязок данных свойствам Back.cs
    - Для каждого измененного XAML файла проверить соответствующий Back.cs
    - Убедиться что все Binding ссылаются на существующие свойства
    - Проверить что имена свойств совпадают (Prop_PropertyName)
    - _Requirements: 8.3_

  - [ ]* 7.3 Проверить визуальное отображение в дизайнере
    - Открыть каждый измененный XAML файл в дизайнере
    - Проверить что иконки отображаются корректно
    - Проверить что заголовки и параметры отображаются правильно
    - Проверить наличие фонового цвета и стандартной высоты
    - _Requirements: 8.5_

- [ ]* 8. Создание property-based тестов для валидации
  - [ ]* 8.1 Написать property test для проверки d:DesignHeight="80"
    - **Property 1: Standard DesignHeight in Browser Activities**
    - **Validates: Requirements 1.1, 1.2, 1.5**
    - Создать тест проверяющий что все Browser активности имеют d:DesignHeight="80"
    - Парсить XAML файлы и проверять атрибуты UserControl
    - Применить ко всем 14 Browser активностям
    - _Requirements: 1.1, 1.2, 1.5_

  - [ ]* 8.2 Написать property test для проверки Grid Background
    - **Property 2: Grid Background Consistency**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
    - Создать тест проверяющий наличие Background в Grid
    - Проверить что Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}"
    - Применить ко всем Browser активностям
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ]* 8.3 Написать property test для проверки стандартизации атрибутов
    - **Property 3: Standard Layout Attribute Consistency**
    - **Validates: Requirements 3.5, 3.6, 4.1, 4.2, 4.3, 4.4**
    - Создать тест проверяющий консистентность Margin и VerticalAlignment
    - Проверить Image: Margin="5", VerticalAlignment="Center"
    - Проверить StackPanel: Margin="5,0,5,5", VerticalAlignment="Top"
    - Применить ко всем 14 Browser активностям
    - _Requirements: 3.5, 3.6, 4.1, 4.2, 4.3, 4.4_

  - [ ]* 8.4 Написать property test для проверки структуры Grid.ColumnDefinitions
    - **Property 4: Clean Grid.ColumnDefinitions Format**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    - Создать тест проверяющий отсутствие лишних пробелов в Grid.ColumnDefinitions
    - Проверить что ColumnDefinition элементы записаны компактно
    - Применить ко всем Browser активностям с Standard_Layout
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ]* 8.5 Написать property test для проверки консистентности TextBlock элементов
    - **Property 5: TextBlock Structure Consistency**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**
    - Создать тест проверяющий FontWeight="Bold", FontSize="12" и Foreground="#FF0E0E0E" для заголовков
    - Проверить FontSize="10" и Foreground="Gray" для параметров
    - Проверить формат Run элементов с привязками
    - Применить ко всем Browser активностям
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 9. Создание unit тестов для конкретных примеров
  - [ ]* 9.1 Написать unit test для AlertHandle.xaml
    - Проверить что AlertHandle.xaml соответствует Standard_Layout с Extended Interface
    - Проверить d:DesignHeight="80", Margin="0,0,0,5", Grid Background
    - Проверить консистентность всех атрибутов
    - _Requirements: 1.1, 2.1, 3.1-3.8, 4.1-4.5, 11.1_

  - [ ]* 9.2 Написать unit test для BrowserOpen.xaml
    - Проверить что BrowserOpen.xaml использует Container_Layout с Extended Interface
    - Проверить наличие Grid.RowDefinitions (Auto + *)
    - Проверить что заголовок (Grid.Row="0") использует Standard_Layout с Background
    - Проверить что контейнер (Grid.Row="1") содержит WFContainerBase
    - _Requirements: 5.1-5.7, 11.14_

  - [ ]* 9.3 Написать unit test для BrowserClose.xaml
    - Проверить что BrowserClose.xaml имеет простой заголовок без параметров
    - Проверить отсутствие второго TextBlock для параметров
    - Проверить консистентность атрибутов Extended Interface
    - _Requirements: 1.1, 2.1, 3.1-3.8, 4.1-4.5, 11.2_

- [ ] 10. Создание документации
  - [ ] 10.1 Создать документ CHANGES.md
    - Создать файл `.kiro/specs/browser-interface-modernization/CHANGES.md`
    - Перечислить все 14 измененных XAML файлов
    - Указать тип изменения для каждого файла (добавление Background, d:DesignHeight="80", Margin, Foreground)
    - Добавить специальное примечание для BrowserOpen (Container_Layout)
    - Включить сравнение "до" и "после" для ключевых атрибутов
    - Добавить статистику изменений
    - Включить правила применения Extended Interface к новым Browser активностям
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

- [x] 11. Final checkpoint - Финальная проверка
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Задачи помеченные `*` являются опциональными и могут быть пропущены для более быстрой реализации MVP
- Каждая задача ссылается на конкретные требования для обеспечения трассируемости
- Checkpoint задачи обеспечивают инкрементальную валидацию
- Property tests валидируют универсальные свойства корректности
- Unit tests валидируют конкретные примеры и граничные случаи
- Все изменения касаются только XAML файлов, .xaml.cs и Back.cs файлы остаются без изменений
- Используется XAML (WPF) для реализации интерфейсов
- BrowserOpen требует специальной обработки как активность-контейнер с сохранением Grid.RowDefinitions
- Шаблон ExtendedInterfaceTemplate.xaml создается в начале для использования при создании новых активностей
- Extended Interface включает: d:DesignHeight="80", Margin="0,0,0,5", Grid Background, Foreground="#FF0E0E0E"
