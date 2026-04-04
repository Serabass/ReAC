# REaC — Reverse Engineering as Code

Консольный инструмент на **.NET 8** (C#): база знаний в текстовых **`project.toml`**, **`*.re`** (типы/таргеты/модули) и **`*.rdoc`** (документы), статический HTML-экспорт, валидация layout. **Игра или платформа не зашиты в инструмент** — в репозитории лежит лишь **иллюстративный** пример KB (имена типов и адреса можно заменить на свои).

## Источник правды (Source of truth)

Канон — **`project.toml`** и деревья **`types/`**, **`modules/`**, **`targets/`**, **`docs/`** (`*.re`, `*.rdoc`). Всё хранится в Git как текст.

Каталог **`generated/`** (в т.ч. **`generated/html`**) — **только производный** вывод; не редактировать как источник; в CI и локально его можно пересобрать командой `export-html` / `build`.

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download) **или** Docker с образом `mcr.microsoft.com/dotnet/sdk:8.0`.

## Сборка и тесты

```powershell
dotnet build
dotnet test
```

Через Docker Compose:

```powershell
docker compose run --rm reac dotnet test
docker compose run --rm reac dotnet run --project src/Reac -- validate
docker compose run --rm reac dotnet run --project src/Reac -- export-html
```

## CLI

Из корня репозитория:

| Команда | Описание |
|--------|----------|
| `dotnet run --project src/Reac -- init [path]` | Каркас KB: `project.toml`, каталоги, примеры `.re`/`.rdoc` |
| `dotnet run --project src/Reac -- validate` | Загрузка проекта, проверки + предупреждения |
| `dotnet run --project src/Reac -- export-html [--out dir]` | Статический сайт (по умолчанию `generated/html`) |
| `dotnet run --project src/Reac -- build` | `validate` + `export-html` |

Глобальная опция: `-p` / `--project` — корень проекта (где лежит `project.toml`).

## DSL (кратко)

- **`.re`**: `target`, `module`, `class`/`struct`, поля `0xOFFSET name : type`, наследование, `pointer_size_bytes` в target. Строка **`source "url"`** может повторяться (несколько источников — порядок сохраняется).
- **Нативные функции** (точки входа в бинарнике, на размер структуры не влияют): `fn 0xADDR Name(paramTypes...) [: ReturnType]`; краткий комментарий в той же строке — через `//`; развёрнутое описание — `note fn Name "..."` (имя должно совпадать с именем в строке `fn`).
- **`size` в типе**: можно указать явно (`class Foo : Bar size 0x120 { ... }`) или **опустить** — тогда итоговый размер **выводится** из цепочки полей (max offset + span), если все вложенные типы и скаляры разрешимы. Используйте явный `size`, если нужен канон при неполной таблице полей.
- **`.rdoc`**: `document Id { title, summary, references { ref Name }, section ... }`.
- Типы полей парсятся в **AST** (`TypeExpr`: скаляры, именованные, `*`, массивы `[n]`).

Статический HTML для производного типа: блок **Ancestor types** — для каждого предка сворачиваемый `<details>` (по умолчанию закрыт) с таблицей own fields и ссылкой на страницу предка.

## Ограничения MVP

- Глобальная уникальность имён `class`/`struct`.
- Размер указателя для layout берётся из **`pointer_size_bytes`** активного target (для примера `example_win32` в каркасе — `4`).

## Лицензия

По желанию репозитория; уточните в корне проекта при публикации.
