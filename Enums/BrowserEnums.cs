namespace Primo.MIA
{
    /// <summary>
    /// Тип браузера для автоматизации
    /// </summary>
    public enum BrowserType
    {
        /// <summary>Google Chrome</summary>
        Chrome,
        /// <summary>Mozilla Firefox</summary>
        Firefox,
        /// <summary>Microsoft Edge</summary>
        Edge
    }

    /// <summary>
    /// Тип локатора для поиска элементов на странице
    /// </summary>
    public enum ElementLocatorType
    {
        /// <summary>Поиск по атрибуту id</summary>
        Id,
        /// <summary>Поиск по атрибуту name</summary>
        Name,
        /// <summary>Поиск по имени CSS-класса</summary>
        ClassName,
        /// <summary>Поиск по имени тега</summary>
        TagName,
        /// <summary>Поиск по полному тексту ссылки</summary>
        LinkText,
        /// <summary>Поиск по частичному тексту ссылки</summary>
        PartialLinkText,
        /// <summary>Поиск по CSS-селектору</summary>
        CssSelector,
        /// <summary>Поиск по XPath-выражению</summary>
        XPath
    }

    /// <summary>
    /// Тип условия ожидания
    /// </summary>
    public enum WaitConditionType
    {
        /// <summary>Элемент существует в DOM</summary>
        ElementExists,
        /// <summary>Элемент видим на странице</summary>
        ElementVisible,
        /// <summary>Элемент доступен для клика</summary>
        ElementClickable,
        /// <summary>Элемент невидим или отсутствует</summary>
        ElementInvisible,
        /// <summary>Текст присутствует в элементе</summary>
        TextPresent,
        /// <summary>Заголовок страницы содержит текст</summary>
        TitleContains,
        /// <summary>URL содержит текст</summary>
        UrlContains,
        /// <summary>Алерт присутствует</summary>
        AlertPresent
    }

    /// <summary>
    /// Режим выбора опции в select элементе
    /// </summary>
    public enum SelectMode
    {
        /// <summary>Выбор по видимому тексту</summary>
        ByText,
        /// <summary>Выбор по значению атрибута value</summary>
        ByValue,
        /// <summary>Выбор по индексу (начиная с 0)</summary>
        ByIndex
    }

    /// <summary>
    /// Выравнивание при прокрутке к элементу
    /// </summary>
    public enum ScrollAlignment
    {
        /// <summary>Прокрутить элемент к верхней части окна</summary>
        Top,
        /// <summary>Прокрутить элемент к центру окна</summary>
        Center,
        /// <summary>Прокрутить элемент к нижней части окна</summary>
        Bottom
    }

    /// <summary>
    /// Операция с cookies
    /// </summary>
    public enum CookieOperation
    {
        /// <summary>Получить cookie по имени</summary>
        Get,
        /// <summary>Получить все cookies</summary>
        GetAll,
        /// <summary>Установить cookie</summary>
        Set,
        /// <summary>Удалить cookie по имени</summary>
        Delete,
        /// <summary>Удалить все cookies</summary>
        DeleteAll
    }

    /// <summary>
    /// Тип переключения контекста браузера
    /// </summary>
    public enum SwitchToType
    {
        /// <summary>Переключиться на iframe по индексу или имени</summary>
        Frame,
        /// <summary>Переключиться на окно по handle</summary>
        Window,
        /// <summary>Переключиться на алерт</summary>
        Alert,
        /// <summary>Вернуться к основному контенту страницы</summary>
        DefaultContent,
        /// <summary>Вернуться к родительскому фрейму</summary>
        ParentFrame
    }

    /// <summary>
    /// Действие с алертом
    /// </summary>
    public enum AlertAction
    {
        /// <summary>Принять алерт (OK)</summary>
        Accept,
        /// <summary>Отклонить алерт (Cancel)</summary>
        Dismiss,
        /// <summary>Получить текст алерта</summary>
        GetText,
        /// <summary>Ввести текст в prompt</summary>
        SendKeys
    }

    /// <summary>
    /// Операция с вкладками браузера
    /// </summary>
    public enum TabOperation
    {
        /// <summary>Открыть новую вкладку</summary>
        OpenNewTab,
        /// <summary>Закрыть текущую вкладку</summary>
        CloseCurrentTab,
        /// <summary>Закрыть вкладку по handle</summary>
        CloseTabByHandle,
        /// <summary>Получить список всех handles</summary>
        GetAllHandles,
        /// <summary>Получить handle текущей вкладки</summary>
        GetCurrentHandle,
        /// <summary>Переключиться на вкладку по индексу</summary>
        SwitchToTab
    }

    /// <summary>
    /// Тип хранилища браузера
    /// </summary>
    public enum StorageType
    {
        /// <summary>localStorage — данные сохраняются между сессиями</summary>
        LocalStorage,
        /// <summary>sessionStorage — данные удаляются при закрытии вкладки</summary>
        SessionStorage
    }

    /// <summary>
    /// Операция с хранилищем браузера
    /// </summary>
    public enum StorageOperation
    {
        /// <summary>Получить значение по ключу</summary>
        GetItem,
        /// <summary>Установить значение</summary>
        SetItem,
        /// <summary>Удалить ключ</summary>
        RemoveItem,
        /// <summary>Очистить всё хранилище</summary>
        Clear,
        /// <summary>Получить все ключи</summary>
        GetAllKeys,
        /// <summary>Получить длину хранилища</summary>
        GetLength
    }

    /// <summary>
    /// Тип логов браузера
    /// </summary>
    public enum BrowserLogType
    {
        /// <summary>Логи браузера</summary>
        Browser,
        /// <summary>Логи драйвера</summary>
        Driver,
        /// <summary>Логи клиента</summary>
        Client,
        /// <summary>Логи сервера</summary>
        Server,
        /// <summary>Логи производительности</summary>
        Performance
    }

    /// <summary>
    /// Операция с multiple select элементом
    /// </summary>
    public enum MultiSelectOperation
    {
        /// <summary>Выбрать опцию по тексту</summary>
        SelectByText,
        /// <summary>Выбрать опцию по value</summary>
        SelectByValue,
        /// <summary>Выбрать опцию по индексу</summary>
        SelectByIndex,
        /// <summary>Снять выбор по тексту</summary>
        DeselectByText,
        /// <summary>Снять выбор по value</summary>
        DeselectByValue,
        /// <summary>Снять выбор по индексу</summary>
        DeselectByIndex,
        /// <summary>Снять все выборы</summary>
        DeselectAll,
        /// <summary>Получить все опции</summary>
        GetAllOptions,
        /// <summary>Получить выбранные опции</summary>
        GetSelectedOptions
    }

    /// <summary>
    /// Операция с окном браузера
    /// </summary>
    public enum WindowOperation
    {
        /// <summary>Развернуть окно на весь экран</summary>
        Maximize,
        /// <summary>Свернуть окно</summary>
        Minimize,
        /// <summary>Полноэкранный режим (F11)</summary>
        FullScreen,
        /// <summary>Установить размер окна</summary>
        SetSize,
        /// <summary>Установить позицию окна</summary>
        SetPosition,
        /// <summary>Получить текущий размер окна</summary>
        GetSize,
        /// <summary>Получить текущую позицию окна</summary>
        GetPosition
    }

    /// <summary>
    /// Специальные клавиши для отправки в элемент
    /// </summary>
    public enum SpecialKeyType
    {
        /// <summary>Enter</summary>
        Enter,
        /// <summary>Tab</summary>
        Tab,
        /// <summary>Escape</summary>
        Escape,
        /// <summary>Backspace</summary>
        Backspace,
        /// <summary>Delete</summary>
        Delete,
        /// <summary>Space (пробел)</summary>
        Space,
        /// <summary>Стрелка вверх</summary>
        ArrowUp,
        /// <summary>Стрелка вниз</summary>
        ArrowDown,
        /// <summary>Стрелка влево</summary>
        ArrowLeft,
        /// <summary>Стрелка вправо</summary>
        ArrowRight,
        /// <summary>Home</summary>
        Home,
        /// <summary>End</summary>
        End,
        /// <summary>Page Up</summary>
        PageUp,
        /// <summary>Page Down</summary>
        PageDown,
        /// <summary>F1</summary>
        F1,
        /// <summary>F2</summary>
        F2,
        /// <summary>F3</summary>
        F3,
        /// <summary>F4</summary>
        F4,
        /// <summary>F5</summary>
        F5,
        /// <summary>F6</summary>
        F6,
        /// <summary>F7</summary>
        F7,
        /// <summary>F8</summary>
        F8,
        /// <summary>F9</summary>
        F9,
        /// <summary>F10</summary>
        F10,
        /// <summary>F11</summary>
        F11,
        /// <summary>F12</summary>
        F12,
        /// <summary>Ctrl+A (выделить всё)</summary>
        CtrlA,
        /// <summary>Ctrl+C (копировать)</summary>
        CtrlC,
        /// <summary>Ctrl+V (вставить)</summary>
        CtrlV,
        /// <summary>Ctrl+X (вырезать)</summary>
        CtrlX,
        /// <summary>Ctrl+Z (отменить)</summary>
        CtrlZ,
        /// <summary>Shift+Tab</summary>
        ShiftTab,
        /// <summary>Alt+F4</summary>
        AltF4
    }

    /// <summary>
    /// Режим выполнения клика по элементу.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>Обычный одиночный клик.</summary>
        Click,

        /// <summary>Двойной клик.</summary>
        DoubleClick,

        /// <summary>Правый клик (контекстное меню).</summary>
        RightClick,

        /// <summary>Клик с удержанием кнопки мыши.</summary>
        ClickAndHold
    }

    /// <summary>
    /// Режим поиска элементов на странице.
    /// </summary>
    public enum FindMode
    {
        /// <summary>Найти первый подходящий элемент и вернуть его ID.</summary>
        FindOne,

        /// <summary>Найти все подходящие элементы и вернуть список их ID.</summary>
        FindAll
    }

    /// <summary>
    /// Режим наведения на элемент.
    /// </summary>
    public enum HoverMode
    {
        /// <summary>Навести на центр элемента.</summary>
        Center,

        /// <summary>Навести с указанием смещения от центра элемента.</summary>
        WithOffset
    }

    /// <summary>
    /// Режим ввода данных в элемент.
    /// </summary>
    public enum InputMode
    {
        /// <summary>Ввести текст (очистить поле и ввести новый текст).</summary>
        TypeText,

        /// <summary>Отправить клавиши (включая специальные клавиши).</summary>
        SendKeys,

        /// <summary>Очистить поле ввода.</summary>
        Clear
    }

    /// <summary>
    /// Режим получения информации об элементе.
    /// </summary>
    public enum ElementInfoMode
    {
        /// <summary>Получить свойство элемента (атрибут, текст, value).</summary>
        Property,

        /// <summary>Получить вычисленный CSS-стиль.</summary>
        ComputedStyle,

        /// <summary>Получить размеры и позицию элемента (Rectangle).</summary>
        Rectangle
    }

    /// <summary>
    /// Режим навигации браузера.
    /// </summary>
    public enum NavigateMode
    {
        /// <summary>Перейти по URL.</summary>
        ToUrl,

        /// <summary>Назад в истории браузера.</summary>
        Back,

        /// <summary>Вперед в истории браузера.</summary>
        Forward,

        /// <summary>Обновить текущую страницу.</summary>
        Refresh
    }

    /// <summary>
    /// Тип информации о браузере для получения.
    /// </summary>
    public enum BrowserInfoType
    {
        /// <summary>Заголовок страницы (Title).</summary>
        Title,

        /// <summary>Текущий URL страницы.</summary>
        CurrentUrl,

        /// <summary>HTML-код страницы (PageSource).</summary>
        PageSource,

        /// <summary>Все три параметра сразу (Title, URL, PageSource).</summary>
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
        Present,

        /// <summary>
        /// Ожидать пока элемент станет видимым (Displayed = true).
        /// </summary>
        Visible,

        /// <summary>
        /// Ожидать пока элемент станет кликабельным (Displayed = true и Enabled = true).
        /// </summary>
        Clickable,

        /// <summary>
        /// Не ожидать - попытаться найти элемент немедленно.
        /// </summary>
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
        Exists,

        /// <summary>
        /// Проверить видимость элемента на странице.
        /// Возвращает: ElementFound (bool), IsVisible (bool)
        /// </summary>
        IsVisible,

        /// <summary>
        /// Проверить кликабельность элемента (видим и активен).
        /// Возвращает: ElementFound (bool), IsVisible (bool), IsEnabled (bool), IsClickable (bool)
        /// </summary>
        IsClickable,

        /// <summary>
        /// Ожидать элемент и проверить его полное состояние.
        /// Возвращает: ElementFound (bool), IsVisible (bool), IsEnabled (bool), IsSelected (bool)
        /// </summary>
        WaitAndCheck,

        /// <summary>
        /// Найти все элементы по локатору.
        /// Возвращает: ElementsFound (List&lt;string&gt;), ElementCount (int)
        /// </summary>
        FindAll,

        /// <summary>
        /// Подсчитать количество элементов по локатору.
        /// Возвращает: ElementCount (int)
        /// </summary>
        Count
    }
}
