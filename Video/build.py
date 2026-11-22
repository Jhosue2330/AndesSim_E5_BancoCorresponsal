#!/usr/bin/env python3
"""Arma el video narrado de ANDES-SIM E5 a partir de:
  - los cuadros grabados del juego (Recordings/frames, demo automática de Unity, 30 fps),
  - la narración por escena (assets/voice/sXX.wav),
  - música y efectos (.media/audio, y los sonidos propios del juego en Assets/_AndesSim/Audio).
Genera assets/shots/sXX.mp4, compositions/label-sXX.html e index.html.
Uso:  python3 build.py [--no-cut]
"""
import json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
FRAMES = os.path.join(REPO, "Recordings", "frames")
GAME_AUDIO = os.path.join(REPO, "Assets", "_AndesSim", "Audio")
FPS = 30
PAD = 0.8          # silencio después de cada frase
LEAD = 1.2         # la primera frase empieza tras el título

RED, AMBER, GREEN, CYAN, VIOLET = "#FF3B4E", "#FFB020", "#2BF59A", "#19D3F0", "#7A5CF0"

# Una toma por frase. src = cuadro de inicio en la grabación; speed = velocidad de la toma;
# extra = segundos extra de la escena (antes de la siguiente frase).
SHOTS = json.load(open(os.path.join(HERE, "shots.json")))

# Rótulos (kicker, título, color, duración); "big" = cifra grande al centro-izquierda.
LABELS = {
    "s02": ("ELIGE EL MODO", "EL PORTAL", VIOLET, 3.4),
    "s05": ("MODO ATAQUE · 2016", "LAS ÓRDENES ENTRAN", RED, 3.6),
    "s06": ("LA INTRUSIÓN", "LAZARUS", RED, 3.2),
    "s07": ("ALERTA", "«JUPITER»", AMBER, 4.2),
    "s09": ("EN EL ESCRITORIO", "EVIDENCIAS", CYAN, 3.4),
    "s10": ("LA LUPA", "FANDATION", AMBER, 3.6),
    "s11": ("BANCO ANDINO", "SIN RESPUESTA", RED, 4.0),
    "s12": ("RESULTADO · ATAQUE", "−US$ 101 M", RED, 5.0),
    "s13": ("ANÁLISIS", "¿POR QUÉ FALLÓ?", AMBER, 4.0),
    "s14": ("MODO DEFENSA", "TRES CONTROLES", GREEN, 4.0),
    "s15": ("FIREWALL", "INTRUSIÓN DETENIDA", GREEN, 3.4),
    "s16": ("BANCO ANDINO", "LLAMADA CONFIRMADA", GREEN, 3.6),
    "s17": ("RESULTADO · DEFENSA", "US$ 0 PERDIDOS", GREEN, 4.6),
}


def dur(path):
    out = subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                   "-of", "csv=p=0", path])
    return float(out)


def cut(sid, src, speed, seconds, folder="frames"):
    os.makedirs(os.path.join(HERE, "assets", "shots"), exist_ok=True)
    out = os.path.join(HERE, "assets", "shots", f"{sid}.mp4")
    n_in = int(seconds * speed * FPS) + 2
    vf = f"setpts=PTS/{speed},fps={FPS}" if speed != 1 else f"fps={FPS}"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-framerate", str(FPS), "-start_number", str(src),
                    "-i", os.path.join(REPO, "Recordings", folder, "f%05d.jpg"), "-frames:v", str(n_in), "-vf", vf,
                    "-t", f"{seconds:.3f}", "-c:v", "libx264", "-crf", "18", "-preset", "medium",
                    "-g", "15", "-pix_fmt", "yuv420p", "-movflags", "+faststart", out], check=True)
    return out


def label_html(sid, kicker, title, color, length):
    cid = f"label-{sid}"
    big = title.startswith("−US$") or title.startswith("US$")
    size = 150 if big else 112
    return f"""<template>
  <style>
    #root {{ position: absolute; inset: 0; }}
    #{cid}-box {{ position: absolute; left: 120px; bottom: 150px; }}
    #{cid}-bar {{ width: 14px; height: {size + 70}px; background: {color};
      position: absolute; left: 0; top: 0; transform-origin: 50% 100%; }}
    #{cid}-kmask, #{cid}-tmask {{ overflow: hidden; margin-left: 40px; }}
    #{cid}-kicker {{ font-family: Inter, sans-serif; font-weight: 700; font-size: 30px;
      letter-spacing: 0.18em; color: {color}; text-shadow: 0 2px 10px rgba(0,0,0,.85); }}
    #{cid}-title {{ font-family: Oswald, sans-serif; font-weight: 600; font-size: {size}px;
      line-height: 1.05; letter-spacing: 0.02em; color: #DDF6FF; white-space: nowrap;
      text-shadow: 0 4px 24px rgba(0,0,0,.9), 0 0 2px rgba(0,0,0,.9); }}
    #{cid}-shade {{ position: absolute; left: 0; bottom: 0; width: 1700px; height: 640px;
      background: radial-gradient(ellipse at 0% 100%, rgba(5,7,13,.94) 0%, rgba(5,7,13,.82) 38%, rgba(5,7,13,0) 72%); }}
  </style>
  <div id="root" data-composition-id="{cid}" data-width="1920" data-height="1080">
    <div id="{cid}-shade"></div>
    <div id="{cid}-box">
      <div id="{cid}-bar"></div>
      <div id="{cid}-kmask"><div id="{cid}-kicker">{kicker}</div></div>
      <div id="{cid}-tmask"><div id="{cid}-title">{title}</div></div>
    </div>
  </div>
  <script>
    const tl = gsap.timeline({{ paused: true }});
    tl.fromTo("#{cid}-shade", {{ opacity: 0 }}, {{ opacity: 1, duration: 0.3, ease: "power1.out" }}, 0);
    tl.fromTo("#{cid}-bar", {{ scaleY: 0 }}, {{ scaleY: 1, duration: 0.33, ease: "power3.out" }}, 0);
    tl.fromTo("#{cid}-kicker", {{ yPercent: 110 }}, {{ yPercent: 0, duration: 0.4, ease: "power4.out" }}, 0.12);
    tl.fromTo("#{cid}-title", {{ yPercent: 105 }}, {{ yPercent: 0, duration: 0.45, ease: "power4.out" }}, 0.2);
    tl.to("#{cid}-title", {{ yPercent: -105, duration: 0.27, ease: "power2.in" }}, {length - 0.4:.2f});
    tl.to("#{cid}-kicker", {{ yPercent: -110, duration: 0.27, ease: "power2.in" }}, {length - 0.35:.2f});
    tl.to("#{cid}-bar", {{ scaleY: 0, duration: 0.27, ease: "power2.in" }}, {length - 0.3:.2f});
    tl.to("#{cid}-shade", {{ opacity: 0, duration: 0.3, ease: "power1.in" }}, {length - 0.3:.2f});
    window.__timelines["{cid}"] = tl;
  </script>
</template>
"""


def main():
    do_cut = "--no-cut" not in sys.argv
    # ---- tiempos: la voz es el reloj ----
    t = 0.0
    plan = []
    for i, sh in enumerate(SHOTS):
        sid = sh["id"]
        vd = dur(os.path.join(HERE, "assets", "voice", f"{sid}.wav"))
        lead = LEAD if i == 0 else 0.0
        slot = lead + vd + PAD + sh.get("extra", 0.0)
        plan.append(dict(sid=sid, start=round(t, 3), slot=round(slot, 3), vstart=round(t + lead, 3), vdur=vd, **sh))
        t += slot
    total = round(t, 3)
    json.dump(plan, open(os.path.join(HERE, "plan.json"), "w"), indent=1)

    if do_cut:
        for p in plan:
            cut(p["sid"], p["src"], p.get("speed", 1.0), p["slot"] + 0.1, p.get("dir", "frames"))

    S = {p["sid"]: p for p in plan}
    os.makedirs(os.path.join(HERE, "compositions"), exist_ok=True)
    els = []
    z = 1
    # ---- tomas del juego ----
    for k, p in enumerate(plan):
        els.append(f'      <video id="shot-{p["sid"]}" class="clip shot" src="assets/shots/{p["sid"]}.mp4" '
                   f'data-start="{p["start"]}" data-duration="{p["slot"]}" data-track-index="1" muted playsinline '
                   f'style="z-index:{z}"></video>')
    z += 1
    # ---- título de apertura y tarjeta final ----
    els.append(f'      <div id="opening" data-composition-id="opening" data-composition-src="compositions/opening.html" '
               f'data-start="0" data-duration="{S["s01"]["slot"]:.3f}" data-track-index="2" data-track-kind="graphics" '
               f'data-width="1920" data-height="1080" style="z-index:{z}"></div>')
    end_start = S["s19"]["start"] - 0.2
    els.append(f'      <div id="closing" data-composition-id="closing" data-composition-src="compositions/closing.html" '
               f'data-start="{end_start:.3f}" data-duration="{total - end_start:.3f}" data-track-index="2" '
               f'data-track-kind="graphics" data-width="1920" data-height="1080" style="z-index:{z}"></div>')
    # ---- rótulos ----
    for sid, (kick, title, color, length) in LABELS.items():
        cid = f"label-{sid}"
        open(os.path.join(HERE, "compositions", f"{cid}.html"), "w").write(label_html(sid, kick, title, color, length))
        st = S[sid]["vstart"] + 0.35
        els.append(f'      <div id="{cid}" data-composition-id="{cid}" data-composition-src="compositions/{cid}.html" '
                   f'data-start="{st:.3f}" data-duration="{length:.2f}" data-track-index="3" data-track-kind="graphics" '
                   f'data-width="1920" data-height="1080" style="z-index:{z + 1}"></div>')

    # ---- audio ----
    aud = []
    for p in plan:
        aud.append(f'      <audio id="vo-{p["sid"]}" src="assets/voice/{p["sid"]}.wav" data-start="{p["vstart"]}" '
                   f'data-duration="{p["vdur"]:.3f}" data-track-index="10" data-volume="1"></audio>')
    attack_end = S["s14"]["start"]
    m1_end = S["s05"]["start"] + 3.0
    aud.append(f'      <audio id="music-open" src="assets/music/open.wav" data-start="0" data-duration="{m1_end:.3f}" '
               f'data-track-index="11" data-volume="0.16" data-fade-in="0.8" data-fade-out="2.5"></audio>')
    m2_len = attack_end - (m1_end - 2.5)
    aud.append(f'      <audio id="music-attack" src="assets/music/attack.wav" data-start="{m1_end - 2.5:.3f}" '
               f'data-duration="{min(m2_len, 73.5):.3f}" data-track-index="12" data-volume="0.14" data-fade-in="2" data-fade-out="2"></audio>')
    defense_len = total - attack_end + 0.5
    aud.append(f'      <audio id="music-defense" src="assets/music/defense.wav" data-start="{attack_end - 0.5:.3f}" '
               f'data-media-start="{max(0.0, 79.0 - defense_len):.3f}" data-duration="{defense_len:.3f}" '
               f'data-track-index="13" data-volume="0.15" data-fade-in="1.5" data-fade-out="2.5"></audio>')
    sfx = [  # (id, archivo, segundo, volumen)
        ("sfx-room", "assets/sfx/ambient.wav", 0.0, 0.10, attack_end),
        ("sfx-w1", "assets/sfx/whoosh1.mp3", S["s02"]["start"] - 0.25, 0.55, None),
        ("sfx-w2", "assets/sfx/whoosh2.mp3", S["s05"]["start"] - 0.25, 0.55, None),
        ("sfx-glitch", "assets/sfx/glitch.mp3", S["s06"]["start"] + 1.0, 0.6, None),
        ("sfx-alarm", "assets/sfx/alarm.wav", S["s07"]["start"], 0.45, None),
        ("sfx-alarm2", "assets/sfx/alarm.wav", S["s07"]["start"] + 1.0, 0.35, None),
        ("sfx-w3", "assets/sfx/whoosh1.mp3", S["s09"]["start"] - 0.25, 0.5, None),
        ("sfx-ring", "assets/sfx/phoneNoAnswer.wav", S["s11"]["start"] + 0.2, 0.55, None),
        ("sfx-w4", "assets/sfx/whoosh2.mp3", S["s12"]["start"] - 0.25, 0.5, None),
        ("sfx-glitch2", "assets/sfx/glitch.mp3", S["s14"]["start"] - 0.2, 0.55, None),
        ("sfx-shield", "assets/sfx/shield.mp3", S["s15"]["start"] + 1.2, 0.6, None),
        ("sfx-answer", "assets/sfx/phoneAnswer.wav", S["s16"]["start"] + 0.1, 0.4, 2.5),
        ("sfx-riser", "assets/sfx/riser.mp3", S["s17"]["start"] - 4.2, 0.45, None),
        ("sfx-impact", "assets/sfx/impact.mp3", S["s17"]["start"] + 0.35, 0.65, None),
        ("sfx-w5", "assets/sfx/whoosh1.mp3", S["s18"]["start"] - 0.25, 0.45, None),
    ]
    lanes = []  # cada efecto en un carril libre (sin solaparse)
    for sid, src, st, vol, d in sfx:
        st = max(0.0, st)
        d = min(d or dur(os.path.join(HERE, src)), total - st)
        lane = next((i for i, end in enumerate(lanes) if end <= st), None)
        if lane is None:
            lanes.append(0.0); lane = len(lanes) - 1
        lanes[lane] = st + d
        aud.append(f'      <audio id="{sid}" src="{src}" data-start="{st:.3f}" data-duration="{d:.3f}" '
                   f'data-track-index="{14 + lane}" data-volume="{vol}"></audio>')

    html = f"""<!doctype html>
<html lang="es">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=1920, height=1080" />
    <script src="https://cdn.jsdelivr.net/npm/gsap@3.15.0/dist/gsap.min.js"></script>
    <style>
      :root {{ --ground: #05070D; --ink: #DDF6FF; }}
      * {{ margin: 0; padding: 0; box-sizing: border-box; }}
      html, body {{ margin: 0; width: 1920px; height: 1080px; overflow: hidden; background: var(--ground); }}
      #root {{ position: relative; width: 100%; height: 100%; }}
      .shot {{ position: absolute; left: 0; top: 0; width: 1920px; height: 1080px; object-fit: cover; }}
    </style>
  </head>
  <body>
    <!-- generado por build.py: editar shots.json / build.py y volver a correrlo -->
    <div id="root" data-composition-id="main" data-start="0" data-duration="{total}" data-width="1920" data-height="1080">
{chr(10).join(els)}
{chr(10).join(aud)}
    </div>
    <script>
      window.__timelines = window.__timelines || {{}};
      const tl = gsap.timeline({{ paused: true }});
      window.__timelines["main"] = tl;
    </script>
  </body>
</html>
"""
    open(os.path.join(HERE, "index.html"), "w").write(html)
    print(f"total {total:.2f}s · {len(plan)} tomas")
    for p in plan:
        print(f'{p["sid"]} {p["start"]:7.2f} +{p["slot"]:5.2f}  src {p["src"]} x{p.get("speed", 1.0)}')


if __name__ == "__main__":
    main()
