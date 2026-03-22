# HTTP: OAuth2 токен

> **Группа:** MIA → HTTP / Web  
> **Класс:** `HttpOAuth2TokenBack`

## Назначение

Получает OAuth2 access token по одному из поддерживаемых grant type. Подходит для server-to-server интеграций, password flow и сценариев с authorization code.

## Входные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Token URL` | `string` | Endpoint выдачи токена. |
| `Grant Type` | `OAuth2GrantType` | `ClientCredentials`, `Password`, `AuthorizationCode`. |
| `Client ID` | `string` | Идентификатор клиента. |
| `Client Secret` | `string` | Секрет клиента. |
| `Scope` | `string` | Область доступа, если поддерживается провайдером. |
| `Username` | `string` | Только для `Password`. |
| `Password` | `string` | Только для `Password`. |
| `Code` | `string` | Код авторизации для `AuthorizationCode`. |
| `Redirect URI` | `string` | Redirect URI для `AuthorizationCode`. |

## Выходные параметры

| Параметр | Тип | Описание |
|---|---|---|
| `Access Token` | `string` | Токен доступа. |
| `Refresh Token` | `string` | Refresh token, если его вернул провайдер. |
| `Expires In` | `int` | Время жизни токена в секундах. |
| `Token Type` | `string` | Обычно `Bearer`. |

## Примечания

- `Password` flow считается устаревающим и обычно не рекомендуется для новых решений.
- Для обновления уже полученного токена используйте [06_HttpOAuth2Refresh.md](06_HttpOAuth2Refresh.md).
