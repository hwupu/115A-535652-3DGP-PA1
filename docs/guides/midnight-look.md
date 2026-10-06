# Midnight Look Guide (URP, Unity 6000.3)

Goal: a moonlit Halloween night with a dark-blue mood, distance fading into fog, a soft blur on far objects, and glowing ghosts and pumpkins.
Starting point (checked 2026-10-06): skybox `Midnight` ✓; fog **off**; **no** post-processing Volume; post-processing **off** on the cameras; the Directional Light is near-white at intensity 1, which is why it still looks like daytime.

Do the steps in order and press Play after each one. Numbers are starting points; tune by eye.

---

## 1. Moonlight (biggest effect, 2 min)
Select `Environment/Lighting/Directional Light`:
- **Color:** pale blue, e.g. `#8FA8FF`
- **Intensity:** `0.3`–`0.5`
- **Rotation:** low and from the side, e.g. X `25`, Y `-40`. Long shadows read as night.
- **Shadow Type:** Soft Shadows; **Strength** `0.6`

## 2. Ambient light and reflections (Lighting window, 2 min)
**Window → Rendering → Lighting → Environment** tab:
- **Environment Lighting → Source:** `Color`; **Ambient Color:** dark navy, e.g. `#1A2238`. With `Skybox` as the source, the bright parts of the sky light the scene too much.
- **Environment Reflections → Intensity Multiplier:** `0.3`

## 3. Fog: distance fades into darkness (1 min)
Same Lighting window, **Environment → Other Settings**:
- **Fog** ✔, **Mode:** `Exponential Squared`
- **Color:** dark blue-grey matching the horizon of the skybox, e.g. `#1C2333`
- **Density:** `0.02`–`0.035` (higher = shorter view; at 0.03 objects fade out around 50–60 m)

Fog is cheap and is the main "midnight" tool: it hides the far walls and makes the arena feel larger. The minimap is unaffected, because the minimap camera turns fog off for its own render.

## 4. Post-processing Volume: blur, glow, color grade (5 min)
1. **Main Camera → Camera → Rendering → Post Processing** ✔. Leave it **off** on `MinimapCamera`.
2. Hierarchy: **right-click → Volume → Global Volume**, rename it `PostProcess`, and put it under `Environment/Lighting`. Click **New** next to *Profile*; it saves `PostProcess Profile` next to the scene.
3. **Add Override** for each of these:

| Override | Settings | What it does |
|---|---|---|
| **Depth Of Field** | Mode `Gaussian`; Start `15`; End `45`; Max Radius `1` | **Distance blur**: sharp near the witch, soft far away |
| **Bloom** | Threshold `0.9`; Intensity `0.6`; Scatter `0.7` | Emissive ghosts and pumpkins glow |
| **Color Adjustments** | Post Exposure `-0.3`; Contrast `15`; Saturation `-15`; Color Filter slight blue `#D6E0FF` | Night grading |
| **White Balance** | Temperature `-20` | Colder, moonlit tone |
| **Vignette** | Intensity `0.3`; Smoothness `0.4` | Darker edges, a spookier frame |

Notes:
- Depth Of Field **Gaussian** blurs by distance only. **Bokeh** blurs both near and far and costs more.
- In **first person**, objects within 15 m stay sharp. Lower Start/End if you want more blur.
- The HUD (Screen Space Overlay canvas) is never blurred.

## 5. Optional: warm light sources (contrast sells "night")
- Open a pumpkin or ghost material → **Emission** ✔, orange or pale green. Bloom (step 4) makes it glow; this costs nothing.
- A few **Point Lights** (orange, Range `6`, Intensity `2`) on wall corners or the Guardian Platform. Keep it to **≤ 8 in view**; URP Forward limits lights per object.
- The Guardian Platform: a cyan Point Light above it makes it the "source of balance" from the story.

## 6. Check
- Press Play: the minimap is still readable; ghosts are visible within throw range (40 m). If they aren't, reduce the fog density.
- **Build** (Boomerang Guardian → Build → macOS) and check once in the app. Post-processing and fog behave the same, but brightness can differ on another screen.
- Take the "after" screenshots for the report here.

> Want this automated? Tell me the values you like and I can add a **Setup → Midnight Look** menu item that applies them, but it's your art call. Doing it by hand teaches the settings for the TA demo.
