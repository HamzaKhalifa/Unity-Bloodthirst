using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public enum ESoundType
{
    None, Roaming, Alert, Running, Attack, Damage, Feeding, Screaming, Agonize
}

[System.Serializable]
public class SoundList
{
    public ESoundType SoundType = ESoundType.Roaming;
    public List<AudioClip> Sounds = new List<AudioClip>();
}

[RequireComponent(typeof(AudioSource))]
public class AISoundManager : NetworkBehaviour
{
    [SerializeField] private List<SoundList> _soundLists = new List<SoundList>();


    #region Cache Fields

    private AudioSource _audioSource = null;
    public AudioSource AudioSource
    {
        get
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            return _audioSource;
        }
    }

    private SoundEmitter _soundEmitter = null;
    public SoundEmitter SoundEmitter
    {
        get
        {
            if (_soundEmitter == null)
                _soundEmitter = GetComponentInChildren<SoundEmitter>();

            return _soundEmitter;
        }
    }

    #endregion

    #region Private Fields

    private ESoundType _playingSoundType = ESoundType.None;
    [SyncVar(hook = "HookPlaySound")]
    private int _playingSoundIndex = -1;
    private float _currentAudioLength = 0f;
    private float _timer = 0f;
    private Dictionary<ESoundType, int> _soundTypesIndexesDictionary = new Dictionary<ESoundType, int>();

    #endregion

    #region Monobehavior Callbacks

    private void Awake()
    {
        int i = 0;
        foreach(ESoundType soundType in Enum.GetValues(typeof(ESoundType)))
        {
            _soundTypesIndexesDictionary.Add(soundType, i);
            i++;
        }
    }

    private void Update()
    {
        if (AudioSource.clip == null && _playingSoundType != ESoundType.None)
        {
            PlaySound(_playingSoundType);
        }

        if (AudioSource.clip != null)
        {
            _timer += Time.deltaTime;

            if (_timer >= _currentAudioLength)
            {
                _timer = 0f;
                AudioSource.clip = null;
                PlaySound(_playingSoundType);
            }
        }
    }

    #endregion

    public void SetPlayingSoundType(ESoundType soundType)
    {
        int nextPlayingSoundIndex = 0;
        _soundTypesIndexesDictionary.TryGetValue(soundType, out nextPlayingSoundIndex);
        _playingSoundIndex = nextPlayingSoundIndex;
    }

    private void HookPlaySound(int oldSoundTypeIndex, int newSoundTypeIndex)
    {
        ESoundType correspondingSoundType = ESoundType.None;

        foreach (ESoundType soundType in Enum.GetValues(typeof(ESoundType)))
        {
            if (_soundTypesIndexesDictionary[soundType] == newSoundTypeIndex)
            {
                correspondingSoundType = soundType;
                _playingSoundType = correspondingSoundType;
                break;
            }
        }

        PlaySound(correspondingSoundType);
    }

    public void PlaySound(ESoundType soundType)
    {
        SoundList soundList = _soundLists.Find((potentialSoundList) => potentialSoundList.SoundType == soundType);

        if (soundList == null) return;

        AudioClip clip = soundList.Sounds[UnityEngine.Random.Range(0, soundList.Sounds.Count)];
        if (clip != null)
        {
            AudioSource.clip = clip;
            AudioSource.Play();
            _timer = 0f;
            _currentAudioLength = clip.length;
        }
    }

    public void EmitSound()
    {
        SoundEmitter.EmitSound();
    }
}
