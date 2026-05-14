[![hyphlow-Logo.png](https://i.postimg.cc/02S7Jmr6/hyphlow-Logo.png)](https://postimg.cc/56xXhXB1)

# 🌱 Hyphlow: Alchemic Logic Engine
A community‑driven evolution of [Fungus](https://github.com/snozbot/fungus/)‑style visual scripting.
Hyphlow is a free, open‑source visual scripting system built as a modern evolution of the Fungus workflow. It keeps the spirit and accessibility of its predecessor while updating the architecture, UI, and extensibility for today’s Unity ecosystem.
If you’re comfortable with Fungus, you’ll feel right at home. However, Hyphlow isn’t a drop‑in replacement. Dialogue, localization, and other such features will be released as separate plugins, keeping the core clean and modular.
We’re currently in open beta, and we welcome feedback, issues, and contributions. Also, feel free to contact us at atelierMycelia@gmail.com.

# ✨ Key Improvements Over Fungus
- Modernized codebase
  - Cleaner subsystems, clearer architecture, and more predictable behavior
- UI-Toolkit-Based Editor Improvements
  - A more stable, scalable editor experience
- Built‑in tweening system 
  - Since due to asset store policies, we can't simply package it with a third-party sys like LeanTween.”
  - This sys also works as a bridge to make it easier to create bindings letting you use stuff like 
        LeanTween and DoTween. In fact, we have those in the works!
- Supporting Unity 2022.3.30 and Unity 6+

# 🧪  Unity Versions Tested On
- 2022.3.30f1
- 6000.0.67f1

# 📦 Installation

## Unity 2022.3 (UPM from disk)
1. Download a release from the Releases page
2. Extract it into a folder you use for on‑disk UPM packages
3. Open your Unity project
4. Go to Package Manager
5. Click + → Add package from disk
6. Select package.json inside AtMyceliaCommonLib
7. Repeat for the package.json inside Hyphlow

## Unity 6+ (Git URL)
Add these two packages via Package Manager, going with Add Package From Git Url instead of Add Package From Disk:
1. https://github.com/Atelier-Mycelia/UnityUtils.git?path=Assets/AtMyceliaCommonLib
2. https://github.com/Atelier-Mycelia/Hyphlow.git?path=Assets/Hyphlow

# 🤖 AI Use Disclosure
We use AI for:
- Debugging
- Small, repetitive tasks
- Architecture improvements

# 🙏 Acknowledgements
- The developers and contributors of [Fungus](https://github.com/snozbot/fungus/) and [Fungus Community Edition](https://github.com/Atelier-Mycelia/Amanita)
- The Fungus Community
