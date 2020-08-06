using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISounds : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _uiSounds = new List<AudioClip>();

    public void PlaySound(int index)
    {
        AudioClip clip = _uiSounds[index];
        if (clip != null)
            GameManager.Instance.AudioManager.PlayOneShotSound(clip, 1, 0, 2, transform.position);
    }
}
