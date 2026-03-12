# Implementation Plan: XAML Interface Unification

## Overview

Данный план описывает пошаговую реализацию унификации XAML интерфейсов для активностей Browser модуля. Задачи включают обновление 13 XAML файлов для применения расширенного интерфейса к Locator-Based активностям, специальную обработку ElementDragDrop с двумя локаторами, создание property-based тестов для валидации изменений и документирование результатов.

## Tasks

- [ ] 1. Подготовка к изменениям
  - [x] 1.1 Создать резервные копии всех XAML файлов, которые будут изменены
    - Создать папку `.kiro/specs/xaml-interface-unification/backups/`
    - Скопировать 13 XAML файлов Locator-Based активностей в папку backups
    - _Requirements: 6.1, 6.2_

  - [x] 1.2 Создать шаблон Extended Interface для повторного использования
    - Создать файл `.kiro/specs/xaml-interface-unification/templates/ExtendedInterfaceTemplate.xaml`
    - Включить полную структуру Extended Interface из design.md
    - Добавить комментарии для замены специфичных элементов (название активности, путь к иконке)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_

  - [x] 1.3 Проверить существующие Back.cs файлы на наличие необходимых свойств
    - Проверить что все 13 активностей имеют свойства Prop_LocatorType и Prop_LocatorValue
    - Проверить что ElementDragDrop имеет Prop_SourceLocatorType, Prop_SourceLocatorValue, Prop_TargetLocatorType, Prop_TargetLocatorValue
    - Создать список активностей с отсутствующими свойствами (если есть)
    - _Requirements: 2.1, 2.3, 3.3, 3.4, 3.6, 3.7, 6.5_

- [x] 2. Обновление XAML файлов для Locator-Based активностей (группа 1)
  - [x] 2.1 Обновить Browser/ElementExists.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Проверка существования элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 2.2 Обновить Browser/ElementIsVisible.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Проверка видимости элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 2.3 Обновить Browser/ElementSelect.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Выбор опции в элементе"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 2.4 Обновить Browser/ElementSelectMultiple.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Множественный выбор опций"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

- [x] 3. Обновление XAML файлов для Locator-Based активностей (группа 2)
  - [x] 3.1 Обновить Browser/ElementGetComputedStyle.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Получение вычисленного стиля"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 3.2 Обновить Browser/ElementGetProperty.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Получение свойства элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 3.3 Обновить Browser/ElementGetRect.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Получение размеров элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 3.4 Обновить Browser/ElementGetScreenshot.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Скриншот элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

- [x] 4. Обновление XAML файлов для Locator-Based активностей (группа 3)
  - [x] 4.1 Обновить Browser/ElementSubmit.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Отправка формы"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 4.2 Обновить Browser/ElementUploadFile.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Загрузка файла"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 4.3 Обновить Browser/ElementFind.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Поиск элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

  - [x] 4.4 Обновить Browser/ElementWaitAndCheck.xaml
    - Заменить содержимое на Extended Interface структуру
    - Установить заголовок "Ожидание и проверка элемента"
    - Настроить привязки к Prop_LocatorType и Prop_LocatorValue
    - Установить путь к иконке
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 5.1-5.9_

- [x] 5. Специальная обработка ElementDragDrop
  - [x] 5.1 Обновить Browser/ElementDragDrop.xaml с двумя наборами локаторов
    - Заменить содержимое на Extended Interface структуру с двумя наборами элементов управления
    - Установить заголовок "Перетаскивание элемента"
    - Добавить TextBlock "Исходный элемент" перед первым набором
    - Настроить привязки к Prop_SourceLocatorType и Prop_SourceLocatorValue
    - Добавить TextBlock "Целевой элемент" перед вторым набором
    - Настроить привязки к Prop_TargetLocatorType и Prop_TargetLocatorValue
    - Установить d:DesignHeight="120" для UserControl
    - Установить путь к иконке
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9_

- [x] 6. Компиляция и базовая проверка
  - [ ] 6.1 Скомпилировать проект и проверить отсутствие ошибок
    - Выполнить полную сборку проекта
    - Проверить что все XAML файлы компилируются без ошибок
    - Проверить что нет ошибок привязки данных
    - _Requirements: 6.1, 6.2, 6.3, 6.5_

  - [ ] 6.2 Проверить визуальное отображение измененных активностей в дизайнере
    - Открыть каждый измененный XAML файл в дизайнере Primo Platform
    - Проверить что ComboBox и TextBox отображаются корректно
    - Проверить что иконки загружаются правильно
    - Проверить что заголовки отображаются корректно
    - _Requirements: 6.4_

- [x] 7. Checkpoint - Убедиться что все изменения применены корректно
  - Ensure all tests pass, ask the user if questions arise.

- [ ]* 8. Создание property-based тестов для валидации структуры XAML
  - [ ]* 8.1 Настроить тестовый проект с FsCheck
    - Создать тестовый проект (если не существует)
    - Добавить NuGet пакет FsCheck
    - Создать класс XamlInterfaceUnificationTests.cs
    - _Requirements: 6.3_

  - [ ]* 8.2 Написать property test для Property 1: Extended Interface Structure Completeness
    - **Property 1: Extended Interface Structure Completeness**
    - **Validates: Requirements 1.1, 1.2, 1.6, 1.7, 5.1, 5.2, 5.3**
    - Создать тест проверяющий наличие всех обязательных элементов Extended Interface
    - Проверить ObjectDataProvider, ComboBox, TextBox, Grid, Image, StackPanel, TextBlock
    - Применить к всем 13 Locator-Based активностям
    - _Requirements: 1.1, 1.2, 1.6, 1.7, 5.1, 5.2, 5.3_

  - [ ]* 8.3 Написать property test для Property 2: Extended Interface Attribute Consistency
    - **Property 2: Extended Interface Attribute Consistency**
    - **Validates: Requirements 1.3, 1.4, 1.8, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9**
    - Создать тест проверяющий корректность всех атрибутов Extended Interface
    - Проверить d:DesignHeight, Margin, Background, VerticalAlignment, Height
    - Применить к всем 13 Locator-Based активностям
    - _Requirements: 1.3, 1.4, 1.8, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_

  - [ ]* 8.4 Написать property test для Property 5: Locator-Based Activities Interface Mapping
    - **Property 5: Locator-Based Activities Interface Mapping**
    - **Validates: Requirements 1.5, 2.3**
    - Создать тест проверяющий что все активности с Prop_LocatorType и Prop_LocatorValue используют Extended Interface
    - Использовать рефлексию для анализа Back.cs файлов
    - Парсить XAML для проверки структуры интерфейса
    - _Requirements: 1.5, 2.3_

  - [ ]* 8.5 Написать property test для Property 8: ElementDragDrop Dual Locator Structure
    - **Property 8: ElementDragDrop Dual Locator Structure**
    - **Validates: Requirements 3.1, 3.3, 3.4, 3.6, 3.7**
    - Создать тест проверяющий наличие двух наборов локаторов в ElementDragDrop.xaml
    - Проверить привязки к Source и Target свойствам
    - Проверить наличие TextBlock меток "Исходный элемент" и "Целевой элемент"
    - _Requirements: 3.1, 3.3, 3.4, 3.6, 3.7_

  - [ ]* 8.6 Написать property test для Property 9: XAML Binding Validity
    - **Property 9: XAML Binding Validity**
    - **Validates: Requirements 6.5, 6.6**
    - Создать тест проверяющий что все Binding выражения в XAML ссылаются на существующие свойства
    - Извлечь все Binding из XAML файлов
    - Использовать рефлексию для проверки существования свойств в Back.cs
    - Применить ко всем измененным XAML файлам
    - _Requirements: 6.5, 6.6_

- [ ]* 9. Создание unit тестов для конкретных примеров
  - [ ]* 9.1 Написать unit test для ElementClick.xaml как эталона Extended Interface
    - Проверить что ElementClick.xaml (уже использующий Extended Interface) соответствует спецификации
    - Использовать как reference для сравнения с измененными файлами
    - _Requirements: 1.1-1.8, 5.1-5.9_

  - [ ]* 9.2 Написать unit test для AlertHandle.xaml как эталона Compact Interface
    - Проверить что AlertHandle.xaml (Browser активность) использует Compact Interface
    - Проверить отсутствие Background в Grid
    - Проверить VerticalAlignment="Center" для StackPanel
    - _Requirements: 4.1, 4.3, 4.4, 4.5, 4.6_

  - [ ]* 9.3 Написать unit test для ElementDragDrop.xaml специального случая
    - Проверить наличие двух ComboBox и двух TextBox
    - Проверить правильность привязок к Source и Target свойствам
    - Проверить d:DesignHeight="120"
    - _Requirements: 3.1-3.9_

  - [ ]* 9.4 Написать unit test для ElementHover.xaml (ElementId-Based активность)
    - Проверить что ElementHover.xaml использует Compact Interface
    - Проверить что нет ComboBox и TextBox для локаторов
    - Проверить привязку к Prop_ElementId
    - _Requirements: 2.4, 4.2_

- [x] 10. Создание документации
  - [x] 10.1 Создать документ XAML_INTERFACE_GUIDE.md
    - Создать файл `.kiro/specs/xaml-interface-unification/XAML_INTERFACE_GUIDE.md`
    - Описать правила выбора типа интерфейса для новых активностей
    - Включить примеры Extended и Compact интерфейсов
    - Добавить таблицу с классификацией всех активностей
    - Описать визуальные различия между интерфейсами
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [x] 10.2 Создать список измененных файлов CHANGES.md
    - Создать файл `.kiro/specs/xaml-interface-unification/CHANGES.md`
    - Перечислить все 13 измененных XAML файлов
    - Указать тип изменения для каждого файла (Compact → Extended)
    - Добавить специальное примечание для ElementDragDrop
    - Указать файлы которые не изменялись и почему
    - _Requirements: 7.1, 7.2_

- [ ] 11. Final checkpoint - Финальная проверка
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Задачи помеченные `*` являются опциональными и могут быть пропущены для более быстрой реализации MVP
- Каждая задача ссылается на конкретные требования для обеспечения трассируемости
- Checkpoint задачи обеспечивают инкрементальную валидацию
- Property tests валидируют универсальные свойства корректности
- Unit tests валидируют конкретные примеры и граничные случаи
- Все изменения касаются только XAML файлов, .xaml.cs и Back.cs файлы остаются без изменений
- Используется C# и XAML (WPF) для реализации
