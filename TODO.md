# Project TODO List

## Office
- **Features to Implement**:
  - Flashlight functionality
  - Powerout + Ultimate Custom Night (UCN) power mode
  - Office stare mechanic

## Cameras
- **Features to Implement**:
  - Signal interrupted visual/audio cue
  - Music box
  - Blip effect
  - Camera static effect

## Menus
- **Features to Implement**:
  - Missing elements for Custom Night (CN)
  - Static visual effects

## Engine
- **Development Tasks**:
  - Add all sounds (e.g., animatronic movement, jumpscares, stare audio)
  - Complete scripting API
  - Implement real data values
  - Add Lua scripting support
  - Implement `%game` and `%ai` expressions for customization
  - Extensive cleanup and polishing of the engine

---

## Bug Fixes
1. **Office**:
   - Fix `Foxy` starting at `Foxy:peek` before reaching `change_state`.
   - Resolve `UpdateOffice` issue that prevents animatronic office interactions (e.g., office light visibility).
2. **Cameras**:
   - Ensure previous camera state updates correctly when an animatronic moves.
3. **Office (First-Time Camera Usage)**:
   - Fix single office frame issue occurring the first time cameras are accessed.
4. **Menus**:
   - Resolve arrow button flicker while animations are playing.
5. **Rendering**:
   - Ensure sprites above the panorama are rendered correctly across all elements.

