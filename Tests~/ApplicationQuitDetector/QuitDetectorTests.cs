using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Calluna.DI.Tests
{
    public class QuitDetectorTests
    {
        private TestQuitDetector _quitDetector;
        private GameObject _quitDetectorGameObject;
        private bool _onQuitIsCalled = false;

        [TearDown]
        public void TearDown()
		{
            _quitDetector.OnQuit -= OnQuit;
            GameObject.DestroyImmediate(_quitDetectorGameObject);
            _onQuitIsCalled = true;
            _quitDetector = null;
            _quitDetectorGameObject = null;
        }

        [UnityTest]
        public IEnumerator IsQuitting_IsTrueOnQuitting()
        {
            GivenADefaultSetup();
            yield return 0;
            WhenIsQuitting();
            ThenIsQuittingIs(true);
        }

        [UnityTest]
        public IEnumerator IsQuitting_IsFalseByDefault()
        {
            GivenADefaultSetup();
            yield return 0;
            ThenIsQuittingIs(false);
        }

        [UnityTest]
        public IEnumerator OnQuit_IsCalledOnQuit()
		{
			GivenADefaultSetup();
            GivenOnQuitListener();
            yield return 0;
			WhenIsQuitting();
			ThenIsOnQuitIsInvoked();
		}

		private void GivenADefaultSetup()
		{
            _quitDetectorGameObject = new GameObject();
            _quitDetector = _quitDetectorGameObject.AddComponent<TestQuitDetector>();
        }

        private void GivenOnQuitListener()
        {
            _quitDetector.OnQuit += OnQuit;
        }

        private void WhenIsQuitting()
        {
            _quitDetector.SetQuitting();
        }

        private void ThenIsQuittingIs(bool shallQuitting)
        {
            Assert.AreEqual(shallQuitting, _quitDetector.IsQuitting,
                $"{nameof(QuitDetector.IsQuitting)} of {nameof(QuitDetector)} should be {shallQuitting}");
        }

        private void ThenIsOnQuitIsInvoked()
        {
            Assert.IsTrue(_onQuitIsCalled,
                $"{nameof(QuitDetector.OnQuit)} of {nameof(QuitDetector)} is not invoked like expected");
        }

        private void OnQuit()
		{
            _onQuitIsCalled = true;
        }
    }
}
