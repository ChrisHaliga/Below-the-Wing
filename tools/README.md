# Tools

Two PowerShell scripts and three Unity menu items. Nothing here is needed to open the project and
press Play; everything here exists because the Unity editor takes an exclusive lock on a project,
so a second Unity cannot be driven at it from a script while the editor is open.

Both scripts copy `Assets`, `Packages` and `ProjectSettings` into a mirror under the system temp
folder and run a headless Unity there. Neither writes anything into the working tree except where
it says it copies results back.

| What | Where | Run it when |
|---|---|---|
| `run-tests.ps1` | here | checking a change against the suites |
| `rebuild-scene.ps1` | here | after changing `SceneBootstrap`, or after adding a serialized field the shipped scene or prefabs need |
| Below the Wing → Rebuild apron scene and prefabs | Unity menu bar | same as above, when the editor is the thing you are sitting in front of |
| Below the Wing → Create missing content | Unity menu bar | on a fresh clone, if the `.asset` profiles are missing |
| Below the Wing → Give the apron scene's objects their identities | Unity menu bar | repairing scene object ids |

`Rebuild apron scene and prefabs` calls `Create missing content` itself, so running both is never
needed.

## Running the tests

```
pwsh -File tools/run-tests.ps1 -Platform EditMode
pwsh -File tools/run-tests.ps1 -Platform PlayMode
pwsh -File tools/run-tests.ps1                     # both
```

EditMode takes about a minute of Unity start-up and under a second of tests. PlayMode takes about
ten minutes, because it steps real physics.

Useful arguments:

- `-Filter <regex>` narrows the run. The default, `BelowTheWing`, is doing real work: Netcode is
  listed as a testable package so its own harness compiles, which also brings roughly 7000 of its
  tests into any unfiltered run.
- `-MirrorName <name>` picks which mirror to use. Two runs against one mirror deadlock and both die
  reporting `NO RESULTS FILE`, so use separate names to run EditMode and PlayMode at once.
- `-Bootstrap` generates missing content assets in the mirror and copies them back.

## Rebuilding the scene

```
pwsh -File tools/rebuild-scene.ps1
```

This runs the `Rebuild apron scene and prefabs` menu item in the mirror and copies
`Assets/Scenes`, `Assets/Content/Prefabs` and `Assets/UI` back into the project.

**The editor will not notice.** Unity does not reload a scene that changed on disk underneath it.
After this runs, the open scene in the editor is the old one, and saving it writes the old one back
over the new. Reopen `Assets/Scenes/Apron.unity` before doing anything else.

That is the reason to prefer the menu item when the editor is already open: it rebuilds in place
and leaves the editor holding the result.

## The Unity the scripts use

They look for `6000.5.10f1` at the default Unity Hub path. Set `BTW_UNITY` to override:

```
$env:BTW_UNITY = "D:\Unity\6000.5.10f1\Editor\Unity.exe"
```
