namespace Primo.MIA
{
    /// <summary>
    /// Тип браузера для автоматизации
    /// </summary>
    public enum BrowserType
    {
        /// <summary>Google Chrome</summary>
        [RuName("Google Chrome")]
        Chrome,
        /// <summary>Mozilla Firefox</summary>
        [RuName("Mozilla Firefox")]
        Firefox,
        /// <summary>Microsoft Edge</summary>
        [RuName("Microsoft Edge")]
        Edge
    }

    /// <summary>
    /// Тип локатора для поиска элементов на странице
    /// </summary>
    public enum ElementLocatorType
    {
        /// <summary>Поиск по атрибуту id</summary>
        [RuName("Id")]
        Id,
        /// <summary>Поиск по атрибуту name</summary>
        [RuName("Name")]
        Name,
        /// <summary>Поиск по имени CSS-класса</summary>
        [RuName("CSS-класс")]
        ClassName,
        /// <summary>Поиск по имени тега</summary>
        [RuName("Тег")]
        TagName,
        /// <summary>Поиск по полному тексту ссылки</summary>
        [RuName("Текст ссылки")]
        LinkText,
        /// <summary>Поиск по частичному тексту ссылки</summary>
        [RuName("Часть текста ссылки")]
        PartialLinkText,
        /// <summary>Поиск по CSS-селектору</summary>
        [RuName("CSS-селектор")]
        CssSelector,
        /// <summary>Поиск по XPath-выражению</summary>
        [RuName("XPath")]
        XPath
    }

    /// <summary>
    /// Тип условия ожидания
    /// </summary>
    public enum WaitConditionType
    {
        /// <summary>Элемент существует в DOM</summary>
        [RuName("Элемент существует")]
        ElementExists,
        /// <summary>Элемент видим на странице</summary>
        [RuName("Элемент видим")]
        ElementVisible,
        /// <summary>Элемент доступен для клика</summary>
        [RuName("Элемент кликабелен")]
        ElementClickable,
        /// <summary>Элемент невидим или отсутствует</summary>
        [RuName("Элемент невидим")]
        ElementInvisible,
        /// <summary>Текст присутствует в элементе</summary>
        [RuName("Текст присутствует")]
        TextPresent,
        /// <summary>Заголовок страницы содержит текст</summary>
        [RuName("Заголовок содержит")]
        TitleContains,
        /// <summary>URL содержит текст</summary>
        [RuName("URL содержит")]
        UrlContains,
        /// <summary>Алерт присутствует</summary>
        [RuName("Алерт присутствует")]
        AlertPresent
    }

    /// <summary>
    /// Режим выбора опции в select элементе
    /// </summary>
    public enum SelectMode
    {
        /// <summary>Выбор по видимому тексту</summary>
        [RuName("По тексту")]
        ByText,
        /// <summary>Выбор по значению атрибута value</summary>
        [RuName("По значению (value)")]
        ByValue,
        /// <summary>Выбор по индексу (начиная с 0)</summary>
        [RuName("По индексу")]
        ByIndex
    }

    /// <summary>
    /// Выравнивание при прокрутке к элементу
    /// </summary>
    public enum ScrollAlignment
    {
        /// <summary>Прокрутить элемент к верхней части окна</summary>
        [RuName("К верху")]
        Top,
        /// <summary>Прокрутить элемент к центру окна</summary>
        [RuName("К центру")]
        Center,
        /// <summary>Прокрутить элемент к нижней части окна</summary>
        [RuName("К низу")]
        Bottom
    }

    /// <summary>
    /// Операция с cookies
    /// </summary>
    public enum CookieOperation
    {
        /// <summary>Получить cookie по имени</summary>
        [RuName("Получить")]
        Get,
        /// <summary>Получить все cookies</summary>
        [RuName("Получить все")]
        GetAll,
        /// <summary>Установить cookie</summary>
        [RuName("Установить")]
        Set,
        /// <summary>Удалить cookie по имени</summary>
        [RuName("Удалить")]
        Delete,
        /// <summary>Удалить все cookies</summary>
        [RuName("Удалить все")]
        DeleteAll
    }

    /// <summary>
    /// Тип переключения контекста браузера
    /// </summary>
    public enum SwitchToType
    {
        /// <summary>Переключиться на iframe по индексу или имени</summary>
        [RuName("Фрейм (iframe)")]
        Frame,
        /// <summary>Переключиться на окно по handle</summary>
        [RuName("Окно")]
        Window,
        /// <summary>Переключиться на алерт</summary>
        [RuName("Алерт")]
        Alert,
        /// <summary>Вернуться к основному контенту страницы</summary>
        [RuName("Основной контент")]
        DefaultContent,
        /// <summary>Вернуться к родительскому фрейму</summary>
        [RuName("Родительский фрейм")]
        ParentFrame
    }

    /// <summary>
    /// Действие с алертом
    /// </summary>
    public enum AlertAction
    {
        /// <summary>Принять алерт (OK)</summary>
        [RuName("Принять (OK)")]
        Accept,
        /// <summary>Отклонить алерт (Cancel)</summary>
        [RuName("Отклонить (Cancel)")]
        Dismiss,
        /// <summary>Получить текст алерта</summary>
        [RuName("Получить текст")]
        GetText,
        /// <summary>Ввести текст в prompt</summary>
        [RuName("Ввести текст")]
        SendKeys
    }

    /// <summary>
    /// Операция с вкладками браузера
    /// </summary>
    public enum TabOperation
    {
        /// <summary>Открыть новую вкладку</summary>
        [RuName("Открыть новую вкладку")]
        OpenNewTab,
        /// <summary>Закрыть текущую вкладку</summary>
        [RuName("Закрыть текущую вкладку")]
        CloseCurrentTab,
        /// <summary>Закрыть вкладку по handle</summary>
        [RuName("Закрыть вкладку по handle")]
        CloseTabByHandle,
        /// <summary>Получить список всех handles</summary>
        [RuName("Получить все handles")]
        GetAllHandles,
        /// <summary>Получить handle текущей вкладки</summary>
        [RuName("Получить текущий handle")]
        GetCurrentHandle,
        /// <summary>Переключиться на вкладку по индексу</summary>
        [RuName("Переключиться на вкладку")]
        SwitchToTab
    }

    /// <summary>
    /// Тип хранилища браузера
    /// </summary>
    public enum StorageType
    {
        /// <summary>localStorage — данные сохраняются между сессиями</summary>
        [RuName("localStorage")]
        LocalStorage,
        /// <summary>sessionStorage — данные удаляются при закрытии вкладки</summary>
        [RuName("sessionStorage")]
        SessionStorage
    }

    /// <summary>
    /// Операция с хранилищем браузера
    /// </summary>
    public enum StorageOperation
    {
        /// <summary>Получить значение по ключу</summary>
        [RuName("Получить значение")]
        GetItem,
        /// <summary>Установить значение</summary>
        [RuName("Установить значение")]
        SetItem,
        /// <summary>Удалить ключ</summary>
        [RuName("Удалить ключ")]
        RemoveItem,
        /// <summary>Очистить всё хранилище</summary>
        [RuName("Очистить всё")]
        Clear,
        /// <summary>Получить все ключи</summary>
        [RuName("Получить все ключи")]
        GetAllKeys,
        /// <summary>Получить длину хранилища</summary>
        [RuName("Получить размер")]
        GetLength
    }

    /// <summary>
    /// Тип логов браузера
    /// </summary>
    public enum BrowserLogType
    {
        /// <summary>Логи браузера</summary>
        [RuName("Браузер")]
        Browser,
        /// <summary>Логи драйвера</summary>
        [RuName("Драйвер")]
        Driver,
        /// <summary>Логи клиента</summary>
        [RuName("Клиент")]
        Client,
        /// <summary>Логи сервера</summary>
        [RuName("Сервер")]
        Server,
        /// <summary>Логи производительности</summary>
        [RuName("Производительность")]
        Performance
    }

    /// <summary>
    /// Операция с multiple select элементом
    /// </summary>
    public enum MultiSelectOperation
    {
        /// <summary>Выбрать опцию по тексту</summary>
        [RuName("Выбрать по тексту")]
        SelectByText,
        /// <summary>Выбрать опцию по value</summary>
        [RuName("Выбрать по значению (value)")]
        SelectByValue,
        /// <summary>Выбрать опцию по индексу</summary>
        [RuName("Выбрать по индексу")]
        SelectByIndex,
        /// <summary>Снять выбор по тексту</summary>
        [RuName("Снять выбор по тексту")]
        DeselectByText,
        /// <summary>Снять выбор по value</summary>
        [RuName("Снять выбор по значению (value)")]
        DeselectByValue,
        /// <summary>Снять выбор по индексу</summary>
        [RuName("Снять выбор по индексу")]
        DeselectByIndex,
        /// <summary>Снять все выборы</summary>
        [RuName("Снять весь выбор")]
        DeselectAll,
        /// <summary>Получить все опции</summary>
        [RuName("Получить все опции")]
        GetAllOptions,
        /// <summary>Получить выбранные опции</summary>
        [RuName("Получить выбранные опции")]
        GetSelectedOptions
    }

    /// <summary>
    /// Операция с окном браузера
    /// </summary>
    public enum WindowOperation
    {
        /// <summary>Развернуть окно на весь экран</summary>
        [RuName("Развернуть")]
        Maximize,
        /// <summary>Свернуть окно</summary>
        [RuName("Свернуть")]
        Minimize,
        /// <summary>Полноэкранный режим (F11)</summary>
        [RuName("Полный экран")]
        FullScreen,
        /// <summary>Установить размер окна</summary>
        [RuName("Задать размер")]
        SetSize,
        /// <summary>Установить позицию окна</summary>
        [RuName("Задать позицию")]
        SetPosition,
        /// <summary>Получить текущий размер окна</summary>
        [RuName("Получить размер")]
        GetSize,
        /// <summary>Получить текущую позицию окна</summary>
        [RuName("Получить позицию")]
        GetPosition
    }

    /// <summary>
    /// Специальные клавиши для отправки в элемент
    /// </summary>
    public enum SpecialKeyType
    {
        /// <summary>Enter</summary>
        [RuName("Enter")]
        Enter,
        /// <summary>Tab</summary>
        [RuName("Tab")]
        Tab,
        /// <summary>Escape</summary>
        [RuName("Escape")]
        Escape,
        /// <summary>Backspace</summary>
        [RuName("Backspace")]
        Backspace,
        /// <summary>Delete</summary>
        [RuName("Delete")]
        Delete,
        /// <summary>Space (пробел)</summary>
        [RuName("Пробел")]
        Space,
        /// <summary>Стрелка вверх</summary>
        [RuName("Стрелка вверх")]
        ArrowUp,
        /// <summary>Стрелка вниз</summary>
        [RuName("Стрелка вниз")]
        ArrowDown,
        /// <summary>Стрелка влево</summary>
        [RuName("Стрелка влево")]
        ArrowLeft,
        /// <summary>Стрелка вправо</summary>
        [RuName("Стрелка вправо")]
        ArrowRight,
        /// <summary>Home</summary>
        [RuName("Home")]
        Home,
        /// <summary>End</summary>
        [RuName("End")]
        End,
        /// <summary>Page Up</summary>
        [RuName("Page Up")]
        PageUp,
        /// <summary>Page Down</summary>
        [RuName("Page Down")]
        PageDown,
        /// <summary>F1</summary>
        [RuName("F1")]
        F1,
        /// <summary>F2</summary>
        [RuName("F2")]
        F2,
        /// <summary>F3</summary>
        [RuName("F3")]
        F3,
        /// <summary>F4</summary>
        [RuName("F4")]
        F4,
        /// <summary>F5</summary>
        [RuName("F5")]
        F5,
        /// <summary>F6</summary>
        [RuName("F6")]
        F6,
        /// <summary>F7</summary>
        [RuName("F7")]
        F7,
        /// <summary>F8</summary>
        [RuName("F8")]
        F8,
        /// <summary>F9</summary>
        [RuName("F9")]
        F9,
        /// <summary>F10</summary>
        [RuName("F10")]
        F10,
        /// <summary>F11</summary>
        [RuName("F11")]
        F11,
        /// <summary>F12</summary>
        [RuName("F12")]
        F12,
        /// <summary>Ctrl+A (выделить всё)</summary>
        [RuName("Ctrl+A")]
        CtrlA,
        /// <summary>Ctrl+C (копировать)</summary>
        [RuName("Ctrl+C")]
        CtrlC,
        /// <summary>Ctrl+V (вставить)</summary>
        [RuName("Ctrl+V")]
        CtrlV,
        /// <summary>Ctrl+X (вырезать)</summary>
        [RuName("Ctrl+X")]
        CtrlX,
        /// <summary>Ctrl+Z (отменить)</summary>
        [RuName("Ctrl+Z")]
        CtrlZ,
        /// <summary>Shift+Tab</summary>
        [RuName("Shift+Tab")]
        ShiftTab,
        /// <summary>Alt+F4</summary>
        [RuName("Alt+F4")]
        AltF4
    }

    /// <summary>
    /// Режим выполнения клика по элементу.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>Обычный одиночный клик.</summary>
        [RuName("Одиночный клик")]
        Click,

        /// <summary>Двойной клик.</summary>
        [RuName("Двойной клик")]
        DoubleClick,

        /// <summary>Правый клик (контекстное меню).</summary>
        [RuName("Правый клик")]
        RightClick,

        /// <summary>Клик с удержанием кнопки мыши.</summary>
        [RuName("Клик с удержанием")]
        ClickAndHold
    }

    /// <summary>
    /// Режим поиска элементов на странице.
    /// </summary>
    public enum FindMode
    {
        /// <summary>Найти первый подходящий элемент и вернуть его ID.</summary>
        [RuName("Первый элемент")]
        FindOne,

        /// <summary>Найти все подходящие элементы и вернуть список их ID.</summary>
        [RuName("Все элементы")]
        FindAll
    }

    /// <summary>
    /// Режим наведения на элемент.
    /// </summary>
    public enum HoverMode
    {
        /// <summary>Навести на центр элемента.</summary>
        [RuName("Центр")]
        Center,

        /// <summary>Навести с указанием смещения от центра элемента.</summary>
        [RuName("Со смещением")]
        WithOffset
    }

    /// <summary>
    /// Режим ввода данных в элемент.
    /// </summary>
    public enum InputMode
    {
        /// <summary>Ввести текст (очистить поле и ввести новый текст).</summary>
        [RuName("Ввести текст")]
        TypeText,

        /// <summary>Отправить клавиши (включая специальные клавиши).</summary>
        [RuName("Отправить клавиши")]
        SendKeys,

        /// <summary>Очистить поле ввода.</summary>
        [RuName("Очистить")]
        Clear
    }

    /// <summary>
    /// Режим получения информации об элементе.
    /// </summary>
    public enum ElementInfoMode
    {
        /// <summary>Получить свойство элемента (атрибут, текст, value).</summary>
        [RuName("Свойство")]
        Property,

        /// <summary>Получить вычисленный CSS-стиль.</summary>
        [RuName("CSS-стиль")]
        ComputedStyle,

        /// <summary>Получить размеры и позицию элемента (Rectangle).</summary>
        [RuName("Размеры и позиция")]
        Rectangle
    }

    /// <summary>
    /// Режим навигации браузера.
    /// </summary>
    public enum NavigateMode
    {
        /// <summary>Перейти по URL.</summary>
        [RuName("Перейти по URL")]
        ToUrl,

        /// <summary>Назад в истории браузера.</summary>
        [RuName("Назад")]
        Back,

        /// <summary>Вперед в истории браузера.</summary>
        [RuName("Вперёд")]
        Forward,

        /// <summary>Обновить текущую страницу.</summary>
        [RuName("Обновить")]
        Refresh
    }

    /// <summary>
    /// Тип информации о браузере для получения.
    /// </summary>
    public enum BrowserInfoType
    {
        /// <summary>Заголовок страницы (Title).</summary>
        [RuName("Заголовок (Title)")]
        Title,

        /// <summary>Текущий URL страницы.</summary>
        [RuName("Текущий URL")]
        CurrentUrl,

        /// <summary>HTML-код страницы (PageSource).</summary>
        [RuName("HTML-код (PageSource)")]
        PageSource,

        /// <summary>Все три параметра сразу (Title, URL, PageSource).</summary>
        [RuName("Все")]
        All
    }

    /// <summary>
    /// Режимы ожидания элементов на странице.
    /// </summary>
    public enum ElementWaitMode
    {
        /// <summary>
        /// Ожидать только появления элемента в DOM (может быть невидимым).
        /// </summary>
        [RuName("Появление в DOM")]
        Present,

        /// <summary>
        /// Ожидать пока элемент станет видимым (Displayed = true).
        /// </summary>
        [RuName("Видимость")]
        Visible,

        /// <summary>
        /// Ожидать пока элемент станет кликабельным (Displayed = true и Enabled = true).
        /// </summary>
        [RuName("Кликабельность")]
        Clickable,

        /// <summary>
        /// Не ожидать - попытаться найти элемент немедленно.
        /// </summary>
        [RuName("Без ожидания")]
        None
    }

    /// <summary>
    /// Режимы поиска и проверки элементов на странице.
    /// </summary>
    public enum ElementSearchMode
    {
        /// <summary>
        /// Проверить существование элемента в DOM (может быть невидимым).
        /// Возвращает: ElementFound (bool)
        /// </summary>
        [RuName("Существует")]
        Exists,

        /// <summary>
        /// Проверить видимость элемента на странице.
        /// Возвращает: ElementFound (bool), IsVisible (bool)
        /// </summary>
        [RuName("Видимость")]
        IsVisible,

        /// <summary>
        /// Проверить кликабельность элемента (видим и активен).
        /// Возвращает: ElementFound (bool), IsVisible (bool), IsEnabled (bool), IsClickable (bool)
        /// </summary>
        [RuName("Кликабельность")]
        IsClickable,

        /// <summary>
        /// Ожидать элемент и проверить его полное состояние.
        /// Возвращает: ElementFound (bool), IsVisible (bool), IsEnabled (bool), IsSelected (bool)
        /// </summary>
        [RuName("Ждать и проверить")]
        WaitAndCheck,

        /// <summary>
        /// Найти все элементы по локатору.
        /// Возвращает: ElementsFound (List&lt;string&gt;), ElementCount (int)
        /// </summary>
        [RuName("Найти все")]
        FindAll,

        /// <summary>
        /// Подсчитать количество элементов по локатору.
        /// Возвращает: ElementCount (int)
        /// </summary>
        [RuName("Количество")]
        Count
    }
}
