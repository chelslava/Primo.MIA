# Requirements Document

## Introduction

Данный документ описывает требования к унификации XAML интерфейсов активностей Browser модуля в Primo Platform. Цель - применить расширенный интерфейс с ComboBox для выбора типа локатора и TextBox для ввода значения локатора ко всем Element активностям, которые работают с локаторами элементов (Prop_LocatorType и Prop_LocatorValue).

## Glossary

- **Element_Activity**: Активность Browser модуля, работающая с элементами веб-страницы
- **Browser_Activity**: Активность Browser модуля, работающая с браузером в целом
- **Extended_Interface**: XAML интерфейс с ComboBox для Prop_LocatorType и TextBox для Prop_LocatorValue
- **Compact_Interface**: XAML интерфейс, показывающий только иконку и текстовое описание
- **Locator_Based_Activity**: Element активность, использующая Prop_LocatorType и Prop_LocatorValue для поиска элементов
- **ElementId_Based_Activity**: Element активность, использующая Prop_ElementId вместо локаторов
- **XAML_Interface**: Визуальный интерфейс активности в дизайнере Primo Platform
- **Prop_LocatorType**: Свойство активности, определяющее тип локатора (CSS, XPath, ID и т.д.)
- **Prop_LocatorValue**: Свойство активности, содержащее значение локатора
- **Prop_ElementId**: Свойство активности, содержащее идентификатор ранее найденного элемента

## Requirements

### Requirement 1: Применение расширенного интерфейса к активностям с одним локатором

**User Story:** Как пользователь Primo Platform, я хочу видеть ComboBox для выбора типа локатора и TextBox для ввода значения локатора в визуальном интерфейсе Element активностей, чтобы удобно настраивать поиск элементов без открытия окна свойств.

#### Acceptance Criteria

1. THE Extended_Interface SHALL содержать ComboBox с привязкой к Prop_LocatorType
2. THE Extended_Interface SHALL содержать TextBox с привязкой к Prop_LocatorValue
3. THE Extended_Interface SHALL использовать VerticalAlignment="Top" для StackPanel
4. THE Extended_Interface SHALL использовать Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" для Grid
5. WHEN активность является Locator_Based_Activity с одним локатором, THE XAML_Interface SHALL использовать Extended_Interface
6. THE Extended_Interface SHALL содержать ObjectDataProvider для ElementLocatorType enum
7. THE Extended_Interface SHALL содержать заголовок с названием активности в TextBlock с FontWeight="Bold" и FontSize="12"
8. THE Extended_Interface SHALL использовать Margin="0,0,0,5" для UserControl

### Requirement 2: Идентификация активностей, требующих расширенный интерфейс

**User Story:** Как разработчик, я хочу четко определить, какие активности должны использовать расширенный интерфейс, чтобы применить изменения к правильному набору файлов.

#### Acceptance Criteria

1. THE Locator_Based_Activity SHALL быть определена как Element активность с свойствами Prop_LocatorType и Prop_LocatorValue
2. THE ElementId_Based_Activity SHALL быть определена как Element активность с свойством Prop_ElementId вместо локаторов
3. WHEN активность имеет Prop_LocatorType и Prop_LocatorValue, THE активность SHALL использовать Extended_Interface
4. WHEN активность имеет только Prop_ElementId, THE активность SHALL использовать Compact_Interface
5. THE Browser_Activity SHALL всегда использовать Compact_Interface
6. THE список Locator_Based_Activity SHALL включать: ElementExists, ElementIsVisible, ElementSelect, ElementSelectMultiple, ElementGetComputedStyle, ElementGetProperty, ElementGetRect, ElementGetScreenshot, ElementSubmit, ElementUploadFile, ElementFind, ElementWaitAndCheck
7. THE список ElementId_Based_Activity SHALL включать: ElementHover, ElementInput, ElementScrollTo

### Requirement 3: Обработка активности ElementDragDrop с двумя локаторами

**User Story:** Как пользователь Primo Platform, я хочу видеть интерфейс для настройки исходного и целевого локаторов в активности ElementDragDrop, чтобы удобно настраивать операцию перетаскивания.

#### Acceptance Criteria

1. THE ElementDragDrop XAML_Interface SHALL содержать два набора элементов управления для локаторов
2. THE ElementDragDrop XAML_Interface SHALL содержать TextBlock с текстом "Исходный элемент" перед первым набором локаторов
3. THE ElementDragDrop XAML_Interface SHALL содержать ComboBox с привязкой к Prop_SourceLocatorType
4. THE ElementDragDrop XAML_Interface SHALL содержать TextBox с привязкой к Prop_SourceLocatorValue
5. THE ElementDragDrop XAML_Interface SHALL содержать TextBlock с текстом "Целевой элемент" перед вторым набором локаторов
6. THE ElementDragDrop XAML_Interface SHALL содержать ComboBox с привязкой к Prop_TargetLocatorType
7. THE ElementDragDrop XAML_Interface SHALL содержать TextBox с привязкой к Prop_TargetLocatorValue
8. THE ElementDragDrop XAML_Interface SHALL использовать VerticalAlignment="Top" для StackPanel
9. THE ElementDragDrop XAML_Interface SHALL использовать Background="{DynamicResource {x:Static SystemColors.ScrollBarBrushKey}}" для Grid

### Requirement 4: Сохранение компактного интерфейса для соответствующих активностей

**User Story:** Как пользователь Primo Platform, я хочу, чтобы Browser активности и Element активности без локаторов сохранили компактный интерфейс, чтобы не загромождать визуальный дизайнер лишними элементами управления.

#### Acceptance Criteria

1. WHEN активность является Browser_Activity, THE XAML_Interface SHALL использовать Compact_Interface
2. WHEN активность является ElementId_Based_Activity, THE XAML_Interface SHALL использовать Compact_Interface
3. THE Compact_Interface SHALL использовать VerticalAlignment="Center" для StackPanel
4. THE Compact_Interface SHALL НЕ содержать Background для Grid
5. THE Compact_Interface SHALL содержать TextBlock с FontWeight="Bold" и FontSize="12" для заголовка
6. THE Compact_Interface SHALL содержать TextBlock с FontSize="10" и Foreground="Gray" для отображения параметра
7. THE список Browser_Activity SHALL включать: BrowserOpen, BrowserClose, BrowserNavigate, BrowserGetInfo, BrowserScreenshot, BrowserExecuteJavaScript, BrowserWaitFor, BrowserSwitchTo, BrowserTabManage, BrowserWindowManage, BrowserManageCookies, BrowserStorageManage, BrowserGetLogs, AlertHandle

### Requirement 5: Консистентность структуры XAML файлов

**User Story:** Как разработчик, я хочу, чтобы все XAML файлы с расширенным интерфейсом имели единообразную структуру, чтобы упростить поддержку и модификацию кода.

#### Acceptance Criteria

1. THE Extended_Interface SHALL использовать Grid с двумя колонками (Auto и *)
2. THE Extended_Interface SHALL содержать Image в Grid.Column="0" с размерами 32x32
3. THE Extended_Interface SHALL содержать StackPanel в Grid.Column="1"
4. THE Extended_Interface SHALL использовать Margin="5" для Image
5. THE Extended_Interface SHALL использовать Margin="5,0,5,5" для StackPanel
6. THE Extended_Interface SHALL использовать Height="22" для ComboBox и TextBox
7. THE Extended_Interface SHALL использовать Margin="0,2,6,2" для ComboBox
8. THE Extended_Interface SHALL использовать Margin="0,2,6,0" для TextBox
9. THE Extended_Interface SHALL использовать d:DesignHeight="80" для UserControl

### Requirement 6: Валидация изменений

**User Story:** Как разработчик, я хочу убедиться, что все изменения XAML файлов не нарушают функциональность активностей, чтобы избежать регрессии.

#### Acceptance Criteria

1. WHEN XAML файл изменен, THE соответствующий .xaml.cs файл SHALL оставаться без изменений
2. WHEN XAML файл изменен, THE соответствующий Back.cs файл SHALL оставаться без изменений
3. THE измененные XAML файлы SHALL компилироваться без ошибок
4. THE измененные XAML файлы SHALL корректно отображаться в дизайнере Primo Platform
5. THE привязки данных (Binding) в XAML SHALL соответствовать существующим свойствам в Back.cs файлах
6. WHEN активность использует динамический заголовок, THE TextBlock SHALL содержать Run элементы с привязками к соответствующим свойствам

### Requirement 7: Документирование изменений

**User Story:** Как разработчик, я хочу иметь документацию по изменениям XAML интерфейсов, чтобы понимать, какие файлы были изменены и почему.

#### Acceptance Criteria

1. THE документация SHALL содержать список всех измененных XAML файлов
2. THE документация SHALL содержать описание типа интерфейса для каждой активности
3. THE документация SHALL содержать примеры расширенного и компактного интерфейсов
4. THE документация SHALL содержать правила определения типа интерфейса для новых активностей
5. THE документация SHALL быть создана в формате Markdown
6. THE документация SHALL содержать скриншоты или описания визуальных различий между интерфейсами
