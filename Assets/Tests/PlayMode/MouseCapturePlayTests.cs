using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// The mouse pointer being taken from the desktop while somebody plays, and given back.
    ///
    /// Here rather than in the editor tests because all of it turns on a component's lifecycle --
    /// coming into existence when a player arrives on the apron, and going away with them when a
    /// session drops. An editor test run never starts either.
    /// </summary>
    public sealed class MouseCapturePlayTests
    {
        /// <summary>
        /// A pointer that remembers what was done to it instead of doing it.
        ///
        /// The real pointer belongs to the operating system, and a test run with no window cannot
        /// read back whether the cursor was hidden -- every assertion against it comes out as
        /// "free and visible" whatever the code did, which is the same answer a component that did
        /// nothing at all would give.
        /// </summary>
        sealed class RecordingPointer : IMousePointer
        {
            public bool Held { get; private set; }
            public int TimesTaken { get; private set; }
            public int TimesGivenBack { get; private set; }

            public void Hold()
            {
                Held = true;
                TimesTaken++;
            }

            public void Release()
            {
                Held = false;
                TimesGivenBack++;
            }
        }

        TestApron m_Apron;
        CrewProfile m_CrewProfile;
        RecordingPointer m_Pointer;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CrewProfile = TestProfiles.CrewMember();
            m_Pointer = new RecordingPointer();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CrewProfile);

            // A test that ended with the pointer taken would take the next test's pointer with it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// The local player turning up on the apron, which is the moment this component exists at all.
        ///
        /// Built inactive so the recording pointer is in place before the component wakes up and
        /// takes one. Switching the object on afterwards is what starts it.
        /// </summary>
        MouseCapture ArriveOnTheApron()
        {
            var go = new GameObject("Local player");
            go.SetActive(false);
            m_Apron.Track(go.transform);

            var capture = go.AddComponent<MouseCapture>();
            capture.Pointer = m_Pointer;
            go.SetActive(true);

            return capture;
        }

        [Test]
        public void ArrivingOnTheApronTakesThePointer()
        {
            var capture = ArriveOnTheApron();

            Assert.That(capture.Held, Is.True);
            Assert.That(m_Pointer.Held, Is.True,
                "a player who is on the apron is playing, and playing means looking around");
        }

        [Test]
        public void PressingEscapeHandsTheRealPointerBack()
        {
            var capture = ArriveOnTheApron();
            Assert.That(m_Pointer.Held, Is.True, "the pointer has to be taken before giving it back means anything");

            capture.React(escapePressed: true, clickPressed: false);

            Assert.That(capture.Held, Is.False);
            Assert.That(m_Pointer.Held, Is.False,
                "Escape is what a player presses to reach another window, so the pointer has to go");
        }

        [Test]
        public void TheWindowGoingIntoTheBackgroundHandsTheRealPointerBack()
        {
            var capture = ArriveOnTheApron();
            Assert.That(m_Pointer.Held, Is.True);

            capture.WindowFocusChanged(hasFocus: false);

            Assert.That(m_Pointer.Held, Is.False,
                "alt-tabbing away with the pointer still locked strands the desktop");
        }

        [Test]
        public void ClickingBackInTakesTheRealPointerAgain()
        {
            var capture = ArriveOnTheApron();
            capture.React(escapePressed: true, clickPressed: false);
            Assert.That(m_Pointer.Held, Is.False, "the pointer has to be gone before asking for it back means anything");

            capture.React(escapePressed: false, clickPressed: true);

            Assert.That(m_Pointer.Held, Is.True, "a click is how the player asks to carry on playing");
        }

        [Test]
        public void TheLocalPlayerGoingAwayHandsThePointerBack()
        {
            var capture = ArriveOnTheApron();
            Assert.That(m_Pointer.Held, Is.True);

            Object.DestroyImmediate(capture.gameObject);

            Assert.That(m_Pointer.Held, Is.False,
                "a session that drops takes the local player with it, and a player left with no game " +
                "must not also be left with no pointer");
        }

        [Test]
        public void AnUneventfulFrameDoesNotKeepTouchingThePointer()
        {
            var capture = ArriveOnTheApron();
            var takenOnArrival = m_Pointer.TimesTaken;

            for (var i = 0; i < 10; i++)
            {
                capture.React(escapePressed: false, clickPressed: false);
            }

            Assert.That(m_Pointer.TimesTaken, Is.EqualTo(takenOnArrival),
                "the pointer is touched when the answer changes, not on every frame it stays the same");
            Assert.That(m_Pointer.TimesGivenBack, Is.Zero);
        }

        // --- what the game does with the pointer's movement ---

        [Test]
        public void MovementReachesTheGameWhileThePointerIsHeld()
        {
            var capture = ArriveOnTheApron();

            Assert.That(capture.Movement(new Vector2(40f, -12f)), Is.EqualTo(new Vector2(40f, -12f)),
                "looking around is the entire reason the pointer is held");
        }

        [Test]
        public void MovementIsIgnoredOnceThePointerHasBeenHandedBack()
        {
            var capture = ArriveOnTheApron();
            capture.React(escapePressed: true, clickPressed: false);

            Assert.That(capture.Movement(new Vector2(400f, 400f)), Is.EqualTo(Vector2.zero),
                "the pointer is out on the desktop; dragging it across another monitor must not turn the view");
        }

        [Test]
        public void ThePointerComingBackBringsNoStoredUpMovementWithIt()
        {
            var capture = ArriveOnTheApron();
            var oneNudge = capture.Movement(new Vector2(40f, 0f));
            Assert.That(oneNudge, Is.EqualTo(new Vector2(40f, 0f)), "a held pointer has to move the view at all");

            capture.React(escapePressed: true, clickPressed: false);
            capture.Movement(new Vector2(2000f, 0f));
            capture.React(escapePressed: false, clickPressed: true);

            Assert.That(capture.Movement(new Vector2(40f, 0f)), Is.EqualTo(oneNudge),
                "the view carries on from where it was left rather than lurching by everything ignored");
        }

        // --- what handing the pointer back does not do ---

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        static float AcrossTheGround(Vector3 from, Vector3 to)
            => Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));

        /// <summary>
        /// Escape frees the pointer and does nothing else. It is not a pause, and a player who
        /// pressed it to answer a message has to be able to drive away straight afterwards.
        ///
        /// Walking is asked for directly rather than by pressing a key, because a headless test run
        /// has no player loop feeding devices through to a component's update. What that leaves
        /// covered is the part this change could break -- a character freezing because the pointer
        /// went away. That the keyboard itself still reaches the character is unchanged code, and
        /// is not covered here by anything.
        /// </summary>
        [UnityTest]
        public IEnumerator HandingThePointerBackDoesNotStopTheCharacterWalking()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            crew.Camera = m_Apron.Track(new GameObject("Camera").AddComponent<FollowCamera>());
            crew.IntentSource = new FixedCrewIntent(new Vector2(0f, 1f));

            var capture = crew.gameObject.AddComponent<MouseCapture>();
            capture.Pointer = m_Pointer;

            capture.React(escapePressed: true, clickPressed: false);
            Assert.That(capture.Held, Is.False, "the pointer has to actually be gone for this to mean anything");

            // Let them land first, or the drop onto the apron counts as having gone somewhere.
            yield return Step(1f);
            var from = crew.transform.position;

            yield return Step(2f);

            Assert.That(AcrossTheGround(from, crew.transform.position), Is.GreaterThan(1f),
                "Escape frees the pointer; it does not pause the game");
        }
    }
}
