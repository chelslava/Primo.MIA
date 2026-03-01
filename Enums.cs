using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primo.MIA
{
    // =========================================================================
    // ПЕРЕЧИСЛЕНИЯ
    // =========================================================================

    /// <summary>
    /// Режим чтения TOML-конфигурации
    /// </summary>
    public enum TomlReadMode
    {
        /// <summary>
        /// Прочитать одно значение по ключу.
        /// Поддерживает вложенность через точку: "section.subsection.key"
        /// </summary>
        SingleValue,

        /// <summary>
        /// Прочитать все ключи указанной секции в Dictionary&lt;string, string&gt;.
        /// Поддерживает вложенные секции: "app.database"
        /// </summary>
        SectionToDictionary,

        /// <summary>
        /// Прочитать весь файл в плоский словарь.
        /// Ключи вложенных секций объединяются через точку: "server.host"
        /// </summary>
        FullFileToDictionary,

        /// <summary>
        /// Чтение конфига с профилями окружений.
        /// Мёржит секцию [default] с секцией выбранного профиля ([production], [staging] и т.д.)
        /// по выбранной стратегии слияния.
        /// </summary>
        ReadProfile
    }

    /// <summary>
    /// Стратегия слияния профиля с секцией default
    /// </summary>
    public enum ProfileMergeStrategy
    {
        /// <summary>
        /// Стандартная стратегия: default как база, профиль перекрывает его ключи.
        /// Ключи из default, которых нет в профиле, сохраняются.
        /// Пример: default.timeout=30, production.timeout=60 → итог: timeout=60
        ///         default.retry=3 (нет в production) → итог: retry=3
        /// </summary>
        DefaultThenProfile,

        /// <summary>
        /// Только секция профиля, без слияния с default.
        /// Используется когда профили полностью самодостаточны.
        /// </summary>
        ProfileOnly,

        /// <summary>
        /// Обратный порядок: профиль как база, default добавляет только недостающие ключи.
        /// Ключи профиля никогда не перекрываются default.
        /// </summary>
        ProfileThenDefault
    }
}
