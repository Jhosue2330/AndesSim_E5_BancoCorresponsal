# SPEC · Video narrado ANDES-SIM E5

GOAL:    the viewer understands the game in one go (Attack 2016 → why it failed → Defense) and
         ends up remembering "one alert, one call and three rules: −US$ 101 M against US$ 0".
OUTPUT:  HyperFrames, 1920 x 1080, 30 fps, MP4.
TIME:    ~160 s. The narrator's voice is the clock: each scene lasts as long as its line + 0.8 s.
LOOK:    the medium is our own game: real Unity footage at full frame (1920x1080, recorded
         frame by frame from the automatic demo). No images from outside the game.
         Overlays use the game's art bible (Palette.cs): ground #05070D, ink #DDF6FF,
         data cyan #19D3F0, threat red #FF3B4E, alert amber #FFB020, protected green #2BF59A.
         Display face Oswald 600, 96–150 px, tracking 2%; kicker Inter 700, 30 px, tracking 18%.
         Only one accent colour per label, matching the moment (red = attack, amber = alert,
         green = defense).
OBJECTS: the portal, the glass elevator, the SWIFT screens, the holographic table, the «JUPITER»
         alert, the evidence on the desk, the FANDATION magnifier, the phone, the defense panel,
         the firewall shield, the result screen — all filmed in the game.
MOTION:  a label enters with a 10-frame colour bar wipe (power3.out), then the text rises out of
         a mask in 12 frames (power4.out); it holds still while it is read; it leaves with an
         8-frame wipe (power2.in). Hard cuts between shots; a whoosh only on chapter changes.
SHOTS:   one shot per narrated line (s01…s19), see `build.py` for frames and times.
SOUND:   two music beds: a tense one for the Attack and one that resolves for the Defense, at
         0.14 under the voice. Effects only where there is a cause on screen: the game's own
         alarm on the alert, the game's phone (rings without an answer / answered), a glitch on
         the intrusion, the shield on the firewall, one riser into an impact on «US$ 0 perdidos».
BANNED:  images that are not from our game; text that moves while it is being read; corner logos;
         decorative glows added on top of the footage; three cards in a row; subtitles repeating
         the voice word for word.
QA:      the title (0–4 s), the alert, the evidence, the phone with no answer, the attack result,
         the defense panel, the firewall, «US$ 0 perdidos» and the closing card.
