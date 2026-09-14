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

## 2026-09-14 — Equipment is described with real figures, and top speed is the exception

Masses, dimensions and spring rates are the real ones for the equipment being modelled, because a
spring rate only means anything next to the weight it holds up and a three tonne tractor towing half
tonne carts behaves unlike any pair of numbers that happen to feel right.

Driveline drag is set by feel instead. A real baggage tractor tops out at about 23 km/h, which is
accurate and dull to drive across an apron; the game asks for plausible rather than precise. It is
the only figure knowingly chosen against the real one, and it is written down here so that the next
person to find it does not treat the rest of the profile as equally negotiable.

## 2026-09-14 — A vehicle carries its weight low, and its suspension travel is shorter than its wheels

Both figures are relationships rather than preferences. The centre of mass is placed well below the
middle of the bodywork, measured from an origin on the tarmac: carried at the bodywork's own middle,
a tug rolls over the first time it corners with a loaded train behind it, and a cart that leans into
a corner throws its load out of itself.

Suspension travel is kept under the radius of the smallest wheel on the vehicle. Longer than that
and the vehicle is visibly floating above its own axles, because the body is held further off the
ground than the wheels holding it up are tall.

## 2026-09-14 — A tyre's grip curve has to fall away after its peak

Grip climbs as a tyre begins to slip, peaks, and then drops. The falling half is not detail: it is
the only thing that lets a vehicle break traction at all. A curve that only ever rises puts
everything on rails, and no amount of speed into a corner will make a train slide or jackknife.

## 2026-09-14 — A vehicle's solid parts stop above its wheels

The box a vehicle collides as is its bodywork, and the bodywork's underside is some way off the
tarmac. A solid part reaching the ground carries the vehicle's weight itself, and then the
suspension never compresses: what should be a machine on wheels is a crate sliding about on the
floor.

## 2026-09-14 — Nothing replicates its position through a transform component

Neither of the two components netcode offers is used on anything that can be crashed into. Both
write a position onto the copy, and a body whose position is written arrives somewhere without
having travelled, so the impulse a collision should have exchanged never happens and the crash comes
out differently on each screen. One of them goes further and makes every non-owning copy kinematic,
which is infinite mass: you drive into somebody else's tractor and bounce off a wall while the
mirror image happens on theirs.

Everything crashable reports what it is doing and is steered toward that with force instead --
people included, because players run each other over on purpose. Bags have the same problem and one
more: which machine simulates a bag changes with who picks it up and whose cart it lands in, and no
transform component has anything to say about that.

## 2026-09-14 — Every machine in a multiplayer test shares one scene

The multiplayer tests run all their machines in a single process, so there is one scene and one
physics world between them. Anything a component finds by searching the scene finds one instance
shared by every machine, so a fixture cannot stand up one session per machine: a single session
object would collect all three machines' vehicles into one impossible train.

What follows from that is a real limit, not a detail. These tests can check what is sent, what
arrives and whether the machines agree on it. They cannot measure two machines' physics diverging,
and a fixture that leaves the session out is not exercising the session -- whatever the session
does with what arrives has to be covered somewhere else.

## 2026-09-14 — A multiplayer fixture takes a port the operating system says is free

The fixtures used to take the transport's default port. The editor takes the same port the moment
somebody hosts a session in it, so the entire multiplayer suite failed at connect, intermittently,
for a reason nothing in the output pointed at -- and the natural reading of an intermittent
connection failure is a flaky test rather than a busy socket.

Every machine in a fixture now shares one port asked for and released just before the run, clients
that join part way through included. It is "was free a moment ago" rather than "is free", which is
the best a fixture can do without holding the socket it means to hand over.

## 2026-09-14 — A request for a vehicle gives up after a deadline

A request is routed to whichever machine owns the thing, and a machine that has left never answers.
Without a deadline the asking machine waits for that answer forever, holding the caller's response
handler and, with it, the seat the player was trying to take. The deadline turns a silence into a
refusal, which the player can act on.

## 2026-09-14 — The cart's deck and envelope are the one set of figures still typed in

Everything else about a vehicle is read off its model when its prefab is built. The baggage cart's
deck height, the clear space above it and the box it takes up are not: the cart's mesh merges the
deck into the rest of the bodywork, so there is no node to measure them from, and they were taken
off the model by hand instead.

That makes them the one thing a re-export can silently invalidate. Re-modelling the cart means
re-measuring them, and nothing in the build will say so.

## 2026-09-14 — Getting into a cart is a climb, and the roof is what makes it one

A baggage cart is a box with a roof on it. The way in is the gap between the lip a person steps over
and that roof, and on this cart that gap is shorter than a person is tall. Standing up, there is no
trajectory into a cart at all: every attempt drives their head into the underside of the roof, which
throws them back out and shoves the cart sideways. That is what made carts feel impossible to get
into, and it is not something a taller step or a shorter person would have fixed.

A climb is therefore two movements and a duck. They push off straight up -- hard enough to clear the
lip, or low enough that a ducked head stays under the roof, whichever is lower -- and then, at the
top of the lift when they are no longer rising, they are pulled inward fast enough to be past the
lip before they fall back level with it. Both are changes to their own velocity, so the whole thing
can fail: a cart can be driven out from under them, something can knock them off the arc, and the
pull gives up if it has not come by the time it would be pointless.

Rejected: raising how high a person can step, which is a climb wearing a disguise -- a capsule has
no step height, so it would mean lifting the body over obstacles, which is teleporting, and a step
tall enough for a cart lip would let people stroll up onto couplings and bodywork everywhere else.
Rejected: making the person shorter, which changes every vertical relationship already settled
against them -- reach, eye height, what they fit under -- to fix something their height was not
causing.

## 2026-09-14 — A person on their way up is not standing on anything

The ground stays within reach of a person's feet for the first few steps of any leap, and while
they counted as standing, their legs kept working: a jump aimed in some direction had that direction
walked back off it before they had risen a hand's breadth. Jumping looked like it worked because a
jump is mostly vertical and nobody missed the part that was being cancelled.

What counts now is whether they pushed off deliberately, which lasts until they land. Deciding it
from how fast they are rising instead would take walking up a ramp for a leap.

## 2026-09-14 — Sources carry no comments

Decided by the repo owner. Facts that must hold are assertions or tests; decisions worth keeping are
entries here; everything a comment used to say about what the code does is the code's job to say
through its names. Tooltips give units and range only.
