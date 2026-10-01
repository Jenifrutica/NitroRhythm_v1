#!/usr/bin/env python3
"""Genera la analítica de la prueba de funcionalidad (GA5-AA1-EV01).

3 usuarios SIMULADOS (para demostración del flujo) + 2 usuarios REALES
pendientes de diligenciar. Produce CSV, gráficos PNG y un resumen Markdown.
"""
import csv
import os
from statistics import mean

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

OUT = "/home/jenifrutica/SENA/NitroRythm/Evidencias/Documentos/Analitica"
os.makedirs(OUT, exist_ok=True)

QUESTIONS = [
    "1. Estructura general clara (menú→selección→carrera→resultados)",
    "2. Navegación entre pantallas coherente",
    "3. Interfaces claras (menú, selección, HUD, pausa)",
    "4. HUD útil (velocímetro, progreso, puntuación)",
    "5. Controles del kart responsivos",
    "6. Acciones del juego consistentes (salto, obstáculos)",
    "7. Estética neón y animaciones",
    "8. Música y generación reactiva al audio",
    "9. Progresión de niveles y checkpoints",
    "10. Mecánicas (carrera, combate, multijugador)",
]

# Respuestas simuladas (escala 1-5). Las dos últimas filas son reales (vacías).
ROWS = [
    {"usuario": "Simulado A", "tipo": "simulado", "r": [5, 4, 5, 4, 4, 5, 5, 4, 4, 5],
     "comentario": "Muy divertido; me gustó el multijugador local y la música."},
    {"usuario": "Simulado B", "tipo": "simulado", "r": [4, 4, 4, 3, 4, 3, 4, 5, 3, 4],
     "comentario": "El HUD podría mostrar más información y la dificultad sube rápido."},
    {"usuario": "Simulado C", "tipo": "simulado", "r": [5, 5, 4, 4, 5, 4, 5, 4, 4, 5],
     "comentario": "La selección de kart en 3D se ve muy bien; repetiría."},
    {"usuario": "Real 1", "tipo": "real", "r": ["", "", "", "", "", "", "", "", "", ""], "comentario": ""},
    {"usuario": "Real 2", "tipo": "real", "r": ["", "", "", "", "", "", "", "", "", ""], "comentario": ""},
]

# CSV
csv_path = os.path.join(OUT, "respuestas.csv")
with open(csv_path, "w", newline="", encoding="utf-8") as fh:
    writer = csv.writer(fh)
    writer.writerow(["Usuario", "Tipo"] + [f"P{i+1}" for i in range(10)] + ["Comentario"])
    for row in ROWS:
        writer.writerow([row["usuario"], row["tipo"]] + row["r"] + [row["comentario"]])

# Promedios sobre simulados
sim = [row for row in ROWS if row["tipo"] == "simulado"]
averages = [mean(row["r"][i] for row in sim) for i in range(10)]
overall = mean(averages)

# Gráfico de barras por pregunta
fig, ax = plt.subplots(figsize=(12, 6))
colors = ["#33CCFF" if a >= 4 else "#FBBF24" if a >= 3 else "#FF4444" for a in averages]
bars = ax.bar(range(1, 11), averages, color=colors, edgecolor="#0b0b14", linewidth=1.2)
ax.axhline(4.0, color="#33CCFF", linestyle="--", linewidth=1, label="Fortaleza (≥4.0)")
ax.set_ylim(0, 5)
ax.set_xticks(range(1, 11))
ax.set_xlabel("Pregunta")
ax.set_ylabel("Promedio (1-5)")
ax.set_title(f"NitroRhythm — Resultados de la prueba de funcionalidad\nPromedio general: {overall:.2f}/5 (3 usuarios simulados)")
ax.legend()
for bar, value in zip(bars, averages):
    ax.text(bar.get_x() + bar.get_width() / 2, value + 0.08, f"{value:.1f}", ha="center", fontsize=9)
plt.tight_layout()
chart_path = os.path.join(OUT, "analitica_preguntas.png")
plt.savefig(chart_path, dpi=140)
plt.close()

# Gráfico por usuario
fig2, ax2 = plt.subplots(figsize=(8, 5))
names = [row["usuario"] for row in sim]
totals = [mean(row["r"]) for row in sim]
ax2.bar(names, totals, color=["#33CCFF", "#5080E0", "#6CB6B5"], edgecolor="#0b0b14")
ax2.set_ylim(0, 5)
ax2.set_ylabel("Promedio (1-5)")
ax2.set_title("Promedio por usuario (simulados)")
for i, v in enumerate(totals):
    ax2.text(i, v + 0.08, f"{v:.2f}", ha="center")
plt.tight_layout()
users_chart = os.path.join(OUT, "analitica_usuarios.png")
plt.savefig(users_chart, dpi=140)
plt.close()

# Resumen Markdown
md = ["# Analítica de la prueba de funcionalidad — NitroRhythm", "",
      f"**Promedio general (3 usuarios simulados): {overall:.2f}/5**", "",
      "> Las filas **Real 1** y **Real 2** deben diligenciarse con los usuarios reales.",
      "", "| # | Aspecto | Pregunta | Promedio | Tipo de hallazgo |",
      "| :-- | :-- | :-- | :-- | :-- |"]
aspects = ["Estructura", "Estructura", "Interfaces", "Interfaces", "Interactividad",
           "Interactividad", "Concepto gráfico", "Concepto gráfico", "Niveles", "Mecánicas"]
for i, (q, a, asp) in enumerate(zip(QUESTIONS, averages, aspects), start=1):
    kind = "Fortaleza" if a >= 4 else "Aceptable" if a >= 3 else "Oportunidad de mejora"
    md.append(f"| {i} | {asp} | {q.split('. ',1)[1]} | {a:.2f} | {kind} |")
md += ["", "## Promedio por usuario", ""]
for row in sim:
    md.append(f"- **{row['usuario']}**: {mean(row['r']):.2f}/5 — _{row['comentario']}_")
md += ["", "## Gráficos", "",
       "- `analitica_preguntas.png` — promedio por pregunta.",
       "- `analitica_usuarios.png` — promedio por usuario."]

with open(os.path.join(OUT, "resumen_analitica.md"), "w", encoding="utf-8") as fh:
    fh.write("\n".join(md))

print("Promedio general:", round(overall, 2))
for i, a in enumerate(averages, 1):
    print(f"P{i}: {a:.2f}")
print("Generado en:", OUT)
