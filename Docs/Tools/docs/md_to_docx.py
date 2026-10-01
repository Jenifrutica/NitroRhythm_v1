#!/usr/bin/env python3
"""Convierte los documentos Markdown de evidencia a DOCX (y luego PDF).

Uso:
    python3 md_to_docx.py
"""
import os
import re
import glob
from docx import Document
from docx.shared import Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH

DOCS_DIR = "/home/jenifrutica/SENA/NitroRythm/Evidencias/Documentos"

ROW_RE = re.compile(r"^\|(.+)\|\s*$")


def add_runs(paragraph, text):
    # Soporte básico de **negrita** y `código`.
    parts = re.split(r"(\*\*.+?\*\*|`.+?`)", text)
    for part in parts:
        if not part:
            continue
        if part.startswith("**") and part.endswith("**"):
            run = paragraph.add_run(part[2:-2])
            run.bold = True
        elif part.startswith("`") and part.endswith("`"):
            run = paragraph.add_run(part[1:-1])
            run.font.name = "Consolas"
        else:
            paragraph.add_run(part)


def convert(path):
    with open(path, encoding="utf-8") as handle:
        lines = handle.read().splitlines()

    doc = Document()
    style = doc.styles["Normal"]
    style.font.name = "Calibri"
    style.font.size = Pt(11)

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        # Tablas
        if ROW_RE.match(stripped):
            block = []
            while i < len(lines) and ROW_RE.match(lines[i].strip()):
                block.append(lines[i].strip())
                i += 1
            rows = []
            for row in block:
                if re.match(r"^\|[\s\-:|]+\|$", row):
                    continue
                cells = [c.strip() for c in row.strip("|").split("|")]
                rows.append(cells)
            if rows:
                table = doc.add_table(rows=len(rows), cols=len(rows[0]))
                table.style = "Light Grid Accent 1"
                for r, cells in enumerate(rows):
                    for c, cell in enumerate(cells):
                        if c < len(table.rows[r].cells):
                            table.rows[r].cells[c].text = cell
            continue

        if stripped.startswith("### "):
            doc.add_heading(stripped[4:], level=3)
        elif stripped.startswith("## "):
            doc.add_heading(stripped[3:], level=2)
        elif stripped.startswith("# "):
            doc.add_heading(stripped[2:], level=1)
        elif stripped.startswith("> "):
            p = doc.add_paragraph()
            run = p.add_run(stripped[2:])
            run.italic = True
            run.font.color.rgb = RGBColor(0x55, 0x55, 0x55)
        elif re.match(r"^[-*] ", stripped):
            p = doc.add_paragraph(style="List Bullet")
            add_runs(p, stripped[2:])
        elif re.match(r"^\d+\. ", stripped):
            p = doc.add_paragraph(style="List Number")
            add_runs(p, re.sub(r"^\d+\. ", "", stripped))
        elif stripped == "---":
            doc.add_paragraph("")
        elif stripped == "":
            pass
        else:
            p = doc.add_paragraph()
            add_runs(p, stripped)
        i += 1

    out = os.path.splitext(path)[0] + ".docx"
    doc.save(out)
    return out


def main():
    for path in sorted(glob.glob(os.path.join(DOCS_DIR, "*.md"))):
        out = convert(path)
        print(f"DOCX generado: {out}")


if __name__ == "__main__":
    main()
