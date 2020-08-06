using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioItem
{
    public Transform transform = null;
    public AudioSource audioSource = null;
    public bool isPlaying = false;
    public IEnumerator coroutine = null;

    public void Stop()
    {
        audioSource.clip = null;
        isPlaying = false;
        coroutine = null;
        transform.gameObject.SetActive(false);
    }
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance = null;

    [SerializeField] private int _capacity = 20;
    [SerializeField] List<AudioMixerGroup> _audioGroups = new List<AudioMixerGroup>();

    // Cache Variables
    private AudioSource _audioSource = null;

    // Private
    private List<AudioItem> _audioItems = new List<AudioItem>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Register cache variables
        _audioSource = GetComponent<AudioSource>();     

        for (int i = 0; i < _capacity; i++)
        {
            GameObject gameObject = new GameObject("Pool Item " + i);
            gameObject.transform.parent = transform;
            gameObject.SetActive(false);
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();

            AudioItem audioItem = new AudioItem();
            audioItem.audioSource = audioSource;
            audioItem.transform = gameObject.transform;

            _audioItems.Add(audioItem);
        }
    }

    public AudioItem PlayOneShotSound(AudioClip clip, float volume, float spatialBlend, int audioGroup, Vector3 position = new Vector3()) 
    {
        if (clip == null) return null;

        for (int i = 0; i < _audioItems.Count; i++)
        {
            if (!_audioItems[i].isPlaying)
            {
                AudioItem audioItem = _audioItems[i];
                audioItem.isPlaying = true;
                audioItem.transform.gameObject.SetActive(true);
                audioItem.transform.position = position;
                audioItem.audioSource.clip = clip;
                audioItem.audioSource.volume = volume;
                audioItem.audioSource.spatialBlend = spatialBlend;
                if (_audioGroups.Count > audioGroup)
                    audioItem.audioSource.outputAudioMixerGroup = _audioGroups[audioGroup];

                audioItem.coroutine = AudioItemCoroutine(i);
                StartCoroutine(audioItem.coroutine);

                audioItem.audioSource.Play();

                return audioItem;
            }
        }

        return null;
    }

    public IEnumerator AudioItemCoroutine(int index)
    {
        if (index >= _audioItems.Count) yield break;

        float audioClipLength = _audioItems[index].audioSource.clip.length;

        yield return new WaitForSeconds(audioClipLength);

        _audioItems[index].Stop();
        yield break;
    }

    public void ChangeMusic(AudioClip audioClip)
    {
        if (_audioSource != null)
        {
            _audioSource.clip = audioClip;
            if(audioClip != null)
                _audioSource.Play();
            else
            {
                _audioSource.Stop();
            }
        }
    }

    public void StopMusic()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
    }
}
