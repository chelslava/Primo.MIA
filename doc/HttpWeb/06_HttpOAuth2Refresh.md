# HTTP: OAuth2 обновление токена

> **Группа:** MIA → HTTP / Web  
> **Класс:** `HttpOAuth2RefreshBack`

## Назначение

Обновляет OAuth2 access token по `refresh_token` без повторной полной авторизации пользователя.

## Входные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Token URL` | `string` | Endpoint обновления токена. |
| `Client ID` | `string` | Идентификатор клиента. |
| `Client Secret` | `string` | Секрет клиента. |
| `Refresh Token` | `string` | Текущий refresh token. |
| `Игнорировать ошибки SSL` | `bool` | Для тестовых сред. |

## Выходные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Access Token` | `string` | Новый access token. |
| `New Refresh Token` | `string` | Новый refresh token, если провайдер его вернул. |
| `Expires In` | `int` | Новое время жизни токена. |

## Примечания

- Некоторые провайдеры возвращают новый `refresh_token`, а некоторые оставляют старый действующим.
- Если refresh token истёк или отозван, активность завершится ошибкой HTTP.
