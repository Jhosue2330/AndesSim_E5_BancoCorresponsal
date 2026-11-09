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

## Propuestas de cambio al núcleo
(Si Antigravity necesita cambiar algo en `Scripts/Core` o `Scripts/Interaction`, anotarlo aquí.)
