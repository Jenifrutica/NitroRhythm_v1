# ASSETS.md — Inventario de recursos

Qué assets hay, dónde están, para qué se usan y de dónde vienen.

Ruta base: `Assets/Resources/NitroRhythm/` (todo se carga por `Resources.Load`).

---

## 1. Resumen de tamaño

| Carpeta | Tamaño | Contenido |
| :-- | :-- | :-- |
| `Models/` | 3.6 MB | 3 karts + 3 pilotos + proyectil |
| `Props/` | 3.3 MB | 8 props (torres, premios, bola, clave) |
| `Textures/` | 5.1 MB | 3 atlas de kart + 8 texturas de props |
| `Skies/` | 352 KB | 4 HDRI (JPG LDR) |
| `UI/` | 1.1 MB | 3 retratos + 7 ilustraciones de dominio |
| `Fonts/` | 192 KB | 3 fuentes SDF + materiales glow |
| `Materials/` | 48 KB | Materiales base URP |
| `prototype_data.json` | 12 KB | Datos narrativos |

---

## 2. Modelos (`Models/`)

| Archivo | Uso | Personaje |
| :-- | :-- | :-- |
| `Kart_Neon_01.fbx` | Kart jugable | Lyra Pulse (teal) |
| `Kart_Neon_02.fbx` | Kart jugable | Karel Volt (azul) |
| `Kart_Neon_03.fbx` | Kart villano | Vox Null (rojo) |
| `Piloto_Neon_01.fbx` | Piloto (skinned) | Lyra |
| `Piloto_Neon_02.fbx` | Piloto (skinned) | Karel |
| `Piloto_Neon_03.fbx` | Piloto (skinned) | Vox |
| `PremioMalo.fbx` | Proyectil del villano | — |

- Origen: modelos propios generados con **Hunyuan3D** y refinados en **Blender**
  (`SENA/general/`), texturizados con los atlas de color base.
- En runtime: `VisualEntityFactory` / `CharacterDisplay` los instancian y los
  normalizan con `ModelNormalizer` (escala mundial fija).
- Los FBX pueden traer cámaras/luces: `ModelSanitizer` las **desactiva**.

---

## 3. Props (`Props/`)

| Archivo | Uso |
| :-- | :-- |
| `PrizeGood.fbx` | Premio recogible en pista (turbo). |
| `PrizeBad.fbx` | Proyectil del villano. |
| `SpikeBall.fbx` | Bola de espinas (hazards/barras). |
| `Torre1..4.fbx` | Torres-altavoz (pilares de checkpoint y decoración). |
| `TrebleClef.fbx` | Clave de sol decorativa. |

- Origen: paquete `Particulas.unitypackage` / `Modelos3D` (Hunyuan), decimados a
  4–5 mil triángulos y texturas reducidas.
- En runtime: `PropFactory` los instancia; `PropAnimator` les da giro/bob/pulso.

---

## 4. Texturas (`Textures/`)

| Archivo | Uso |
| :-- | :-- |
| `Lyra.jpg` | Atlas de color base del kart de Lyra. |
| `Karel.jpg` | Atlas de color base del kart de Karel. |
| `Vox.jpg` | Atlas de color base del kart de Vox. |
| `Props/PrizeGood.jpg`, `PrizeBad.jpg`, `SpikeBall.jpg`, `Torre1..4.jpg`, `TrebleClef.jpg` | Texturas de props. |

- Las texturas PBR originales de Hunyuan (`texture_pbr_*`) se perdieron (rutas de
  Windows); se usaron los atlas de color base disponibles en `SENA/general`.
- En runtime: `VisualEntityFactory.CreateModelMaterial(color, texture)` crea el
  material URP Lit con `_BaseMap`.

---

## 5. Cielos (`Skies/`)

| Archivo | Dominio |
| :-- | :-- |
| `qwantani_dusk_2_puresky.jpg` | Percusalia |
| `kloppenheim_07_puresky.jpg` | Echoris |
| `kloofendal_48d_partly_cloudy_puresky.jpg` | Treble Spire |
| `qwantani_moonrise_puresky.jpg` | Noctua Chord |

Los demás dominios usan **cielo procedural** (`DomainTheme.panorama = null`):
Bassline Abyss, Arpeggion y Void Crescendo.

- Origen: **Poly Haven** (CC0), convertidos a JPG 2048×1024 con `hdr_to_jpg.py`.
- En runtime: `DomainDecorator` aplica el panorama o el cielo procedural.

---

## 6. Arte de interfaz (`UI/`)

| Archivo | Uso |
| :-- | :-- |
| `Portraits/lyra.jpg`, `karel.jpg`, `vox.jpg` | Retratos en selección, HUD, resultados. |
| `Domains/percusalia.jpg`, `echoris.jpg`, `bassline_abyss.jpg`, `arpeggion.jpg`, `treble_spire.jpg`, `noctua_chord.jpg`, `void_crescendo.jpg` | Ilustraciones de la tarjeta de inicio de cada dominio. |

- Origen: concept art del taller (dibujo digitalizado).
- En runtime: `UIArt` los carga desde `Resources`.

---

## 7. Fuentes (`Fonts/` y `Assets/Fonts/`)

| Fuente | Uso | Licencia |
| :-- | :-- | :-- |
| **Orbitron** | Títulos | SIL OFL 1.1 (`OFL-Orbitron.txt`) |
| **Rajdhani** | Texto y números | SIL OFL 1.1 (`OFL-Rajdhani.txt`) |
| **Material Icons** | Iconos | Apache 2.0 (`LICENSE-MaterialIcons.txt`) |

- TTF en `Assets/Fonts/`; SDF generados en `Resources/NitroRhythm/Fonts/`.
- Se generan con **NitroRhythm ▸ Build Font Assets**.

---

## 8. Materiales base (`Materials/`)

| Archivo | Shader | Uso |
| :-- | :-- | :-- |
| `BaseLit.mat` | URP/Lit | Material base para karts, plataformas, props. |
| `BaseUnlit.mat` | URP/Unlit | Suelo de rejilla y elementos sin luz. |
| `BaseUI.mat` | UI/Default | UI. |
| `BaseParticles.mat` | URP/Particles | Partículas. |
| `SkyPanoramic.mat` | Skybox/Panoramic | Cielos HDRI. |
| `SkyProcedural.mat` | Skybox/Procedural | Cielos procedurales. |

**Por qué existen:** `Shader.Find` puede devolver `null` en el build (shader
stripped) y todo sale magenta. El runtime clona estos materiales para garantizar
que el shader esté incluido.

---

## 9. Datos

`prototype_data.json` (TextAsset) → `PrototypeData.Instance`:
- 3 personajes, 10 dominios (7 jugables), 10 cutscenes.

---

## 10. Fuentes externas y licencias

| Fuente | Dónde | Licencia |
| :-- | :-- | :-- |
| Karts/pilotos/props (Hunyuan3D + Blender) | `SENA/general/`, `SENA/Modelos3D/` | Material propio |
| HDRI Poly Haven | `Resources/.../Skies` | CC0 |
| Google Fonts (Orbitron, Rajdhani) | `Assets/Fonts` | SIL OFL 1.1 |
| Material Icons (Google) | `Assets/Fonts` | Apache 2.0 |

Detalle completo en `Docs/CREDITOS_Y_LICENCIAS.md`.

---

## 11. Cómo se cargan (rutas de código)

| Recurso | Ruta `Resources.Load` |
| :-- | :-- |
| Datos | `NitroRhythm/prototype_data` |
| Kart | `NitroRhythm/Models/Kart_Neon_0X` |
| Piloto | `NitroRhythm/Models/Piloto_Neon_0X` |
| Proyectil | `NitroRhythm/Models/PremioMalo` |
| Prop | `NitroRhythm/Props/<nombre>` |
| Textura | `NitroRhythm/Textures/<nombre>` |
| Cielo | `NitroRhythm/Skies/<nombre>` |
| Retrato | `NitroRhythm/UI/Portraits/<id>` |
| Dominio | `NitroRhythm/UI/Domains/<id>` |
| Fuente | `NitroRhythm/Fonts/<nombre> SDF` |
| Material base | `NitroRhythm/Materials/Base*` |

---

## 12. Notas

- **Duplicado**: `Assets/Models/` y `Assets/Resources/.../Models/` contienen los
  mismos FBX. El runtime usa **Resources**; `Assets/Models` es la salida de Blender.
- **Escala**: los modelos Hunyuan tienen escalas distintas; `ModelNormalizer` los
  ajusta a un tamaño mundial fijo.
- **TMP**: los recursos esenciales están en `Assets/TextMesh Pro/`; no borrar.
- **link.xml**: preserva `UnityEngine.PhysicsModule` para el `MeshCollider` del suelo.
- **WebGL**: no hay archivos de audio; música/SFX son procedurales.
