# ANDES-SIM · Escenario 5 · Banco Corresponsal Internacional (VR)

Instrucciones compartidas para **todos los agentes de IA** que trabajan en este repo
(Claude Code y Antigravity). Léelas completas antes de tocar cualquier archivo.

## 1. Qué es este proyecto

Curso de VR/AR · Semana 10 · Caso BANCO ANDINO. Simulador basado en el robo al
Banco de Bangladesh (2016, grupo Lazarus), con entidades **ficticias**.
Nuestro equipo tiene el **Escenario 5: Banco Corresponsal Internacional** (tipo **VR**, Cardboard).

- Fechas del escenario: **4 al 8 feb 2016**.
- Historia: en la sala de monitoreo salta una alerta: la calle de la agencia receptora
  se llama igual que un petrolero de una lista de sanciones (**«JUPITER»**). **30 órdenes**
  quedan congeladas, pero **5 ya pasaron** (US$101 M: 4 a Manila por US$81 M y 1 a la
  «Fandation» por US$20 M). Las consultas al BANCO ANDINO no tienen respuesta: es fin de semana.
- Vulnerabilidades: contactos de emergencia solo en días hábiles; sin confirmación por canal
  alterno; reglas que no frenaron las 5 primeras.
- Defensas: contactos 24/7 para incidentes; confirmación por llamada a número registrado;
  reglas por destino y monto inusual.
- Flujo: Portal a la sala → Pantallas de monitoreo → Alerta «Jupiter» → Decidir bloqueos →
  Llamar al banco → Resultado.

### Requisitos funcionales (RF)
1. Tres pantallas grandes muestran órdenes entrando en tiempo real.
2. Una alerta parpadea al detectar la coincidencia con la lista de sanciones.
3. El jugador elige qué órdenes bloquear mirando y tocando cada una.
4. Modo ataque: el teléfono suena sin respuesta. Modo defensa: se completa la verificación.
5. La pantalla final muestra cuánto dinero se salvó en cada modo.

### Criterios de aceptación
- El jugador entiende por qué se activó la alerta (texto explicativo).
- En defensa, las 5 órdenes pagadas pasan a retenidas.

### Requisitos generales VR (obligatorios)
Unity 6 · APK Android real · modo Ataque y Defensa en la misma app · portal de entrada ·
teletransporte por mirada (sin movimiento continuo) · ≥ 5 objetos interactivos con la retícula ·
sonido · pantalla final · créditos dentro de la app (incluye las IA usadas).

## 2. Arquitectura común (contrato del curso — NO cambiar firmas)

```csharp
public enum SimMode { Ataque, Defensa }
public class GameManager : MonoBehaviour {
    public static GameManager I;
    public ScenarioData data;
    public SimMode mode = SimMode.Ataque;
    public void RegisterEvidence(string id);
    public void ApplyDefense(string id);
    public void EndScenario();
}
```

## 3. Estructura de carpetas

```
Assets/_AndesSim/
  Scripts/Core/          GameManager, ScenarioData, SimMode, UIManager, AudioManager
  Scripts/Interaction/   GazeInteractable, GazeReticlePointer, EvidenceItem, DefenseControl, TeleportPoint, PortalEntrada
  Scripts/Scenario05/    OrderFeed, SanctionsMatcher, CallbackDefense, ResultPanel, ...
  Scripts/Editor/        Constructor automático de la escena
  Data/                  ScenarioData (.asset)
  Scenes/                Corresponsal_VR.unity
  Art/  Audio/  Prefabs/ Recursos de arte y sonido (con licencia libre)
```

Namespace de todo el código: `AndesSim` (y `AndesSim.Scenario05`, `AndesSim.EditorTools`).

## 4. Reparto de trabajo entre IAs

| Área | Dueño | Notas |
|---|---|---|
| `Scripts/Core/`, `Scripts/Interaction/` | **Claude** | Contrato común. Otros agentes: solo proponer cambios en `TAREAS.md`. |
| `Scripts/Scenario05/` lógica | **Claude** | |
| `Scripts/Editor/` | **Claude** | |
| Arte, materiales, modelos, `Art/`, `Prefabs/` visuales | **Antigravity** | Modelos libres (Sketchfab, Poly Pizza) con créditos. |
| Audio (`Audio/`), textos de diálogos y créditos | **Antigravity** | |
| Pulido de UI (layout, fuentes, colores) | **Antigravity** | Sin cambiar nombres de campos públicos. |
| Escena en el Editor, build APK, prueba en celular | **Humano** | Las IAs no controlan el Editor. |

## 5. Reglas para no pisarse

1. Antes de empezar, lee `TAREAS.md`, marca la tarea como `EN CURSO (agente)` y al terminar `HECHO`.
2. **No edites un archivo que otra IA tiene `EN CURSO`.**
3. No renombres ni muevas scripts existentes: Unity pierde las referencias (`.meta`/GUID).
   Nunca borres ni regeneres archivos `.meta`.
4. No cambies firmas públicas del contrato (sección 2). Agrega, no rompas.
5. Un cambio = un commit pequeño con mensaje claro (`feat(e5): ...`, `fix(core): ...`).
6. El código debe compilar sin el plugin de Cardboard (usamos `GazeReticlePointer` propio,
   compatible con los mensajes `OnPointerEnter/Exit/Click` de `CardboardReticlePointer`).
7. Input: el proyecto usa el **Input Manager clásico** (`activeInputHandler: 0`).
8. Textos con TextMeshPro (`com.unity.ugui` 2.0).

## 6. Reglas éticas del curso (incumplir = nota anulada)

- Solo simulación y defensa. Nada contra sistemas reales.
- Solo datos inventados: nombres, cuentas y correos ficticios.
- Entidades ficticias: BANCO ANDINO, Banco Corresponsal Internacional, Banco Pacífico Insular,
  agencia Las Palmeras. **Sin logos ni colores de bancos reales.** No usar nombres de personas reales
  del caso (los beneficiarios de las órdenes son inventados).
- Lazarus = grupo criminal; sin caricaturas de ningún pueblo o nacionalidad.
- Créditos: todo recurso de terceros con autor y licencia, e indicar las IA usadas
  (Claude Code y Google Antigravity).
