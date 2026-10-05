#!/usr/bin/env python3
"""Genera la analítica de la prueba de funcionalidad (GA5-AA1-EV01).

Produce:
- respuestas.csv            (formato de exportación de Google Forms)
- reporte_google_forms.md   (resumen por pregunta como el de Forms)
- analitica_preguntas.png   (promedio por pregunta)
- analitica_usuarios.png    (promedio por usuario)
- distribucion_respuestas.png (distribución de la escala 1-5)
- resumen_analitica.md

Los 5 registros son de EJEMPLO para dejar el flujo completo; la usuaria los
reemplaza por las respuestas reales y vuelve a ejecutar este script.
"""
import csv
import os
from collections import Counter
from statistics import mean

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

OUT = "/home/jenifrutica/SENA/NitroRythm/My project/Docs/Analitica"
FORMS = "/home/jenifrutica/SENA/NitroRythm/My project/Docs/Forms_Google"
os.makedirs(OUT, exist_ok=True)
os.makedirs(FORMS, exist_ok=True)

QUESTIONS = [
    "La estructura general del juego (menú → selección → carrera → resultados) es clara y fácil de entender.",
    "La navegación entre pantallas es coherente y siempre sé cómo volver al menú principal.",
    "Las interfaces (menú principal, selección de piloto, HUD y pausa) se entienden a simple vista.",
    "El HUD muestra de forma útil la información necesaria (velocímetro, progreso y puntuación).",
    "Los controles del kart (WASD / Flechas / IJKL) responden de manera rápida y predecible.",
    "Las acciones del juego (salto, impulsos y obstáculos) reaccionan de forma consistente a lo que hago.",
    "La estética neón y las animaciones transmiten bien la identidad del juego.",
    "La música y su relación con la pista (generación reactiva al audio) aportan a la experiencia.",
    "Los niveles tienen una progresión de dificultad adecuada y los checkpoints evitan frustración.",
    "Las mecánicas principales (carrera, persecución del villano, combate y modos multijugador) funcionan correctamente.",
]
ASPECTS = ["Estructura", "Estructura", "Interfaces", "Interfaces", "Interactividad",
           "Interactividad", "Concepto gráfico", "Concepto gráfico", "Niveles", "Mecánicas"]

# 5 registros de ejemplo (marcados como 'real' una vez diligenciados por la usuaria).
USERS = [
    ("Laura Martínez", "2026-10-05 09:12:04", [5, 5, 4, 4, 4, 4, 5, 5, 4, 5],
     "Muy entretenido; el cooperativo local es lo mejor. Me gustó que la pista siga la música."),
    ("Andrés Pérez", "2026-10-05 10:03:41", [4, 4, 4, 3, 4, 3, 4, 4, 3, 4],
     "El HUD podría dar más información y el primer nivel se siente algo difícil."),
    ("Sofía Ramírez", "2026-10-05 11:27:18", [5, 4, 5, 5, 5, 5, 5, 4, 4, 5],
     "La estética y la música me encantaron; los karts 3D se ven muy bien."),
    ("Camilo Torres", "2026-10-05 14:45:52", [4, 5, 4, 4, 4, 4, 5, 5, 4, 4],
     "El kart responde bien; a veces la cámara se aleja en las curvas."),
    ("Valentina Gómez", "2026-10-05 16:08:33", [5, 4, 4, 3, 4, 4, 4, 4, 3, 4],
     "Fácil de entender. Subiría la dificultad de forma más gradual."),
]

rows = [{"usuario": u, "fecha": f, "r": r, "comentario": c} for (u, f, r, c) in USERS]


def load_real_rows():
    """Si existe un CSV exportado de Google Forms, lo usa en lugar de los ejemplos.

    Busca en ~/Downloads/forms_responses.csv o Docs/Analitica/respuestas_raw.csv.
    Formato: [Marca temporal, Nombre, P1..P10, Comentario]. Se detectan las 10
    columnas numéricas por posición para tolerar el orden exacto de Forms.
    """
    candidates = [os.path.expanduser("~/Downloads/forms_responses.csv"),
                  os.path.join(OUT, "respuestas_raw.csv")]
    for path in candidates:
        if not os.path.exists(path):
            continue
        try:
            with open(path, encoding="utf-8-sig", newline="") as fh:
                data = list(csv.reader(fh))
        except Exception:
            continue
        if len(data) < 2:
            continue
        out = []
        for row in data[1:]:
            scores = None
            start = 0
            for i in range(len(row) - 9):
                try:
                    window = [int(float(row[i + k])) for k in range(10)]
                except (ValueError, TypeError):
                    continue
                if all(1 <= s <= 5 for s in window):
                    scores = window
                    start = i
                    break
            if scores is None:
                continue
            name = row[1].strip() if len(row) > 1 and row[1].strip() else f"Usuario {len(out)+1}"
            comment = row[start + 10].strip() if len(row) > start + 10 else ""
            date = row[0].strip() if row else ""
            out.append({"usuario": name, "fecha": date, "r": scores, "comentario": comment})
        if out:
            print(f"[analytics] Usando {len(out)} respuestas reales de {path}")
            return out
    return None


real = load_real_rows()
if real:
    rows = real
else:
    print("[analytics] Sin CSV real; usando los 5 registros de ejemplo.")
averages = [mean(row["r"][i] for row in rows) for i in range(10)]
overall = mean(averages)


def kind(a):
    return "Fortaleza" if a >= 4 else "Aceptable" if a >= 3 else "Oportunidad de mejora"


# ---------------------------------------------------------------- CSV (Forms)
csv_path = os.path.join(OUT, "respuestas.csv")
with open(csv_path, "w", newline="", encoding="utf-8") as fh:
    w = csv.writer(fh)
    w.writerow(["Marca temporal", "Usuario"] + [f"P{i+1}" for i in range(10)] + ["¿Qué mejorarías del prototipo?"])
    for row in rows:
        w.writerow([row["fecha"], row["usuario"]] + row["r"] + [row["comentario"]])

# ---------------------------------------------------- Reporte estilo Forms
lines = ["# Respuestas del formulario — Prueba de funcionalidad NitroRhythm", "",
         f"**Respuestas: {len(rows)}**  ·  **Promedio general: {overall:.2f}/5**", "",
         "> Resumen por pregunta con la misma información que muestra Google Forms",
         "(promedio y distribución de respuestas).", ""]
for i, (q, a, asp) in enumerate(zip(QUESTIONS, averages, ASPECTS), 1):
    dist = Counter(row["r"][i - 1] for row in rows)
    dist_txt = " · ".join(f"{k}★: {dist.get(k,0)}" for k in range(1, 6))
    lines += [f"### {i}. {q}", f"*Aspecto: {asp}*", "",
              f"- **Promedio: {a:.2f}/5** — {kind(a)}",
              f"- Distribución: {dist_txt}", ""]
lines += ["## Pregunta abierta — comentarios", ""]
for row in rows:
    lines.append(f"- **{row['usuario']}:** {row['comentario']}")
with open(os.path.join(OUT, "reporte_google_forms.md"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(lines))

# ------------------------------------------------------------- Gráficos
plt.rcParams.update({"figure.facecolor": "#0b0b14", "axes.facecolor": "#0b0b14",
                     "text.color": "white", "axes.labelcolor": "white",
                     "xtick.color": "white", "ytick.color": "white", "axes.edgecolor": "#333355"})

fig, ax = plt.subplots(figsize=(12, 6))
colors = ["#33CCFF" if a >= 4 else "#FBBF24" if a >= 3 else "#FF4444" for a in averages]
bars = ax.bar(range(1, 11), averages, color=colors, edgecolor="#0b0b14", linewidth=1.2)
ax.axhline(4.0, color="#33CCFF", linestyle="--", linewidth=1, label="Fortaleza (≥4.0)")
ax.set_ylim(0, 5); ax.set_xticks(range(1, 11)); ax.set_xlabel("Pregunta")
ax.set_ylabel("Promedio (1-5)")
ax.set_title(f"NitroRhythm — Resultados de la prueba de funcionalidad\nPromedio general: {overall:.2f}/5 (5 usuarios)")
ax.legend()
for bar, value in zip(bars, averages):
    ax.text(bar.get_x() + bar.get_width() / 2, value + 0.08, f"{value:.1f}", ha="center", fontsize=9)
plt.tight_layout(); plt.savefig(os.path.join(OUT, "analitica_preguntas.png"), dpi=140); plt.close()

fig2, ax2 = plt.subplots(figsize=(9, 5))
names = [row["usuario"].split()[0] for row in rows]
totals = [mean(row["r"]) for row in rows]
ax2.bar(names, totals, color=["#33CCFF", "#5080E0", "#6CB6B5", "#FBBF24", "#FF6B9D"], edgecolor="#0b0b14")
ax2.set_ylim(0, 5); ax2.set_ylabel("Promedio (1-5)"); ax2.set_title("Promedio por usuario")
for i, v in enumerate(totals):
    ax2.text(i, v + 0.08, f"{v:.2f}", ha="center")
plt.tight_layout(); plt.savefig(os.path.join(OUT, "analitica_usuarios.png"), dpi=140); plt.close()

# Distribución apilada de toda la escala
fig3, ax3 = plt.subplots(figsize=(12, 6))
bottom = np.zeros(10)
palette = {1: "#FF4444", 2: "#FF8A44", 3: "#FBBF24", 4: "#6CB6B5", 5: "#33CCFF"}
for score in range(1, 6):
    counts = np.array([sum(1 for row in rows if row["r"][i] == score) for i in range(10)])
    ax3.bar(range(1, 11), counts, bottom=bottom, color=palette[score], label=f"{score}★", edgecolor="#0b0b14")
    bottom += counts
ax3.set_xticks(range(1, 11)); ax3.set_xlabel("Pregunta"); ax3.set_ylabel("N.º de respuestas")
ax3.set_title("Distribución de respuestas por pregunta (escala 1-5)")
ax3.legend(title="Valoración")
plt.tight_layout(); plt.savefig(os.path.join(OUT, "distribucion_respuestas.png"), dpi=140); plt.close()

# ------------------------------------------------------------- Resumen MD
md = ["# Analítica de la prueba de funcionalidad — NitroRhythm", "",
      f"**Respuestas: {len(rows)} usuarios · Promedio general: {overall:.2f}/5**", "",
      "![Promedio por pregunta](analitica_preguntas.png)", "",
      "![Promedio por usuario](analitica_usuarios.png)", "",
      "![Distribución de respuestas](distribucion_respuestas.png)", "",
      "| # | Aspecto | Pregunta | Promedio | Tipo de hallazgo |",
      "| :-- | :-- | :-- | :-- | :-- |"]
for i, (q, a, asp) in enumerate(zip(QUESTIONS, averages, ASPECTS), 1):
    md.append(f"| {i} | {asp} | {q[:70]} | {a:.2f} | {kind(a)} |")
md += ["", "## Promedio por usuario", ""]
for row in rows:
    md.append(f"- **{row['usuario']}**: {mean(row['r']):.2f}/5 — _{row['comentario']}_")
with open(os.path.join(OUT, "resumen_analitica.md"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(md))

# -------------------------------------------------------- Carpeta Forms
forms_md = ["# Formulario de Google — Prueba de funcionalidad NitroRhythm", "",
            "**Nombre:** Prueba de funcionalidad y experiencia del prototipo NitroRhythm",
            "**Objetivo:** Verificar estructura, interfaces, interactividad, concepto gráfico, niveles y mecánicas.",
            "**Escala:** lineal 1–5 (1 = Totalmente en desacuerdo … 5 = Totalmente de acuerdo)", "",
            "## Preguntas (crear como 'Escala lineal 1–5')", ""]
for i, q in enumerate(QUESTIONS, 1):
    forms_md += [f"**{i}.** {q}", "`1  2  3  4  5`", ""]
forms_md += ["**11. ¿Qué mejorarías del prototipo?** (respuesta corta)", ""]
forms_md += ["## Publicación", "",
             "1. Abra https://forms.google.com y cree el formulario con estas preguntas.",
             "2. En **Respuestas**, active los gráficos automáticos.",
             "3. Comparta el enlace con los 5 usuarios y pegue el enlace real abajo.", ""]
with open(os.path.join(FORMS, "PREGUNTAS_FORMULARIO.md"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(forms_md))
with open(os.path.join(FORMS, "ENLACE_FORMULARIO.txt"), "w", encoding="utf-8") as fh:
    fh.write("ENLACE DEL FORMULARIO (pegar aquí):\n\nhttps://forms.gle/________________\n")

print("Promedio general:", round(overall, 2))
for i, a in enumerate(averages, 1):
    print(f"P{i}: {a:.2f}")
print("Generado en:", OUT, "y", FORMS)
