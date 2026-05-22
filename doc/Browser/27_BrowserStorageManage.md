# Браузер: Управление Storage

> **Группа:** MIA → Браузер  
> **Класс:** `BrowserStorageManageBack`  
> **Тип результата:** `void` (данные в выходной переменной)

## Назначение

Управляет localStorage и sessionStorage браузера: получение, установка, удаление данных.

---

## Параметры

### Входные

| Параметр | Тип | Обязателен | Описание |
|---|---|---|---|
| **ID сессии** | `string` | Да | Идентификатор сессии браузера |
| **Тип storage** | `StorageType` | Да | LocalStorage или SessionStorage |
| **Операция** | `StorageOperation` | Да | Get, Set, Remove, Clear |
| **Ключ** | `string` | Для Get/Set/Remove | Ключ данных |
| **Значение** | `string` | Для Set | Значение для сохранения |

### Типы Storage

| Тип | Описание |
|---|---|
| `LocalStorage` | Постоянное хранилище |
| `SessionStorage` | Хранилище сессии |

### Операции

| Операция | Описание |
|---|---|
| `Get` | Получить значение по ключу |
| `Set` | Установить значение |
| `Remove` | Удалить ключ |
| `Clear` | Очистить все данные |

### Выходные

| Параметр | Тип | Описание |
|---|---|---|
| **Результат** | `string` | Полученное значение (для Get) |

---

## Примеры использования

**Получение из localStorage:**
```
Тип storage: LocalStorage
Операция: Get
Ключ: "user_token"
Результат: token
```

**Сохранение в sessionStorage:**
```
Тип storage: SessionStorage
Операция: Set
Ключ: "temp_data"
Значение: "value123"
```

**Удаление ключа:**
```
Тип storage: LocalStorage
Операция: Remove
Ключ: "old_data"
```

**Очистка storage:**
```
Тип storage: LocalStorage
Операция: Clear
```
