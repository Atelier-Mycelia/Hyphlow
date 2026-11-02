# Hyphlow Migration Notes

What you need to do when updating Hyphlow, and what may behave differently afterwards.
Newest changes come first. If you're skipping several versions, read every section between
your current version and the new one.

Each change says one of:

- **Action needed:** something you must do, or your project may break.
- **Check:** nothing breaks, but something works differently, and you may want to confirm
  it still does what you expect.
- **None:** listed only so you know it changed.

Hyphlow keeps older serialized data loading (`FormerlySerializedAs`, `MovedFrom`). So unless a
section says otherwise, existing Flowcharts, Blocks, Commands and Variable Sets load without
changes.

---

## Unreleased (after 0.7.1)

### Variable Sets are restored more thoroughly after Play Mode

**Check.** Changes Play Mode makes to Variable Sets have always been undone when it ends. That's
now done by one editor module (`VariableSetPlayModeSnapshots`) instead of each set backing
itself up to PlayerPrefs. What's different:

- **More is undone.** Variables added or removed during Play Mode, reordered variables, and the
  set's own settings (**Include in Saves**, **Include in Registry**) are put back too. Before,
  only the values of variables that existed when Play Mode started were.
- **It happens a little later.** Restoring now waits until the editor is fully back in Edit
  Mode, after the scene's objects have shut down. Before, it happened as Play Mode began to end,
  so a script that changed a variable in its `OnDisable` or `OnDestroy` could undo the restore.
  If you were relying on such a script's changes sticking, they no longer will.
- **Crashes are recoverable.** The backup is kept in `Library/Hyphlow/` until Play Mode ends
  normally. If the editor crashes mid-Play Mode, Hyphlow asks you at the next startup whether to
  restore your sets from it.
- **Unchanged sets are left alone,** so ending Play Mode no longer marks every set as modified.

Older versions may have left entries in your project's PlayerPrefs with keys like
`<set id>_<variable id>`. Nothing reads them anymore and they're harmless. If you want them gone,
*Edit > Clear All PlayerPrefs* removes them, along with every other PlayerPrefs entry the project
has made in the editor.

### For scripters: Play Mode hooks moved out of `VariableSet`

**Action needed** only if your code uses either of these:

- `VariableManager.OnPlayModeStateChanged` now only exists in the editor, since it uses an
  editor-only type. Wrap any calls to it in `#if UNITY_EDITOR`. (Nothing in Hyphlow calls it.)
- `VariableSet.EditorSetSubs` no longer subscribes to anything. It still exists, so subclasses
  that override it keep compiling. If your override relied on the base version handling Play
  Mode restores, it doesn't anymore; `VariableSetPlayModeSnapshots` does.

### Player builds no longer trip over editor-only code

**None.** Several runtime scripts used editor-only APIs without `#if UNITY_EDITOR`, which kept
player builds from compiling: `VariableSet`, `VariableManager`, `VariableRegistry`,
`VariableTypeDiscovery`, `Block` and `Flowchart`. They're guarded now. Overrides of `OnValidate`
in `VariableSet` subclasses are unaffected.

Two of them also needed to *work* in builds, not just compile:

- **The variable registry now runs in builds.** It used to start only in the editor, so in a
  build, `{$VariableName}` substitution could never find a Variable Set's variables. A build can
  only find the Variable Sets that are in a `Resources` folder (such as
  `Assets/Resources/Hyphlow/VSA`, where the Control Panel keeps them), plus any that a loaded
  scene references.
- **Variable types are found by reflection in builds,** since the editor's faster `TypeCache`
  isn't available there. If you use managed code stripping, check that your custom variable
  types still show up in a build.

### Adding a variable no longer announces it twice

**Check**, if you write code against Hyphlow's variables. Adding a variable through
`AddNewVariable` (which `VariableSet.AddNewVariableOfContentType` uses) raised the
`VariableAdded` event twice. If the variable turned out to be a duplicate and wasn't added, it
still raised it once. It's now raised once, and only when a variable is actually added. If your
code counted these events, or worked around the duplicate, adjust it.

### Variable Sets can now be managed from the Control Panel

**None.** *Window > Atelier Mycelia > Control Panel > Hyphlow > Variable Sets* is now the
recommended way to create, rename, duplicate, delete and edit Variable Sets.

- New sets are saved to `Assets/Resources/Hyphlow/VSA`.
- Sets that are elsewhere in your project keep working where they are. The panel marks them as
  **Outside** and offers to move them, but never moves anything without you asking. Moving a set
  keeps references to it intact.
- Sets in subfolders of `Assets/Resources/Hyphlow/VSA` are left where they are.
- **Nothing reaches your project until you click Save.** Creating, renaming, duplicating,
  moving, deleting and editing a set's variables are all staged in the panel first. Sets with
  staged changes are badged in the list (**New**, **Edited**, **Moving**, **Deleting**), and
  **Cancel** or **Reinit** throws the staged changes away. Deleted sets go to your system's
  trash, and Save asks you to confirm before it does that.
- During Play Mode the panel is read-only, because changes made to Variable Sets in Play Mode are
  reverted when it ends. Staged changes can't be saved then either.
- Entering Play Mode with unsaved changes in the panel asks whether to save them first, since
  entering Play Mode reloads scripts, which discards anything that's only staged.

Avoid editing Variable Sets through Unity's regular Inspector unless you know what you're doing.
Changes made there bypass the panel, so if the same set has staged edits in the panel, saving
them overwrites the Inspector's changes.

### Variable Sets now show up in variable pickers and text substitution

**Check.** A bug left Hyphlow unable to find any Variable Sets for its variable registry, so
sets marked **Include in Registry** were effectively ignored. That's fixed.

As a result, variable-select popups list those sets' variables, and `{$VariableName}`
substitution can now resolve to a Variable Set's variable where it previously found nothing.
If some text was relying on a placeholder staying unresolved, check it. To keep a set out of
both, turn off its **Include in Registry** toggle.

### Variable Sets have a new hidden field

**None.** Variable Sets now serialize an extra hidden flag (`_isDetached`), which defaults to off.
Existing assets load as before. The field may show up in your version control diffs the next time
a set is saved.

### For scripters: detached copies and applying state

**None**, unless you write code against `VariableSet`. These are new APIs (no existing ones changed):

- `VariableSet.CreateDetachedCopy()` makes an in-memory copy that shares the original's
  UniqueId, but raises no `VarSetSignals` or `VariableSignals`, and is ignored by Play Mode
  backups. Destroy it when you're done with it.
- `VariableSet.CreateDetached()` makes a new, empty set that's detached from the start, for sets
  that don't exist as assets yet.
- `VariableSet.IsDetached` tells you whether a set is such a copy.
- `VariableSet.ApplyStateFrom(VariableSet source)` (editor only) makes a set's settings and
  variables match another's. Variables both have, matched by ItemId and type, keep their
  existing instances. Ones only the source has are added, and ones it lacks are removed. Order
  follows the source. The target's name, UniqueId and asset file are left alone. It marks the
  set dirty but doesn't save it.
- `VarSetSignals.StateApplied` is raised after `ApplyStateFrom` changes a set.

A copy made with plain `Instantiate` is *not* detached, and behaves the way it always has.

---

<!--
Template for new entries. Put new versions above older ones.

## <version> (<release date>)

### <What changed, in a few words>

**Action needed / Check / None.** What the user will notice, and why it changed.

Steps to migrate, if any:

1. ...
-->
