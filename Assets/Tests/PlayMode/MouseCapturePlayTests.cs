using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class MouseCapturePlayTests
    {
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

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

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

        static float AcrossTheGround(Vector3 from, Vector3 to)
            => Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));

        [UnityTest]
        public IEnumerator HandingThePointerBackDoesNotStopTheCharacterWalking()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            crew.Camera = m_Apron.Track(new GameObject("Camera").AddComponent<FollowCamera>());
            crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));

            var capture = crew.gameObject.AddComponent<MouseCapture>();
            capture.Pointer = m_Pointer;

            capture.React(escapePressed: true, clickPressed: false);
            Assert.That(capture.Held, Is.False, "the pointer has to actually be gone for this to mean anything");

            yield return Steps.Seconds(1f);
            var from = crew.transform.position;

            yield return Steps.Seconds(2f);

            Assert.That(AcrossTheGround(from, crew.transform.position), Is.GreaterThan(1f),
                "Escape frees the pointer; it does not pause the game");
        }
    }
}
