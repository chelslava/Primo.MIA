# -*- coding: utf-8 -*-
"""Миграция комбобоксов на EnumValuesExtension + конвертер заголовков (issue #14). v2

Для каждого *.xaml с ObjectDataProvider(Enum.GetValues):
  1. Удаляет блок ObjectDataProvider, запоминает KEY -> TYPE.
  2. Любой ComboBox с ItemsSource="{Binding Source={StaticResource KEY}}" и
     SelectedItem="{B}": -> ItemsSource="{P:EnumValues TYPE}", SelectedValue="{B}",
     SelectedValuePath="Key", DisplayMemberPath="Display" (порядок остальных
     атрибутов сохраняется).
  3. Run Text="{Binding Prop_X, Mode=OneWay}" (+ любые доп. атрибуты) —
     добавляет Converter={StaticResource EnumDisplayNameConverter}.

Запуск: python migrate_combos.py [--apply]
Без --apply — только отчёт; откат при невалидном XML.
"""
import re
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(r"D:\Repo\Primo.MIA")
MODULES = ["Browser", "HttpWeb", "Json", "Xml", "Database", "Sftp", "Mq", "Crypto",
           "Calendar", "Utilities", "Files", "List", "Dictionary", "Tuple", "Text"]

ODP_RE = re.compile(
    r"[ \t]*<ObjectDataProvider\s+x:Key=\"(?P<key>\w+)\"\s+MethodName=\"GetValues\"\s+"
    r"ObjectType=\"\{x:Type system:Enum\}\">\s*"
    r"<ObjectDataProvider\.MethodParameters>\s*"
    r"<x:Type\s+TypeName=\"(?P<type>[^\"]+)\"/>\s*"
    r"</ObjectDataProvider\.MethodParameters>\s*"
    r"</ObjectDataProvider>\r?\n?",
    re.S)

COMBO_RE = re.compile(r"<ComboBox\b(?P<attrs>[^>]*?)(?P<close>/?)>", re.S)
ITEMSRC_RE = re.compile(r'\s*ItemsSource="\{Binding Source=\{StaticResource (\w+)\}\}"')
SELECTED_RE = re.compile(r'\s*SelectedItem="([^"]+)"')
RUN_RE = re.compile(
    r"(<Run\s+Text=\"\{Binding\s+(?:Path=)?(Prop_\w+),\s*Mode=OneWay\})\""
    r"(?P<rest>[^>]*?)/>")


def find_prefix(text):
    m = re.search(r'xmlns:(\w+)="clr-namespace:Primo\.MIA(?:;assembly=[^"]+)?["/]', text)
    return m.group(1) if m else None


def migrate(path, apply):
    src = path.read_text(encoding="utf-8-sig")
    actions = {"odp": 0, "combo": 0, "run": 0, "prefix": False, "errors": []}
    text = src

    keys = {}
    def odp_sub(m):
        actions["odp"] += 1
        keys[m.group("key")] = m.group("type")
        return ""
    text = ODP_RE.sub(odp_sub, text)

    if not keys:
        return None

    prefix = find_prefix(text)
    if not prefix:
        text = text.replace(
            "<UserControl x:Class=",
            "<UserControl xmlns:local=\"clr-namespace:Primo.MIA\" x:Class=", 1)
        prefix = "local"
        actions["prefix"] = True

    unresolved = set()

    def combo_sub(m):
        attrs, close = m.group("attrs") or "", m.group("close") or ""
        m1 = ITEMSRC_RE.search(attrs)
        m2 = SELECTED_RE.search(attrs)
        if not m1:
            return m.group(0)
        key = m1.group(1)
        if key not in keys:
            unresolved.add(key)
            return m.group(0)
        if not m2:
            actions["errors"].append(f"combo без SelectedItem: {key}")
            return m.group(0)
        etype = keys[key]
        if ":" not in etype:
            etype = f"{prefix}:{etype}"
        sel = m2.group(1)
        new_attrs = ITEMSRC_RE.sub("", attrs, count=1)
        new_attrs = SELECTED_RE.sub("", new_attrs, count=1)
        # Пробел между последним атрибутом и '/>' не нужен
        new_attrs = re.sub(r'\s+$', '', new_attrs, count=1)
        sep = "" if new_attrs.startswith(" ") or not new_attrs else " "
        actions["combo"] += 1
        return (f"<ComboBox ItemsSource=\"{{{prefix}:EnumValues {etype}}}\" "
                f"SelectedValue=\"{sel}\" SelectedValuePath=\"Key\" "
                f"DisplayMemberPath=\"Display\"{sep}{new_attrs}{'/>' if close == '/' else '>'}")

    text = COMBO_RE.sub(combo_sub, text)

    if unresolved:
        actions["errors"].append(f"Неразрешённые ключи: {sorted(unresolved)}")

    def run_sub(m):
        run_open, prop, rest = m.group(1), m.group(2), m.group("rest") or ""
        actions["run"] += 1
        return run_open + ", Converter={StaticResource EnumDisplayNameConverter}}\"" + rest + "/>"

    text = RUN_RE.sub(run_sub, text)

    if apply:
        path.write_text(text, encoding="utf-8-sig")
        try:
            ET.fromstring(text.encode("utf-8"))
        except ET.ParseError as e:
            path.write_text(src, encoding="utf-8-sig")
            actions["errors"].append(f"XML invalid: {e} — откат")
    return actions


def main():
    apply = "--apply" in sys.argv
    total = {"odp": 0, "combo": 0, "run": 0, "prefix": 0, "files": 0, "rollback": 0}
    for mod in MODULES:
        for path in sorted((ROOT / mod).rglob("*.xaml")):
            res = migrate(path, apply)
            if res is None:
                continue
            total["files"] += 1
            total["odp"] += res["odp"]
            total["combo"] += res["combo"]
            total["run"] += res["run"]
            total["prefix"] += 1 if res["prefix"] else 0
            status = "; ".join(res["errors"]) if res["errors"] else "OK"
            if res["errors"]:
                total["rollback"] += 1
            print(f"{path.relative_to(ROOT)}: odp={res['odp']} combo={res['combo']} "
                  f"run={res['run']} prefix_added={res['prefix']} -> {status}")
    print(f"\nИТОГО: файлов={total['files']} odp={total['odp']} combo={total['combo']} "
          f"run={total['run']} prefix_added={total['prefix']} откатов={total['rollback']}")


if __name__ == "__main__":
    main()
