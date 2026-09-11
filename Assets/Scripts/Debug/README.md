# BelowTheWing.Debug

Everything that exists to exercise the game rather than to be the game: test rigs, sandbox
controls, and anything else shaped like a scene somebody set up to watch a behaviour.

This assembly may reference any runtime assembly. **No runtime assembly may reference it.** Nothing
outside it is allowed to know that a rig exists, what radius it drives, or what its buttons are
called — and because the reference only goes one way, that is a compile error rather than something
everyone has to remember.

The rule it enforces: a component in a runtime assembly should still make sense if every sandbox
scene were deleted. If it would not, it belongs here.
