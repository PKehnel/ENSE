using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class Fader : MonoBehaviour
{
  [SerializeField] private bool fadeOut = false;
  [SerializeField] private float fadeInDelay = 0;
  [SerializeField] private float fadeInDuration = 1;
  [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
  [SerializeField] private bool affectChildren = true;
  [SerializeField] private bool delayChildren = true;

  private Image image;
  private TMPro.TextMeshProUGUI text;

  private Color originalColor;
  private Color fromColor;
  private float progress;
  private bool reverse;
  private bool disableAfterReverse = true;

  private List<Fader> children = new List<Fader>();

  public float FadeDuration => fadeInDuration;
  public bool IsAnimating => progress <= fadeInDuration;

  public bool IsVisible => gameObject.activeSelf && progress > 0 && !reverse;

  void Awake()
  {
    if (affectChildren)
    {
      foreach (var fader in GetComponentsInChildren<Fader>(true))
      {
        if (fader == this) continue;
        fader.fadeInDelay = delayChildren ? fadeInDuration : 0;
        fader.fadeInDuration = fadeInDuration;
        children.Add(fader);
      }
    }

    this.image = GetComponent<Image>();
    this.text = GetComponent<TMPro.TextMeshProUGUI>();

    if (this.text != null)
    {
      this.originalColor = this.text.color;
    }
    else if (this.image != null)
    {
      this.originalColor = this.image.color;
    }

    this.fromColor = this.originalColor.WithAlpha(0);
  }

  void OnEnable()
  {
    FadeIn();
  }

  void Update()
  {
    if (fadeInDuration <= progress)
    {
      if (reverse && disableAfterReverse)
      {
        gameObject.SetActive(false);
      }
      return;
    }

    var t = (progress / fadeInDuration).Curvate(easing);
    if (reverse)
    {
      t = 1 - t;
    }

    var color = Color.Lerp(this.fromColor, this.originalColor, t);

    if (this.image != null)
    {
      this.image.color = color;
    }
    if (this.text != null)
    {
      this.text.color = color;
    }

    progress += Time.deltaTime;
  }

  public void FadeOut(bool disable = true)
  {
    foreach (var child in children)
    {
      child.FadeOut(disable);
    }

    if (!fadeOut)
    {
      if (disable) gameObject.SetActive(false);
      return;
    }

    if (image != null) image.color = this.originalColor;
    if (text != null) text.color = this.originalColor;

    reverse = true;
    progress = 0;
    disableAfterReverse = disable;
  }

  public void Hide(bool disable = true)
  {
    foreach (var child in children)
    {
      child.Hide(disable);
    }

    if (disable)
    {
      gameObject.SetActive(false);
    }
    else
    {
      progress = fadeInDuration;
      if (image != null) image.color = this.fromColor;
      if (text != null) text.color = this.fromColor;
    }
  }
  public void FadeIn()
  {
    gameObject.SetActive(true);

    if (image != null) image.color = this.fromColor;
    if (text != null) text.color = this.fromColor;

    progress = -fadeInDelay;
    reverse = false;
    disableAfterReverse = true;

    foreach (var child in children)
    {
      child.FadeIn();
    }
  }
}
