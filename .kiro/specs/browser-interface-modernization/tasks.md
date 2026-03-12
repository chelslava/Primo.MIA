# Implementation Plan: Browser Interface Modernization

## Overview

Данный план описывает пошаговую реализацию модернизации XAML интерфейсов активностей Browser модуля для приведения их к единому стандарту Compact Interface. Задачи включают удаление избыточного d:DesignHeight, стандартизацию структуры макета, унификацию отступов и выравнивания, специальную обработку BrowserOpen как активности-контейнера, создание шаблона для новых активностей и документирование результатов.

## Tasks

- [ ] 1. Создание шаблона Compact Interface
  - [ ] 1.1 Создать файл CompactInterfaceTemplate.xaml
    - Создать файл `.kiro/specs/browser-interface-modernization/CompactInterfaceTemplate.xaml`
    - Включить полную структуру Standard_Layout из design.md
    - Добавить подробные комментарии с инструкциями по использованию
    - Включить примеры для простого заголовка и заголовка с параметрами
    - Добавить плейсхолдеры: [ACTIVITY_CLASS_NAME], [ICON_PATH], [ACTIVITY_TITLE], [PARAMETER_LABEL], [PROPERTY_NAME]
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

- [ ] 2. Модернизация стандартных Browser активностей (группа 1)
  - [ ] 2.1 Модернизировать Browser/AlertHandle.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.1_

  - [ ] 2.2 Модернизировать Browser/BrowserClose.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.2_

  - [ ] 2.3 Модернизировать Browser/BrowserNavigate.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.3_

  - [ ] 2.4 Модернизировать Browser/BrowserGetInfo.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.4_

  - [ ] 2.5 Модернизировать Browser/BrowserScreenshot.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.5_

- [ ] 3. Модернизация стандартных Browser активностей (группа 2)
  - [ ] 3.1 Модернизировать Browser/BrowserExecuteJavaScript.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.6_

  - [ ] 3.2 Модернизировать Browser/BrowserWaitFor.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.7_

  - [ ] 3.3 Модернизировать Browser/BrowserSwitchTo.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.8_

  - [ ] 3.4 Модернизировать Browser/BrowserTabManage.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.9_

- [ ] 4. Модернизация стандартных Browser активностей (группа 3)
  - [ ] 4.1 Модернизировать Browser/BrowserWindowManage.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.10_

  - [ ] 4.2 Модернизировать Browser/BrowserManageCookies.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.11_

  - [ ] 4.3 Модернизировать Browser/BrowserStorageManage.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.12_

  - [ ] 4.4 Модернизировать Browser/BrowserGetLogs.xaml
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Очистить Grid.ColumnDefinitions от лишних пробелов и переносов строк
    - Стандартизировать Image: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - _Requirements: 1.1, 1.3, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4, 7.5, 11.13_

- [ ] 5. Специальная обработка BrowserOpen (активность-контейнер)
  - [ ] 5.1 Модернизировать Browser/BrowserOpen.xaml с сохранением Container_Layout
    - Удалить атрибут d:DesignHeight="450" из UserControl
    - Сохранить Grid.RowDefinitions (Auto + *) для контейнерной структуры
    - Модернизировать заголовок (Grid.Row="0") с применением Standard_Layout
    - Очистить Grid.ColumnDefinitions в заголовке от лишних пробелов
    - Стандартизировать Image в заголовке: Margin="5", VerticalAlignment="Center"
    - Стандартизировать StackPanel в заголовке: Margin="5", VerticalAlignment="Center"
    - Проверить TextBlock заголовок: FontWeight="Bold", FontSize="12"
    - Проверить TextBlock параметры: FontSize="10", Foreground="Gray"
    - Сохранить контейнер (Grid.Row="1") с WFContainerBase без изменений
    - _Requirements: 1.2, 1.4, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 11.14_

- [ ] 6. Checkpoint - Проверка модернизации всех активностей
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
    - Проверить отсутствие лишнего пространства (эффект от удаления d:DesignHeight)
    - _Requirements: 8.5_

- [ ]* 8. Создание property-based тестов для валидации
  - [ ]* 8.1 Написать property test для проверки отсутствия d:DesignHeight
    - **Property 1: No DesignHeight in Browser Activities**
    - **Validates: Requirements 1.1, 1.2, 1.5**
    - Создать тест проверяющий что все Browser активности не содержат d:DesignHeight
    - Парсить XAML файлы и проверять атрибуты UserControl
    - Применить ко всем 14 Browser активностям
    - _Requirements: 1.1, 1.2, 1.5_

  - [ ]* 8.2 Написать property test для проверки стандартизации атрибутов
    - **Property 2: Standard Layout Attribute Consistency**
    - **Validates: Requirements 2.5, 2.6, 3.1, 3.2, 3.3, 3.4**
    - Создать тест проверяющий консистентность Margin и VerticalAlignment
    - Проверить Image: Margin="5", VerticalAlignment="Center"
    - Проверить StackPanel: Margin="5", VerticalAlignment="Center"
    - Применить ко всем 14 Browser активностям
    - _Requirements: 2.5, 2.6, 3.1, 3.2, 3.3, 3.4_

  - [ ]* 8.3 Написать property test для проверки структуры Grid.ColumnDefinitions
    - **Property 3: Clean Grid.ColumnDefinitions Format**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4**
    - Создать тест проверяющий отсутствие лишних пробелов в Grid.ColumnDefinitions
    - Проверить что ColumnDefinition элементы записаны компактно
    - Применить ко всем Browser активностям с Standard_Layout
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ]* 8.4 Написать property test для проверки отсутствия Background в Compact Interface
    - **Property 4: No Background in Compact Interface**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    - Создать тест проверяющий что Grid не содержит Background атрибут
    - Применить ко всем Browser активностям
    - Убедиться что это отличает Compact от Extended Interface
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ]* 8.5 Написать property test для проверки консистентности TextBlock элементов
    - **Property 5: TextBlock Structure Consistency**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**
    - Создать тест проверяющий FontWeight="Bold" и FontSize="12" для заголовков
    - Проверить FontSize="10" и Foreground="Gray" для параметров
    - Проверить формат Run элементов с привязками
    - Применить ко всем Browser активностям
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 9. Создание unit тестов для конкретных примеров
  - [ ]* 9.1 Написать unit test для AlertHandle.xaml
    - Проверить что AlertHandle.xaml соответствует Standard_Layout
    - Проверить отсутствие d:DesignHeight
    - Проверить консистентность всех атрибутов
    - _Requirements: 1.1, 2.1-2.8, 3.1-3.5, 11.1_

  - [ ]* 9.2 Написать unit test для BrowserOpen.xaml
    - Проверить что BrowserOpen.xaml использует Container_Layout
    - Проверить наличие Grid.RowDefinitions (Auto + *)
    - Проверить что заголовок (Grid.Row="0") использует Standard_Layout
    - Проверить что контейнер (Grid.Row="1") содержит WFContainerBase
    - _Requirements: 4.1-4.7, 11.14_

  - [ ]* 9.3 Написать unit test для BrowserClose.xaml
    - Проверить что BrowserClose.xaml имеет простой заголовок без параметров
    - Проверить отсутствие второго TextBlock для параметров
    - Проверить консистентность атрибутов
    - _Requirements: 1.1, 2.1-2.8, 3.1-3.5, 11.2_

- [ ] 10. Создание документации
  - [ ] 10.1 Создать документ CHANGES.md
    - Создать файл `.kiro/specs/browser-interface-modernization/CHANGES.md`
    - Перечислить все 14 измененных XAML файлов
    - Указать тип изменения для каждого файла (удаление d:DesignHeight, стандартизация атрибутов)
    - Добавить специальное примечание для BrowserOpen (Container_Layout)
    - Включить сравнение "до" и "после" для ключевых атрибутов
    - Добавить статистику изменений
    - Включить правила применения Compact Interface к новым Browser активностям
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

- [ ] 11. Final checkpoint - Финальная проверка
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
- Шаблон CompactInterfaceTemplate.xaml создается в начале для использования при создании новых активностей
