using Blasphemous.NewbieEltonLibs.Storage;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Components;

/// <summary>
/// Displays an <see cref="AnimationInfo" /> through a SpriteRenderer over time.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ModAnimator : MonoBehaviour
{
    private AnimationInfo? _animation;
    private float _nextUpdateTime;
    private int _currentIndex;
    private SpriteRenderer _spriteRenderer = null!;

    /// <summary>
    /// Gets or sets the animation displayed by this component.
    /// </summary>
    /// <remarks>
    /// Assigning an animation displays its first frame and restarts playback timing.
    /// Assigning <see langword="null" /> stops animation updates without changing the current sprite.
    /// </remarks>
    public AnimationInfo? Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            if (value == null)
            {
                return;
            }

            _spriteRenderer.sprite = value.Sprites[0];
            _nextUpdateTime = Time.time + value.SecondsPerFrame;
            _currentIndex = 0;
        }
    }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_animation == null || Time.time < _nextUpdateTime)
        {
            return;
        }

        if (++_currentIndex >= _animation.Sprites.Length - 1)
        {
            _currentIndex = 0;
        }

        _spriteRenderer.sprite = _animation.Sprites[_currentIndex];
        _nextUpdateTime = Time.time + _animation.SecondsPerFrame;
    }
}