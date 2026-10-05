# Paquete de referencia para Hunyuan3D — NitroRhythm

No puedo ejecutar Hunyuan3D desde mi entorno, así que dejo todo listo para que lo
generes tú: **vistas de referencia** de cada personaje (renderizadas desde el
juego), **descripciones y paletas**, **prompts** y los pasos para meter el modelo
nuevo en Unity sin romper nada.

> Los modelos actuales ya funcionan en el juego. Esto es para mejorar el detalle
> (sobre todo los **pilotos**, que hoy son pequeños y casi no se ven entre el asiento
> y el ala del kart; además el juego los tiñe de un solo color y se pierde el diseño original).

## 1. Qué hay en `Referencias/` (listo para subir a Hunyuan3D)

Fuente: lo que tenías en `SENA/` — `Guia 2/010326_conceptArt` (diseños a mano
digitalizados), `general/*.blend` (karts con sus texturas) y `general/Wireframes`.

```
Referencias/
  lyra/  karel/  vox/
    personaje_frente.png · personaje_tres_cuartos.png · personaje_trasera.png · personaje_lado.png
    kart_frente.png      · kart_tres_cuartos.png      · kart_trasera.png      · kart_lado.png
    hoja_concepto_original.png     (la hoja completa del concept art)
  hoja_karts_con_piloto_original.png      (los 3 karts con piloto, frente/atrás/lado/3-4)
  hoja_poses_conduccion_original.png      (poses de conducción de los 3)
```

| Serie | Qué es | Resolución | Notas |
| :-- | :-- | :-- | :-- |
| `personaje_*` | Recortes de la hoja de concepto, ampliados a 1280 px de alto | La fuente es de ~500–580 px por vista (ampliación ×2.2, sin detalle nuevo) | Pose en **T**; fondo de la hoja. Karel y Lyra traen restos del fondo/reglas |
| `kart_*` | **Renders de tus modelos originales de Blender** con sus texturas (`KartLyra`, `karelKart`, `voxKartv2`) | 1600 × 1200, fondo gris neutro, luz de estudio | Sin sombras; frente = morro hacia la cámara |

Cómo usarlas en Hunyuan3D:
- **Multivista** (frente + lado + trasera, como pide Hunyuan3D-2mv): usa
  `*_frente.png`, `*_lado.png` y `*_trasera.png` del mismo personaje o kart, y
  `*_tres_cuartos.png` como imagen extra o para comprobar el resultado.
- **Una sola imagen:** `*_tres_cuartos.png` suele dar el mejor volumen.
- Genera el **kart** y el **personaje** por separado (no juntos), y el personaje en T-pose.
  En el juego el piloto va sentado: se pone con `pilotOffset` y, si hace falta, se
  sienta con una pose en Blender.
- Las vistas `kart_*` salen del modelo actual: si Hunyuan debe **mejorar** el kart, dale
  también la hoja `hoja_karts_con_piloto_original.png` (diseño original) para el estilo.

Regenerar las vistas de los karts (Blender, sin GPU):

```bash
flatpak run --command=blender org.blender.Blender --background \
  --python "$PWD/Docs/Tools/blender/render_views.py" -- \
  "/home/jenifrutica/SENA/general/KartLyra.blend" "$PWD/Docs/Hunyuan/Referencias/lyra" kart 0
```

(Último argumento: azimut en el que el morro mira a la cámara; `0` sirve para los 3 karts.
`VIEW_SIZE=2048` en el entorno da más resolución.)

`Vistas_Personajes/` (carpeta anterior) son capturas del **juego**, con los modelos
teñidos de un solo color; **no** las uses como referencia de estilo.

## 2. Lyra Pulse — heroína

- **Concepto original:** espíritu blanco de estilo anime *xianxia* (cultivación); pureza, serenidad,
  gracia melódica. Kart "de agua sonora": curvas fluidas, derrapes suaves.
- **Personaje:** cabello larguísimo **blanco que degrada a teal**, **orejas de zorro**, pequeña
  **corona/tiara dorada** con cintas teal, ojos ámbar, gargantilla oscura; **kimono blanco crema**
  de hombros descubiertos con **olas teal y notas musicales doradas**, **obi oscuro con lazo teal**
  y borlas blancas, mangas anchas, cola larga en dos capas (blanco y teal), **geta negros** de plataforma.
- **Paleta:** blanco crema `#F4ECDF`, crema sombra `#E8E0D0`, gris teal `#C8D8D8`, cabello `#A8C8CA`
  con reflejos `#5C9090`, ornamentos dorados.
- **Kart:** carrocería teal luminoso `#68B9BA` con bordes `#3C7878`, **alas de plumas angulares**
  `#9CC8C8` tras el asiento, llantas negras `#282828`, volante negro.

**Prompt (personaje):**
> Full-body anime character in T-pose, young woman with extremely long white hair fading to
> teal at the ends, white fox ears, small golden tiara with teal ribbons, amber eyes, white
> cream off-shoulder kimono with teal wave patterns and golden musical notes, dark obi with a
> teal bow and white tassels, wide sleeves, long layered skirt white and teal, black platform
> geta, xianxia spirit style, clean flat colors, neutral background.

**Prompt (kart):**
> Stylized arcade go-kart, luminous teal body with deep-teal edges, large angular feathered
> wings behind the seat, low pointed nose, black wheels with grey rims, black steering wheel,
> low-poly game-ready, neutral background.

## 3. Karel Volt — héroe

(En el storyboard aparece también como *Kaito*/*Kael*.)

- **Concepto original:** poder contenido, inteligencia fría, control racional. Kart = "máquina de
  precisión"; el **propulsor azul claro** es el único "grito" de energía.
- **Personaje:** hombre elfo de **orejas puntiagudas**, cabello largo **negro que degrada a violeta**
  en las puntas; **túnica tipo kimono azul** `#283C64` con **rombos cian**, ribetes celestes, **obi
  carmesí** con adorno plateado, **hakama índigo oscuro** `#28283C`, tabi blancos y geta.
- **Paleta:** índigo muy oscuro `#28283C`, índigo `#3C3C50`, azul índigo `#283C64`, azul medio `#5080A0`,
  contornos `#1E1E2D`.
- **Kart:** carrocería azul `#4B6496` con **líneas neón cian**, paneles `#283C64`, **motor con tambores
  naranjas** a la vista, **alerón trasero**, respaldo alto, propulsor `#60A0E0`, llantas `#141428`
  con aro cian.

**Prompt (personaje):**
> Full-body anime character in T-pose, tall male elf with pointed ears, long black hair
> fading to violet at the tips, deep-blue kimono-style robe with cyan diamond patterns and
> light-blue trim, crimson obi with a silver ornament, dark indigo hakama, white tabi and
> geta, calm calculating expression, clean flat colors, neutral background.

**Prompt (kart):**
> Precision racing go-kart, cobalt-blue body with thin glowing cyan neon lines on nose and side
> pods, exposed rear engine with orange drums, rear wing, tall seat back, light-blue thruster,
> black wheels with cyan rims, low-poly game-ready, neutral background.

## 4. Vox Null — villano

- **Concepto original:** amenaza silenciosa, anti-armonía, "poder que absorbe en lugar de emitir".
  Tonos apagados (más inquietantes que un rojo brillante). El **orbe púrpura** del kart es su única
  fuente de energía visible y gira caótico.
- **Personaje:** **máscara plateada de zorro** con dos cuernos altos y marcas rojas, **cabello plateado
  larguísimo**, orejas puntiagudas, **manto de pelaje blanco** sobre los hombros con hombreras
  metálicas, **kimono blanco con mangas carmesí** y escudos (mon) blancos, **túnica negra**, hakama
  blanco con interior rojo, **gran cola peluda blanca**, geta.
- **Paleta:** negro rojizo `#2D2020`, marrón oscuro `#3C2828`, granate `#644040`, rojo apagado `#8A5050`,
  blanco/plata.
- **Kart:** carrocería gris oscuro `#484040` con paneles `#686068`, **líneas y llantas con brillo rojo
  burdeos** `#783C3C`, **muchas espinas negras** (alas/pinchos en el chasis y el morro), **orbe
  púrpura** `#5D304D` / resplandor `#704561` detrás del asiento.

**Prompt (personaje):**
> Full-body anime character in T-pose, mysterious figure with a silver fox mask with two tall
> horns and red markings, very long silver hair, pointed ears, white fur mantle over shoulders
> with metal pauldrons, white kimono with crimson sleeves and white crests, black tunic, white
> hakama with red lining, huge fluffy white tail, geta, ominous, clean flat colors, dark neutral
> background.

**Prompt (kart):**
> Villain go-kart, dark gray body with burgundy-red glowing trim and red-glowing wheel rims,
> many long black spikes on the chassis, wings and nose, a dark purple energy orb mounted
> behind the seat, sinister, low-poly game-ready, neutral background.

## 5. Requisitos técnicos para que encaje sin tocar código

| Requisito | Valor | Motivo |
| :-- | :-- | :-- |
| Formato | `.fbx` (o `.glb` convertido a `.fbx` con Blender) | El juego carga FBX desde `Resources` |
| Nombres | Mismos que hoy: `Kart_Neon_01..03.fbx`, `Piloto_Neon_01..03.fbx` | Los referencia `prototype_data.json` |
| Carpeta | `Assets/Resources/NitroRhythm/Models/` | `Resources.Load` |
| Orientación | Eje vertical = Y arriba al importar; morro hacia −Z del modelo | El showcase lo gira 180° |
| Escala | Cualquiera | `ModelNormalizer` lo lleva a 3.8 (kart) / 1.9 (piloto) unidades |
| Textura | JPG/PNG de color base en `Assets/Resources/NitroRhythm/Textures/` (`Lyra`, `Karel`, `Vox`) | El kart usa la textura del personaje |
| Piloto | Malla estática o con esqueleto; ambas funcionan | `ModelNormalizer` soporta MeshRenderer y SkinnedMeshRenderer |
| Polígonos | Kart ≤ 40 k tris, piloto ≤ 25 k tris | Build WebGL (33 MB hoy) |

Conversión desde Blender (si Hunyuan entrega `.glb`):

```bash
flatpak run --command=blender org.blender.Blender --background --python-expr \
 "import bpy; bpy.ops.import_scene.gltf(filepath='/ruta/piloto.glb'); \
  bpy.ops.export_scene.fbx(filepath='/ruta/Piloto_Neon_01.fbx', axis_forward='-Z', axis_up='Y')"
```

## 6. Integrar el modelo nuevo y reajustar el piloto

1. Copia el `.fbx` con el nombre exacto a `Assets/Resources/NitroRhythm/Models/`
   (Unity genera el `.meta` al abrir el proyecto).
2. Ejecuta la prueba de alineación (necesita GPU, sin `-nographics`; desde la raíz del proyecto):

   ```bash
   flatpak run --command="/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity" com.unity.UnityHub \
     -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode \
     -testFilter CharacterDisplayAlignmentTests -testResults Logs/tr_align.xml -logFile Logs/cli_align.log
   ```

   En el log aparece una línea por personaje:

   ```
   [Alignment] lyra: kart c=(..) | pilot c=(..) | dXZ=(dx,dz) pilotBottomVsKartTop=..
   ```

3. Ajusta `pilotOffset` del personaje en `prototype_data.json`:
   - `x` ← `x − dx` (centra el piloto lateralmente).
   - `z` ← `z + (0.70 − dz)` (lo lleva a la zona del asiento; el objetivo es dz ≈ 0.70).
   - `y` sube o baja el piloto; revisa `lyra_piloto_lado.png` tras regenerar las vistas.
4. Repite hasta que la prueba pase (|dx| < 0.5 y 0.3 ≤ dz ≤ 1.2) y mira
   `Docs/Hunyuan/Vistas_Personajes/*_kart_lado.png` para confirmar que va sentado.
5. Corre la suite completa (ver `Docs/Evidencias/RESULTADO_PRUEBAS.md`).

## 7. Alternativa sin Hunyuan

Si prefieres no depender de generación externa, puedo afinar los modelos actuales en
Blender (escala del piloto, postura, rig) con `Docs/Tools/blender/`. Dímelo y lo hago.
