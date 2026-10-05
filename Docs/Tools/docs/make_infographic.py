#!/usr/bin/env python3
"""Genera la infografía comparativa (GA5-AA4-EV01) como PPTX para importar en Canva.

Crea una infografía vertical con las secciones exigidas (estructura e interfaces,
concepto gráfico, niveles y mecánicas) y pares de imágenes antes/después.
"""
import os
from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from PIL import Image

ROOT = "/home/jenifrutica/SENA/NitroRythm/My project/Docs"
CAP = os.path.join(ROOT, "Capturas")
OUT = os.path.join(ROOT, "Evidencias", "GA5-AA4-EV01_Infografia.pptx")

BG = RGBColor(0x0B, 0x0B, 0x14)
CYAN = RGBColor(0x33, 0xCC, 0xFF)
GOLD = RGBColor(0xFB, 0xBF, 0x24)
RED = RGBColor(0xFF, 0x44, 0x44)
WHITE = RGBColor(0xF2, 0xF2, 0xF7)
GREY = RGBColor(0x9A, 0x9A, 0xB0)

prs = Presentation()
prs.slide_width = Inches(7.5)
prs.slide_height = Inches(16.67)
blank = prs.slide_layouts[6]

slide = prs.slides.add_slide(blank)


def background(slide, color=BG):
    from pptx.enum.shapes import MSO_SHAPE
    shape = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height)
    shape.fill.solid()
    shape.fill.fore_color.rgb = color
    shape.line.fill.background()
    shape.shadow.inherit = False
    slide.shapes._spTree.remove(shape._element)
    slide.shapes._spTree.insert(2, shape._element)


def text(slide, x, y, w, h, runs, align=PP_ALIGN.LEFT, size=14, color=WHITE, bold=False, spacing=1.0):
    box = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = box.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = MSO_ANCHOR.TOP
    first = True
    for item in runs:
        p = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False
        p.alignment = align
        p.line_spacing = spacing
        if isinstance(item, tuple):
            content, c, b, s = item
        else:
            content, c, b, s = item, color, bold, size
        r = p.add_run()
        r.text = content
        r.font.size = Pt(s)
        r.font.bold = b
        r.font.color.rgb = c
        r.font.name = "Arial"
    return box


def fit_image(slide, path, x, y, max_w, max_h):
    if not os.path.exists(path):
        return None
    with Image.open(path) as im:
        iw, ih = im.size
    ratio = min(max_w / iw, max_h / ih)
    w = iw * ratio
    h = ih * ratio
    left = Inches(x + (max_w - w) / 2)
    top = Inches(y + (max_h - h) / 2)
    return slide.shapes.add_picture(path, left, top, Inches(w), Inches(h))


def section(title, before_text, after_text, before_img, after_img, y):
    text(slide, 0.4, y, 6.7, 0.4, [(title, CYAN, True, 20)])
    text(slide, 0.4, y + 0.42, 3.2, 1.0, [("ANTES", RED, True, 12), (before_text, GREY, False, 10)])
    text(slide, 3.9, y + 0.42, 3.2, 1.0, [("DESPUÉS", CYAN, True, 12), (after_text, WHITE, False, 10)])
    img_y = y + 1.15
    fit_image(slide, before_img, 0.4, img_y, 3.2, 2.2)
    fit_image(slide, after_img, 3.9, img_y, 3.2, 2.2)
    return img_y + 2.35


background(slide)

text(slide, 0.4, 0.4, 6.7, 0.7, [("NITRO RHYTHM", CYAN, True, 40)], align=PP_ALIGN.CENTER)
text(slide, 0.4, 1.1, 6.7, 0.5,
     [("Infografía comparativa — antes / después de los ajustes", WHITE, False, 16)], align=PP_ALIGN.CENTER)
text(slide, 0.4, 1.6, 6.7, 0.4,
     [("GA5-220501087-AA4-EV01 · SENA · Jenifer Daniela Urbano Córdoba", GOLD, False, 11)], align=PP_ALIGN.CENTER)

y = 2.2
y = section(
    "1 · Estructura e interfaces",
    "Prototipo inicial: una sola escena con menú y HUD en texto plano, sin navegación.",
    "Cuatro escenas (menú, selección, gameplay, resultados) en Canvas + TextMeshPro, con HUD ampliado.",
    os.path.join(CAP, "Antes/legacy/legacy_menu.png"),
    os.path.join(CAP, "Despues/ronda2/ui/ui_menu_principal.png"),
    y)

y = section(
    "2 · Concepto gráfico",
    "Primitivas cúbicas de color plano; sin garaje 3D ni audio reactivo.",
    "Karts y pilotos 3D texturizados, garaje 3D y estética neón URP.",
    os.path.join(CAP, "Antes/legacy/legacy_select.png"),
    os.path.join(CAP, "Despues/ronda3/seleccion_personajes.png"),
    y)

y = section(
    "3 · Niveles",
    "Pista de un solo estilo: plataformas grises de cubos, sin temática.",
    "7 dominios jugables con cielo, props, partículas y música propios; checkpoints visibles.",
    os.path.join(CAP, "Antes/legacy/legacy_track.png"),
    os.path.join(CAP, "Despues/ronda2/RESUMEN_dominios.png"),
    y)

y = section(
    "4 · Mecánicas",
    "Karts y villano de cubos; movimiento básico y sin combate.",
    "4 modos, villano que ataca al compás y K.O. con cuenta regresiva y feedback de impacto.",
    os.path.join(CAP, "Antes/legacy/legacy_gameplay.png"),
    os.path.join(CAP, "Despues/ronda3/gameplay_ko_cuenta_regresiva.png"),
    y)

text(slide, 0.4, y + 0.1, 6.7, 1.2,
     [("Hallazgos P4 (HUD), P6 (impacto) y P9 (progresión) atendidos y verificados con 40 pruebas automáticas.",
       WHITE, False, 12),
      ("El estado «antes» se reconstruye a partir del prototipo inicial (primitivas, sin garaje 3D).",
       GREY, False, 10),
      ("Colores del juego: cian #33CCFF · dorado #FBBF24 · rojo #FF0000", GREY, False, 10)],
     align=PP_ALIGN.CENTER)

prs.save(OUT)
print("PPTX generado:", OUT)
