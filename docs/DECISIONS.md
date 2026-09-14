# Decisions

Why things are the way they are, for choices that cost real time to make and would otherwise be
re-litigated. Not a description of the code: the code says what it does, tests say what must stay
true, and this file says what was chosen and what was turned down.

**How to write an entry so it cannot go stale.** Each one is a dated record of a decision, in the
past tense. It names the thing decided in plain words rather than by file, type or member, because
those get renamed. It records the relationship or the reason rather than a tuned number, because
numbers move and the test that pins one is where it belongs. Entries are never edited to match new
code: a decision that has been reversed gets a new dated entry saying so, and the old one stays as
the record of what was believed then.

---

## 2026-09-14 — A towed cart on a machine that does not own it is moved by its couplings

Three mechanisms were moving it at once: wheel physics running on every machine, couplings engaged
on every machine, and a correction impulse applied to every body in the train. Chosen: the leader
is corrected and the rest of the train follows through the couplings, exactly as on the machine
that owns it. Wheel physics and couplings stay on every machine.

Rejected: correcting every member independently, which needs a report per vehicle and makes each
coupling an argument between the solver and the network. Rejected: kinematic remote carts driven
from the leader's transform, which is cheapest but cannot take part in a collision, and cross-player
collisions are the thing this whole layer exists to preserve.

Measured before choosing, because the codebase contained four confident and contradictory claims
about it. Over ten seconds of correction against a real error, leader-only and all-bodies closed the
gap equally well and the couplings held the train's shape exactly in both cases -- the gaps between
members did not change at all. The claim that correcting only the front makes a train wander was
false with couplings engaged.

## 2026-09-14 — A copy is never moved outright, however far out of step it is

It used to be blended below two metres and teleported above. Chosen: always blended, with the
closing speed capped.

The teleport existed because the blend had no ceiling: it asks for a closing speed proportional to
the error, so a large error asked for a speed no vehicle could survive, and jumping was the lesser
evil. Capping the closing speed removes the reason for it. A body that arrives somewhere without
having travelled arrives inside whatever was standing there and takes no part in the collision it
should have had, and this project has already spent time on what deep interpenetration does.

The cost is accepted: a badly diverged copy is visibly sliding back into place for a second or two
rather than arriving. A copy that gets far enough out for that to be ugly is a divergence bug to
find, not a thing to hide.

## 2026-09-14 — Nothing on the apron is put to sleep by us

Parked trains used to be put to sleep deliberately, and a sleeping vehicle's suspension was skipped
so that the force holding it up would not wake it again. That left no spring under a parked train on
the step something struck it: the same knock drove a sleeping cart twice as deep as an awake one,
and the springs threw it back out afterwards.

Bags are not allowed to sleep either. A sleeping body is frozen in whatever pose it had when it went
quiet, including one it was halfway through falling out of a cart in.

What keeps a parked train still instead is its tyres -- see the entry on holding at a crawl. The two
decisions depend on each other and neither is safe alone.

## 2026-09-14 — A tyre holds at a crawl instead of reading its grip curve

Sideways force comes from a grip curve read at the speed the contact patch is sliding, and that
curve passes through the origin: the slower something drifts, the less there is to stop it, so
nothing ever quite stopped and a nudged cart wandered until a sleep timer ended it.

Below a crawl a tyre now holds with what it can grip with, bounded so that it can never reverse a
slide. The crawl is taken out over several steps rather than one: each wheel knows only its own
contact patch, and four of them cancelling their own slip in the same step overshoot a slow turn
between them.

The threshold is set below what a wheel sees mid-corner. Set too high it starts resisting the turn
itself, which showed up as a train coming round noticeably less on full lock.

## 2026-09-14 — What a person's feet grip and how fast they walk are two figures

One number used to be both, and they pull in opposite directions: high enough for controls that
answer at once is high enough that nothing can shake a rider off a cart, and low enough to be honest
about friction is a walk that wades.

Grip is the real one and is what a corner has to beat. It is set above what a tractor can pull away
at, so a launch keeps its riders, and below what a cart makes cornering hard at speed, so a corner
takes them. Gait is a game-feel figure and applies only while the feet still have the surface: once
it has out-pulled them, walking does nothing until they are moving with it again, which is also what
happens to a person skidding.

Riders being thrown off a cart is a requirement of the game, and the first attempt at snappier
controls quietly traded it away.

## 2026-09-14 — A bag's friction beats the deck's, and a person's feet beat everything

Both use the friction combine mode that takes the lower of the two materials rather than averaging
them, deliberately and for different reasons. A bag's grip is the dial that decides how hard a
corner has to be to throw a load, so the bag's figure has to be the one that counts rather than
being averaged with whatever it happens to be lying on. A person's capsule is frictionless because
the legs are the only thing that should move them: a capsule with friction of its own fights every
step they take and holds a rider through a corner their legs could not.

## 2026-09-14 — Couplings are anchored where the two hitches meet, and not preprocessed

Both ends of a coupling are anchored at the midpoint of the two hitch heights. The two halves of a
real coupling sit at different heights on purpose, and anchoring each end at its own hitch asks the
solver to close that gap -- which it does by tilting both vehicles, and a leaning vehicle's
suspension pushes sideways. That is what made parked trains walk across the apron.

Joint preprocessing is off because the solver silently gives way on joints it considers
over-constrained, which in a chain shows up as couplings that let go under load.

## 2026-09-14 — Geometry lives with the model, not in the profile

A vehicle's profile says how it drives; where its parts are and how big they are is measured off its
model when its prefab is built. A wheelbase written down twice is how the invisible suspension
probes ended up fourteen centimetres from the visible wheels, and a wheel radius written down twice
is the same fault waiting for a vehicle whose axles carry different wheels -- which the tractor has.

## 2026-09-14 — Sources carry no comments

Decided by the repo owner. Facts that must hold are assertions or tests; decisions worth keeping are
entries here; everything a comment used to say about what the code does is the code's job to say
through its names. Tooltips give units and range only.
