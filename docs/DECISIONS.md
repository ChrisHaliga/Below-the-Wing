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

## 2026-09-15 — A crash cannot free a port, so nothing is allowed to depend on one

A player reported that closing the game unexpectedly left it holding their port with no way to shut
it. The obvious reading is that the socket leaks, and it does not: an operating system frees a
process's sockets when the process dies. A port still held means a process is still alive, and no
code inside a dead one can run, so there is no shutdown event that fires on a crash.

So the fix is in two halves, and only the second one survives a crash.

The first half closes the session on every exit that does run code. Quitting shuts the transport
down, and where a session was created through the multiplayer service, the quit is refused once
while the service is told the player has gone, then allowed through when that finishes. It is also
allowed through after a deadline whether the service answered or not, because a game that cannot be
closed is the complaint being answered and nothing may hold a quit open indefinitely. The same
closing runs when the thing holding the session is destroyed, which is what leaving play mode does.

The second half removes the dependency. A session started alone asks the machine for a port instead
of taking the one it was built with. Nothing dials into a session running alone, so the number was
never needed, and a copy of the game still alive can no longer be in the way of the next launch.

Turned down: picking a free port by probing for one. Between the probe and the bind, something else
can take it. Asking for port zero has no such gap.

Given up: a well-known port to be dialled directly by address, with no service and no relay. Nothing
in the game dials one today.

Not solved: a session left behind in the multiplayer service by a host that died without leaving.
It expires on the service's own timeout, and nothing in a dead process can shorten that. Finding and
clearing one at the next launch would be the fix, and it was not done.
## 2026-09-15 — A cart is measured from the doors that close over its load space

The cart carried the last set of figures in the project that were typed rather than measured: its
deck top, width and length, the clear height inside, and the size and centre of its envelope. The
entry of 2026-09-14 recorded them as the one exception left, and any re-model invalidated them.

The re-exported cart has doors, and a door is exactly the boundary of the space it closes over. The
union of the four door panels gives the deck top as their lowest edge, the roof underside as their
highest, the clear height as their height, and the deck's width and length as their span. Derived
that way the deck top comes out at 0.47 against the 0.4727 that was typed, and the length at 3.14
against 3.1538.

The envelope is the union of every mesh, less the coupling hardware. A drawbar reaches more than a
metre past the body, and the layout already spaces vehicles by the coupling markers, so counting the
drawbar in the envelope as well would space them by it twice. Left in, the cart measured 5.18 m long
instead of 3.56.

Two figures are still typed, the lip's height and its thickness, because no part of the model
describes a lip. That is the whole of what remains.

Two things the builder had to learn. A part may be found anywhere inside a model rather than as a
direct child of its root, because this export wraps everything in a node and the next one may do
something else again; a path still works, a bare name works, and an ambiguous name is refused rather
than guessed. And a part may be a skinned mesh rather than a mesh filter, because a door that opens
on a shape key has to be skinned.

---

## 2026-09-15 — The tractor's launch is a feel figure and outranks the tests that argued with it

Decided by the repo owner, in those words: press the gas, feel the tractor shoot forward and really
jerk you forward, and realism does not enter into it.

Two tests stood in the way and both encoded an opinion nobody had asked for. One said a launch still
boosting at cruise is not a launch but a different engine. The other required full drive force and no
more at half of top speed. Both were written here, not requested, and both are gone.

The tractor now leaves the line at about seventy metres a second squared, a little over seven times
gravity, holding above sixty for the first tenth of a second and lifting its own nose five and a half
degrees. No tug on any apron does this. A test says so in its own failure message, so that nobody
later reads the figure as a measurement and tunes it back down to something a real machine could do.

The shove is a multiplier on drive force that fades out by six tenths of top speed, and it scales with
throttle, so feathering the pedal gives a proportionally smaller kick rather than none.

Known and accepted: a rider standing on a towed cart is thrown by every launch, because feet grip at
ten metres a second squared. That was already true at twenty-four and is recorded above.

Two test fixtures had to grow. They built two hundred and four hundred metre aprons, which were large
enough only because a tractor took so long to get going; a train that now reaches nineteen metres a
second drove off the edge and the fall read as a runaway. The assertions were untouched and the
ground was made to outlast the vehicle. A third fixture cornered a train at four tenths throttle,
which used to be a towing pace and is now brisk, so it corners at two tenths instead and the test
measures the towing geometry it was written for.

---

## 2026-09-15 — What a driver feels is the first tenth of a second, not the settled corner

The entry below chose a tyre by sweeping settled full-lock corners and picked the curve that came
round tightest. Driving it was worse, and the report was that the tractor turned less at speed, not
more.

Both measurements were of the same thing and it was the wrong thing. A settled corner is where a
vehicle has found whatever slip angle balances the force it needs, so a tyre that makes little at
small slip simply runs at a larger slip angle and arrives at nearly the same radius. Measured on the
same tractor, the old tyre and the new one settle within a couple of degrees a second of each other
at every steering input. Nothing in that number could have shown the fault.

The number that shows it is how fast the vehicle answers the wheel. A tenth of a second after a small
input at twenty metres a second, the chosen tyre was coming round at ten degrees a second and the one
it replaced at thirty-three. It took three times as long to begin doing what it was told, and it
overshot harder and snaked more once it did. That delay is what a driver calls not being able to
turn.

So the curve keeps the peak that made a hard corner work and regains its bite early, by reaching most
of its force by three metres a second of slip rather than eight. It still falls away after the peak,
so a tyre can still let go.

Turned down: a curve that bites earlier still. It barely drops after its peak, which means it never
breaks traction, and it left the test that proves a tyre can let go passing on a two per cent margin.

The lasting part of this is the method, not the numbers. A steady-state measurement cannot see a
transient fault, and a vehicle is driven in transients.

---

## 2026-09-15 — A corner is bought with grip, not with steering lock

The tractor would not change direction at speed. The obvious lever looked like the lock allowed at
speed, so a sweep measured a settled full-lock corner across three locks and four tyre grip peaks.

More lock made the circle wider, not narrower. At the shipped grip, raising the lock allowed at top
speed from thirty degrees to sixty took the radius from thirty-nine metres to sixty-seven, because
the extra wheel angle only drags the front tyres further past the slip they grip hardest at. The
radius a vehicle can hold is its speed squared over the sideways acceleration its tyres can make,
and no steering angle adds to that.

The tractor's grip curve peak was raised by half. Measured on the same tractor at full throttle and
full lock, the corner settles at twenty-two metres instead of thirty-nine, the yaw rate goes from
twenty-five degrees a second to forty, and it holds fifteen and a half metres a second through the
corner rather than scrubbing down to walking pace. A test pins both halves of that, because a tight
circle bought by losing all the speed is a handbrake turn and not a corner.

Turned down: a peak high enough to corner in five metres. It does corner in five, by scrubbing from
twenty metres a second down to six, and that grip then applies at every other speed too.

Left open, and the reason the entry stops here: the top speed itself. Seventy-two kilometres an hour
is several times what a real baggage tractor does, and radius goes with the square of speed, so
cutting it would tighten every corner at no cost in grip. That is a decision about how the apron
feels to cross, and it was not made.

---

## 2026-09-15 — The shape of steering lock against speed is unsettled

Two tests disagree and both are kept failing rather than picking a winner by default.

One says the wheels go all the way over at walking pace, because lining a tractor up on a hitch wants
every degree there is. The other says the lock sheds an even number of degrees across each quarter of
the speed range, because a limit that collapses early leaves a driver with a wheel that stops
answering the moment they are moving.

A lock that falls evenly from its full value at rest to a reduced one at top speed cannot give the
whole lock at any speed above rest. Holding the full lock through a low-speed band satisfies the
first and breaks the second. There is no shape that satisfies both, so one of the two states an
intent that is not actually wanted, and which one is a question for the repo owner.

Until then the even falloff ships and the crawl test stays red as the record of the open question.

---

## 2026-09-15 — Hauling yourself up goes up first and in second

Holding something with both hands and asking to be lifted used to raise the body straight up, which
left a person rising alongside a cart and never crossing into it.

Driving the body up and inward at the same time is worse, and measured so: the horizontal part
presses the body against the very thing it is gripping, the capsule jams under it, and a climb that
reached two and a third metres unaided rises four inches and stops.

So the haul climbs vertically while the feet are below what the hands hold, and only closes the
horizontal gap once the feet are clear of it. That is the order a person uses, and it is the order
that works against a solid edge.

The crouch outlasts the haul. It is released when the body is down on something rather than when the
key comes up, because a body that stretches to full height in mid air meets whatever is overhead.
The previous climb ended with a head against a cart roof for that reason.

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

## 2026-09-14 — The apron scene is saved twice, and its object identities move each time

A networked object is known to other machines by an identity worked out from where it lives in a
file. Building the scene in memory and saving it once cannot produce that: at the moment of the
save the object has no file to live in, so the identity comes out as zero and zero is what is
stored. Netcode refuses to spawn an object whose identity is zero, so the session never starts, the
apron is never built, and a player is left on bare ground.

None of that was ever seen in play, because opening the scene in the editor works the identity out
and holds it in memory. Only a built player reads what is actually on the disk. The scene is
therefore saved, reopened so the identities can be worked out, and saved again -- and the check
that guards it reads the file as text, because a check that opens the scene repairs the fault
before it looks at it.

Accepted, not solved: the identities are different every time the scene is rebuilt, because the
file positions they are derived from are not the same twice. Within one build that is harmless --
every machine reads the same file -- but two builds of the scene do not agree with each other, and
the whole scene file churns in the repository each time it is regenerated.

That churn is the reason the scene already in the repository was repaired by opening and saving it
rather than by rebuilding it. Rebuilding is a regeneration of everything, and the one attempt at it
came back with the aircraft and crew profiles unassigned while every prefab reference survived --
which the shipped-scene checks caught, and which is the whole argument against regenerating an
asset to change one field in it.

## 2026-09-14 — A machine is solid in the pieces it was modelled from

A tractor used to collide as one box drawn round its whole bodywork: solid from just above the
tarmac to above head height, the full length of the machine. There was nothing on it to stand on,
no gap between the axles to walk through, and the open sides of it were a wall. The box was not a
simplification of the shape, it was the absence of one.

What a vehicle is solid in is now a list of parts where a part is either a box or a named piece of
its model taken at its own shape, and a tractor names the pieces that make it solid -- its shell,
frame, supports, the platform over its back wheels, the seat and its back, the dashboard. The
platform is how somebody gets on, and it is a surface now rather than the lid of a block. Re-model
any of it and the collision follows without anybody editing code.

A cart stays boxes. Its deck, lips, ends and roof are slabs, and more to the point they are the
description of a load space rather than a drawing of one: the space a cart keeps clear for cargo is
the one thing about it that has to be stated rather than inferred from whatever the bodywork mesh
happens to be.

## 2026-09-14 — The front wheels go as far as the tyres still bite, and no further

A tyre makes its sideways force out of sliding a little. Slide it much further and the force falls
away again, which is the falling half of the grip curve that lets a vehicle break traction at all.
At full lock and speed the front tyres were being dragged sideways four times harder than the slip
they grip hardest at, so they were pushing with less than half of what they had -- and every extra
degree of lock bought less turn than the one before it. Wound fully over, a tractor at speed took a
hundred and thirteen metres to come round, with its wheels pointing somewhere it was not going.

The wheels now go as far as they are asked or as far as they can go while the tyres still bite,
whichever is less. That limit is worked out from where the vehicle's own grip curve peaks, so a
retyred vehicle gets a different one without anybody choosing a number. Measured on the same tractor
at the same speed with the same input, the lock settles at nine degrees instead of forty-five and it
comes round three times faster; the straight-line speed is unchanged.

Nothing about this adds grip. It stops the steering from throwing away the grip already there, and
at a crawl -- where a tyre barely slides at all and a tractor is being lined up on a hitch -- the
wheels still go all the way over.

Not solved, and a separate question: grip alone holds a tractor at this speed to a sixty-seven metre
circle however well it steers. Only the speed itself moves that, and a test records that the top
speed is a deliberate choice.

## 2026-09-15 — A launch now out-pulls what a rider's feet can hold

The entry of 2026-09-14 on grip and gait set a person's foot grip at 10 m/s^2 on the grounds that it
sat above what a tractor could pull away at, so a launch kept its riders, and below what a cart makes
cornering hard at, so a corner took them. A drive force multiplier of three off the line puts a
standing start at about 24 m/s^2, which is above both. A rider on a cart is now thrown by the launch
as well as by the corner.

That was not chosen, it fell out of the request for harder acceleration. The two wants are no longer
satisfiable by one figure: a corner pulls 13 to 23 m/s^2, so any grip high enough to survive a
24 m/s^2 launch also survives every corner, and riders stop being thrown at all. Whichever of the two
matters more is a decision for the repo owner, and until it is made the launch wins because it is the
one that was asked for.

Reversed from the earlier entry, which stands as the record of what was believed then.

## 2026-09-14 — Sources carry no comments

Decided by the repo owner. Facts that must hold are assertions or tests; decisions worth keeping are
entries here; everything a comment used to say about what the code does is the code's job to say
through its names. Tooltips give units and range only.

## 2026-09-16 — A thing built wrong throws, and does not go on running

Decided during the audit fix pass. Nine places logged an error about a missing profile, shape,
coupling, broker, session or hand anchor and then carried on: some disabled themselves, some
returned a default, some simply continued with the broken object. Three of them left a vehicle
running its physics against state it never finished building.

All of them now throw one exception type naming the object and the part it lacks. A component
that throws out of its own configuration disables itself first, so nothing runs FixedUpdate on a
half-built thing; the exception then surfaces once, with the object as context, which is the one
report the person fixing the prefab needs. Nothing else catches it, because nothing downstream can
act on a mis-built prefab.

What this rules out: a default standing in for a part that is missing. A vehicle without a seat
marker is refused, not seated at its origin. A missing bag prefab is refused, not silently no bags.
The shipped content is guarded by the prefab tests; the throws are what catches content the tests
have not seen yet.

## 2026-09-17 — One reach and one cone, on the crew member

Decided during the audit fix pass. The distance a player could interact over was
written as 3 m in three places and the cone as 40 degrees in four, one of them a
serialized field on the crew prefab. Setting the field moved the seat offer and
left parking and hitching on the old figure, so one key reached two different
distances.

Both now live on CrewCharacter and everything that aims reads them from there:
the seat, the coupling hand, the cart brake. VehicleOccupancy takes the aim, the
reach and the cone as constructor arguments rather than as settable properties,
so there is no state in which it has a reach but no aim.

What this removed: the nearest-vehicle fallback. VehicleOccupancy used to pick
the closest driveable vehicle when no aim was supplied, and nothing in the game
ever supplied no aim, so the fallback was reached only by its own tests while
the cone-then-raycast path the game runs had no coverage at all. DriverPrompt,
which existed to serve it, is gone.

The rule that stands: what a player interacts with is whatever their camera ray
hits inside the interaction distance, and otherwise the thing closest to the
centre of their view cone. Distance from the body decides nothing.

