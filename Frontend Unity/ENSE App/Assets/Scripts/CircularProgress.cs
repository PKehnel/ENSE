using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

[SelectionBase]
public class CircularProgress : MonoBehaviour
{
  private enum State
  {
    Hidden,
    Loading,
    Loaded,
    Faking,
  }

  [SerializeField] private float animationDuration = 5;
  [SerializeField] private float animationFakeDuration = 0.3f;
  [SerializeField] private AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);


  private Image image;
  private Fader innerLogoFader;
  private Fader selfFader;

  private float progress;
  private State state;

  void Awake()
  {
    image = GetComponent<Image>();
    innerLogoFader = transform.GetComponentsInChildren<Fader>().First(x => x.gameObject != gameObject);
    selfFader = transform.GetComponent<Fader>();

    selfFader.Hide(false);
    innerLogoFader.Hide();
  }

  void Update()
  {
    // Update state of the fader. Start loading. Cancel loading.
    if (state == State.Hidden)
    {

    }
    else if (state == State.Loading)
    {
      image.fillAmount = animationCurve.Evaluate(progress / animationDuration);

      if (progress > animationDuration)
      {
        innerLogoFader.FadeIn();
        selfFader.FadeOut(false);
        state = State.Loaded;
      }

      progress += Time.deltaTime;
    }
    else if (state == State.Loaded)
    {

    }
  }

  public void StartAnimation()
  {
    if (state == State.Hidden)
    {
      progress = 0;
      innerLogoFader.Hide();
      selfFader.FadeIn();
      state = State.Loading;
    }
  }

  public void Hide(System.Action callback)
  {
    StartCoroutine(FakeLoading(callback));
  }

  private IEnumerator FakeLoading(System.Action callback)
  {
    if (state == State.Hidden)
    {
      if (callback != null) callback();
      yield break;
    }

    var oldState = state;
    state = State.Faking;

    if (oldState == State.Loading)
    {
      innerLogoFader.FadeIn();

      var startProgress = progress / animationDuration;
      var missingProgress = 1 - startProgress;
      var fakeProgress = 0f;
      while (true)
      {
        image.fillAmount = animationCurve.Evaluate(fakeProgress / animationFakeDuration * missingProgress + startProgress);

        if (fakeProgress > animationFakeDuration)
        {
          selfFader.FadeOut(false);
          break;
        }

        fakeProgress += Time.deltaTime;
        yield return 0;
      }
    }

    innerLogoFader.FadeOut();
    state = State.Hidden;
    if (callback != null) callback();

    yield return new WaitForSeconds(innerLogoFader.FadeDuration);
  }
}
