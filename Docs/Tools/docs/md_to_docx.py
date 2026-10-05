#!/usr/bin/env python3
"""Convierte los documentos Markdown de evidencia a DOCX (y luego PDF).

Uso:
    python3 md_to_docx.py [carpeta_con_md]

Sin argumento convierte ``Docs/Evidencias`` del repositorio. Soporta imágenes
con la sintaxis ``![texto](ruta/relativa.png)`` (rutas relativas al .md) y
bloques de código ``` ``` ```.
"""
import os
import re
import glob
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH

DOCS_DIR = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Evidencias"))
IMG_RE = re.compile(r"^!\[(.*?)\]\((.+?)\)\s*$")

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

        # Bloques de código
        if stripped.startswith("```"):
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("```"):
                p = doc.add_paragraph()
                run = p.add_run(lines[i])
                run.font.name = "Consolas"
                run.font.size = Pt(9)
                i += 1
            i += 1
            continue

        # Imágenes
        image = IMG_RE.match(stripped)
        if image:
            alt, rel = image.group(1), image.group(2)
            img_path = os.path.normpath(os.path.join(os.path.dirname(path), rel))
            if os.path.exists(img_path):
                doc.add_picture(img_path, width=Inches(6.0))
                caption = doc.add_paragraph()
                run = caption.add_run(alt)
                run.italic = True
                run.font.size = Pt(9)
            else:
                doc.add_paragraph(f"[Imagen no encontrada: {rel}]")
            i += 1
            continue

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
                            paragraph = table.rows[r].cells[c].paragraphs[0]
                            add_runs(paragraph, cell)
                            for run in paragraph.runs:
                                run.font.size = Pt(9)
                                if r == 0:
                                    run.bold = True
            continue

        if stripped.startswith("### "):
            doc.add_heading(stripped[4:], level=3)
        elif stripped.startswith("## "):
            doc.add_heading(stripped[3:], level=2)
        elif stripped.startswith("# "):
            doc.add_heading(stripped[2:], level=1)
        elif stripped.startswith("> "):
            p = doc.add_paragraph()
            add_runs(p, stripped[2:])
            for run in p.runs:
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
    import sys
    folder = sys.argv[1] if len(sys.argv) > 1 else DOCS_DIR
    for path in sorted(glob.glob(os.path.join(folder, "*.md"))):
        out = convert(path)
        print(f"DOCX generado: {out}")


if __name__ == "__main__":
    main()
