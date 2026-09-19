# Tools

Three PowerShell scripts, one shared helper they dot-source, and four Unity menu items. Nothing here is needed to open the project and
press Play; everything here exists because the Unity editor takes an exclusive lock on a project,
so a second Unity cannot be driven at it from a script while the editor is open.

Each script copies `Assets`, `Packages` and `ProjectSettings` into a mirror under the system temp
folder and runs a headless Unity there. None writes anything into the working tree except where
it says it copies results back.

| What | Where | Run it when |
|---|---|---|
| `run-tests.ps1` | here | checking a change against the suites |
| `rebuild-scene.ps1` | here | after changing `SceneBootstrap`, or after adding a serialized field the apron scene or the prefabs need |
| Below the Wing → Rebuild apron scene and prefabs | Unity menu bar | same as above, when the editor is the thing you are sitting in front of |
| `mirror.ps1` | here | never run directly; the other two dot-source it for mirroring, running Unity and copying results back |
| Below the Wing → Create missing content | Unity menu bar | on a fresh clone, if the `.asset` profiles are missing |
| Below the Wing → Give the apron scene's objects their identities | Unity menu bar | repairing scene object ids |

`Rebuild apron scene and prefabs` calls `Create missing content` itself, so running both is never
needed.

## Running the tests

```
pwsh -File tools/run-tests.ps1 -Platform EditMode
pwsh -File tools/run-tests.ps1 -Platform PlayMode
pwsh -File tools/run-tests.ps1                     # both
pwsh -File tools/run-tests.ps1 -Platform Compile   # compile only, about a minute
```

`Compile` is the one to run first after a patch. A test run with a compile error in it does not
fail; Unity sits waiting for a test runner that never starts, and the script's timeout is what
ends it. `Compile` exits non-zero in about a minute and lists the errors.

EditMode takes about a minute of Unity start-up and under a second of tests. PlayMode takes about
ten minutes, because it steps real physics.

Useful arguments:

- `-Filter <regex>` narrows the run. The default, `BelowTheWing`, is doing real work: Netcode is
  listed as a testable package so its own harness compiles, which also brings roughly 7000 of its
  tests into any unfiltered run.
- `-MirrorName <name>` picks which mirror to use. Two runs against one mirror deadlock and both die
  reporting `NO RESULTS FILE`, so use separate names to run EditMode and PlayMode at once.
- `-Bootstrap` generates missing content assets in the mirror and copies them back.

## Who owns which scene

`Assets/Scenes/Apron.unity` is generated. Its positions are derived rather than chosen: train
spacing comes from the widest vehicle's footprint plus `ApronLayoutSettings.trainSpacingMetres`,
cart pitch from each cart's front and rear reach, and the five arrival crew spawn into are probed
for clearance. `ShippedSceneTests` checks the shipped scene against those rules. Moving something
in it by hand is undone by the next rebuild.

`Assets/Scenes/Menu.unity` is yours. Open it and move things. Nothing regenerates it except
`Lay out the menu scene`, which writes it from scratch and discards whatever was placed by hand.
`MenuSceneDressingTests` checks only what the menu needs to run: three camera stations, a door
driver pointed at a cart and not riding on its prefab instance, five crew figures that each draw
something, and the belt loader and jet the backdrop names. Framing, spacing and nameplate height
are not asserted, so they are free to change.

## Rebuilding the apron scene

```
pwsh -File tools/rebuild-scene.ps1
```

This runs the `Rebuild apron scene and prefabs` menu item in the mirror and copies
`Assets/Scenes/Apron.unity`, `Assets/Content/Prefabs` and `Assets/UI` back into the project. It
does not touch `Menu.unity`.

**The editor will not notice.** Unity does not reload a scene that changed on disk underneath it.
After this runs, the open scene in the editor is the old one, and saving it writes the old one back
over the new. Reopen `Assets/Scenes/Apron.unity` before doing anything else.

That is the reason to prefer the menu item when the editor is already open: it rebuilds in place
and leaves the editor holding the result.

## The menu scene

`Assets/Scenes/Menu.unity` is authored by hand and no script writes it. The three camera stations
the menu travels between are plain transforms in it: the `Menu Camera` object's own position is the
opening frame, and `Cart shot` and `Inside shot` are the two the camera pans to. Move or turn any of
them in the editor and the menu follows, because `MenuCamera` reads their position and rotation when
a pan starts. `MenuSceneDressingTests` is what holds the scene to what the menu code reaches for.

The same caveat about the editor not noticing applies. Reopen `Assets/Scenes/Menu.unity` afterwards.

## When a mirror stops compiling with no errors in Assets

A Unity killed or crashed while extracting packages leaves `Library\PackageCache` holding a
package with its source files but none of its `.asmdef` files. Later runs trust that folder,
every source in it is ignored as being in an immutable folder with no assembly definition, and
everything referencing that assembly fails with types not found. The log shows hundreds of
`error CS` lines under `Library\PackageCache` and none under `Assets`.

`Sync-Mirror` in `mirror.ps1` checks for a package with sources and no asmdef on every run and
clears the cache when it finds one. A stale `Library\Bee` build graph fails the same way with the
cache intact, so when any run fails with errors only under `Library\PackageCache`, the script
deletes the mirror's whole `Library` itself and says so. The run after pays for one import and is
then healthy.

The scripts wait on the Unity process itself rather than with `Start-Process -Wait`. `-Wait`
also waits for every descendant, and a Unity that had to spawn its own `Unity.Licensing.Client`
leaves that child running after it exits, so a run that finished in a minute did not return for
ten.

## The Unity the scripts use

They look for `6000.5.10f1` at the default Unity Hub path. Set `BTW_UNITY` to override:

```
$env:BTW_UNITY = "D:\Unity\6000.5.10f1\Editor\Unity.exe"
```
