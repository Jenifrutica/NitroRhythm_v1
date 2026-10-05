#!/usr/bin/env python3
"""Genera Docs/Evidencias/RESULTADO_PRUEBAS.md desde los XML de Unity Test Framework.

Uso (desde la raíz del proyecto):
    python3 Docs/Tools/docs/summarize_tests.py
"""
import datetime
import os
import xml.etree.ElementTree as ET

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Docs", "Evidencias", "RESULTADO_PRUEBAS.md")
SETS = (("PlayMode", "test_results_playmode.xml"), ("EditMode", "test_results_editmode.xml"))

ICON = {"Passed": "OK", "Failed": "FALLO", "Skipped": "Omitida", "Inconclusive": "Inconcluso"}


def main():
    lines = [
        "# Resultado de las pruebas automáticas — NitroRhythm",
        "",
        f"Generado: {datetime.date.today().isoformat()} · Unity 6000.5.8f1 · Unity Test Framework 1.7.0",
        "",
        "> Archivo generado por `Docs/Tools/docs/summarize_tests.py` a partir de",
        "> `Logs/test_results_*.xml`. Las pruebas \"Omitida\" son generadores de capturas que",
        "> solo corren con la variable `NITRO_SHOT_DIR` definida.",
        "",
    ]
    totals = {"passed": 0, "failed": 0, "skipped": 0, "total": 0}
    for label, filename in SETS:
        path = os.path.join(ROOT, "Logs", filename)
        root = ET.parse(path).getroot()
        for key in totals:
            totals[key] += int(root.attrib.get(key, 0))
        lines += [
            f"## {label}",
            "",
            f"Total {root.attrib.get('total')} · aprobadas {root.attrib.get('passed')} · "
            f"fallidas {root.attrib.get('failed')} · omitidas {root.attrib.get('skipped')}",
            "",
            "| Clase | Prueba | Resultado | Duración (s) |",
            "| :-- | :-- | :-- | :-- |",
        ]
        for case in root.iter("test-case"):
            full = case.get("fullname", "")
            cls, _, name = full.rpartition(".")
            cls = cls.split(".")[-1]
            duration = float(case.get("duration", 0) or 0)
            lines.append(f"| {cls} | {name} | {ICON.get(case.get('result'), case.get('result'))} | {duration:.2f} |")
        lines.append("")
    lines += [
        "## Resumen global",
        "",
        f"**{totals['passed']} aprobadas, {totals['failed']} fallidas, {totals['skipped']} omitidas "
        f"de {totals['total']}.** Línea base anterior a esta ronda de ajustes: 14 pruebas.",
        "",
        "### Cómo reproducirlo",
        "",
        "```bash",
        'UNITY=/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity',
        'flatpak run --command="$UNITY" com.unity.UnityHub -batchmode -nographics \\',
        '  -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults Logs/test_results_playmode.xml',
        'flatpak run --command="$UNITY" com.unity.UnityHub -batchmode -nographics \\',
        '  -projectPath "$PWD" -runTests -testPlatform EditMode -testResults Logs/test_results_editmode.xml',
        "```",
        "",
    ]
    with open(OUT, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines))
    print(f"Escrito {OUT}")


if __name__ == "__main__":
    main()
