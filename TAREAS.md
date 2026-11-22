# Tablero de tareas · Escenario 5

Estados: `PENDIENTE` · `EN CURSO (Claude|Antigravity|Humano)` · `HECHO`.
Regla: no tocar archivos de una tarea `EN CURSO` de otro agente.

## Sprint 1 · Base funcional

| # | Tarea | Dueño | Estado |
|---|---|---|---|
| 1 | Crear proyecto Unity 6, git, .gitignore, AGENTS.md | Claude | HECHO |
| 2 | Arquitectura común (GameManager, ScenarioData, EvidenceItem, DefenseControl, UIManager, AudioManager) | Claude | HECHO |
| 3 | Interacción por mirada + teletransporte + portal de entrada | Claude | HECHO |
| 4 | Lógica escenario 5 (OrderFeed, SanctionsMatcher, CallbackDefense, ResultPanel) | Claude | HECHO |
| 5 | Menú `AndesSim > Construir escena Escenario 5` (escena con primitivas) | Claude | HECHO |
| 6 | Instalar módulo **Android Build Support** en Unity Hub para 6000.0.85f2 | Humano | HECHO |
| 7 | Instalar **Cardboard XR Plugin** y configurar XR Plug-in Management (Android) | Claude | HECHO |

## Sprint 2 · Arte, sonido y pulido

| # | Tarea | Dueño | Estado |
|---|---|---|---|
| 8 | Reemplazar primitivas por modelos libres: sala de monitoreo, escritorios, teléfono, pantallas curvas | Antigravity | HECHO |
| 9 | Audios: ambiente de sala, alarma, teléfono sonando, tono de llamada, clic de mirada (CC0) → asignar en `AudioManager` | Antigravity | HECHO |
| 10 | Pulir UI de pantallas (fuentes, colores estilo terminal verde/rojo, legibilidad en VR) | Antigravity | PENDIENTE |
| 11 | Texto de créditos completo (autores, licencias, IA usadas) en `ScenarioData.credits` | Antigravity | HECHO |
| 12 | Probar en celular, ajustar escala de textos y distancia de pantallas | Humano | HECHO (Redmi Note 14: cabeza, textos, selección y sonido OK) |

## Sprint 3 · Entrega

| # | Tarea | Dueño | Estado |
|---|---|---|---|
| 13 | Build APK firmado y prueba en 2 celulares | Humano | PENDIENTE |
| 14 | Video 2–3 min (ataque y defensa) | Humano | PENDIENTE |
| 15 | Ficha del proyecto (máx. 4 páginas) | Humano + IA | PENDIENTE |

## Sprint 4 · Rediseño "VR avanzada" (game design)

| # | Tarea | Dueño | Estado |
|---|---|---|---|
| 16 | Art bible + paleta única (`Palette.cs`) y re-teñido de modelos | Claude | HECHO |
| 17 | Sala cyber-noir: neón, ventanal con ciudad nocturna, racks con LEDs, techo de paneles, polvo ambiental | Claude | HECHO |
| 18 | Mesa holográfica: mapa del dinero con pulsos (Andino → Corresponsal → Manila/Sri Lanka) y contadores | Claude | HECHO |
| 19 | Ambiente por estado (calma/alerta/peligro/protegido), baliza giratoria y música de tensión generada | Claude | HECHO |
| 20 | Panel de misión, intro con máquina de escribir, tarjetas de título y calificación S/A/B/C | Claude | HECHO |
| 21 | Feedback: evidencias (+1), palancas animadas, teléfono que vibra, filas animadas, fundido al teletransportarse | Claude | HECHO |
| 22 | Probar en celular la versión rediseñada (rendimiento, lectura, comodidad) | Humano | PENDIENTE |

## Sprint 5 · Paquete "wow" (todas las ideas)

| # | Tarea | Dueño | Estado |
|---|---|---|---|
| 23 | Vibración del celular y sonido espacial 3D | Claude | HECHO |
| 24 | Tormenta en el ventanal (lluvia, relámpagos que iluminan la sala) y glitch de hackeo | Claude | HECHO |
| 25 | Blender: analistas animados, supervisora holográfica, teléfono, globo y ascensor | Claude | HECHO |
| 26 | Llegada en ascensor de vidrio por la fachada | Claude | HECHO |
| 27 | Caminar libre con túnel anti-mareo; inspeccionar documentos; expediente de órdenes | Claude | HECHO |
| 28 | Llamada con opciones; minijuego de la lupa «FANDATION»; logros y tiempo | Claude | HECHO |
| 29 | Intrusión de Lazarus (enjambre) + escudo firewall; holograma gigante final; línea de tiempo | Claude | HECHO |
| 30 | Modo demo automático para grabar el video | Claude | HECHO |
| 31 | Voz narradora real (TTS) — hoy son subtítulos con "voz" sintetizada por sílabas | Humano | PENDIENTE (decidir herramienta) |
| 32 | Probar en celular el paquete wow (rendimiento con personajes, lluvia y efectos) | Humano | PENDIENTE |
| 33 | Video narrado 2:37 con tomas reales del juego, voz, música y efectos (`Video/`, HyperFrames) | Claude | HECHO |

## Propuestas de cambio al núcleo
(Si Antigravity necesita cambiar algo en `Scripts/Core` o `Scripts/Interaction`, anotarlo aquí.)
