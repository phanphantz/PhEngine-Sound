using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using PhEngine.Core.AssetBox;
using PhEngine.Core;

namespace PhEngine.Sound
{
    /// <summary>
    /// Static class responsible for playing and managing audio and sounds.
    /// </summary>
    public class SoundManager : Singleton<SoundManager>
    {
        [Header("Sound Database")]
        [SerializeField] AssetBox gameplaySoundLoader;
        [SerializeField] AssetBox uiSoundLoader;
        [SerializeField] AssetBox musicLoader;
        
        public AudioClip Find(string loadId)
        {
            var tryFindUISound = FindUISound(loadId);
            if (tryFindUISound)
                return tryFindUISound;
            
            var tryFindGameplaySound = FindGameplaySound(loadId);
            return tryFindGameplaySound ? tryFindGameplaySound : FindMusic(loadId);
        }
        
        public AudioClip FindGameplaySound(string loadId) => gameplaySoundLoader.LoadAsset<AudioClip>(loadId);
        public AudioClip FindUISound(string loadId) => uiSoundLoader.LoadAsset<AudioClip>(loadId);
        public AudioClip FindMusic(string loadId) => musicLoader.LoadAsset<AudioClip>(loadId); 
        public bool isShowingLog;
        
        /// <summary>
        /// When set to true, new music audios that have the same audio clip as any other music audios, will be ignored
        /// </summary>
        public bool IsIgnoreDuplicateMusic { get; set; }

        /// <summary>
        /// When set to true, new sound audios that have the same audio clip as any other sound audios, will be ignored
        /// </summary>
        public bool IsIgnoreDuplicateGameplaySounds { get; set; }

        /// <summary>
        /// When set to true, new UI sound audios that have the same audio clip as any other UI sound audios, will be ignored
        /// </summary>
        public bool IsIgnoreDuplicateUISounds { get; set; }

        /// <summary>
        /// Global volume
        /// </summary>
        public float GlobalVolume { get; set; }

        /// <summary>
        /// Global music volume
        /// </summary>
        public float MusicVolume { get; set; }

        /// <summary>
        /// Global sounds volume
        /// </summary>
        public float GameplaySoundsVolume { get; set; }

        /// <summary>
        /// Global UI sounds volume
        /// </summary>
        public float UISoundsVolume { get; set; }
        
        Dictionary<int, Audio> musicAudio;
        Dictionary<int, Audio> gameplaySoundsAudio;
        Dictionary<int, Audio> UISoundsAudio;
        Dictionary<int, Audio> audioPool;

        bool initialized = false;
        
        /// <summary>
        /// Initialized the sound manager
        /// </summary>
        void Init()
        {
            if (initialized) 
                return;
            
            musicAudio = new Dictionary<int, Audio>();
            gameplaySoundsAudio = new Dictionary<int, Audio>();
            UISoundsAudio = new Dictionary<int, Audio>();
            audioPool = new Dictionary<int, Audio>();

            GlobalVolume = 1;
            MusicVolume = 1;
            GameplaySoundsVolume = 1;
            UISoundsVolume = 1;

            IsIgnoreDuplicateMusic = false;
            IsIgnoreDuplicateGameplaySounds = false;
            IsIgnoreDuplicateUISounds = false;
            initialized = true;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Event triggered when a new scene is loaded
        /// </summary>
        /// <param name="scene">The scene that is loaded</param>
        /// <param name="mode">The scene load mode</param>
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // Stop and remove all non-persistent audio
            RemoveNonPersistAudio(musicAudio);
            RemoveNonPersistAudio(gameplaySoundsAudio);
            RemoveNonPersistAudio(UISoundsAudio);
        }

        void Update()
        {
            UpdateAllAudio(musicAudio);
            UpdateAllAudio(gameplaySoundsAudio);
            UpdateAllAudio(UISoundsAudio);
        }

        /// <summary>
        /// Retrieves the audio dictionary based on the audioType
        /// </summary>
        /// <param name="audioType">The audio type of the dictionary to return</param>
        /// <returns>An audio dictionary</returns>
        Dictionary<int, Audio> GetAudioTypeDictionary(Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = new Dictionary<int, Audio>();
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    audioDict = musicAudio;
                    break;
                case Audio.AudioType.GameplaySound:
                    audioDict = gameplaySoundsAudio;
                    break;
                case Audio.AudioType.UISound:
                    audioDict = UISoundsAudio;
                    break;
            }

            return audioDict;
        }

        /// <summary>
        /// Retrieves the IgnoreDuplicates setting of audios of a specified audio type
        /// </summary>
        /// <param name="audioType">The audio type that the returned IgnoreDuplicates setting affects</param>
        /// <returns>An IgnoreDuplicates setting (bool)</returns>
        bool GetAudioTypeIgnoreDuplicateSetting(Audio.AudioType audioType)
        {
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    return IsIgnoreDuplicateMusic;
                case Audio.AudioType.GameplaySound:
                    return IsIgnoreDuplicateGameplaySounds;
                case Audio.AudioType.UISound:
                    return IsIgnoreDuplicateUISounds;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Updates the state of all audios of an audio dictionary
        /// </summary>
        /// <param name="audioDict">The audio dictionary to update</param>
        void UpdateAllAudio(Dictionary<int, Audio> audioDict)
        {
            // Go through all audios and update them
            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                audio.Update();

                // Remove it if it is no longer active (playing)
                if (!audio.IsPlaying && !audio.Paused)
                {
                    Destroy(audio.AudioSource);

                    // Add it to the audio pool in case it needs to be referenced in the future
                    audioPool.Add(key, audio);
                    audio.Pooled = true;
                    audioDict.Remove(key);
                }
            }
        }

        /// <summary>
        /// Remove all non-persistant audios from an audio dictionary
        /// </summary>
        /// <param name="audioDict">The audio dictionary whose non-persistant audios are getting removed</param>
        void RemoveNonPersistAudio(Dictionary<int, Audio> audioDict)
        {
            // Go through all audios and remove them if they should not persist through scenes
            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                if (!audio.Persist && audio.Activated)
                {
                    Destroy(audio.AudioSource);
                    audioDict.Remove(key);
                }
            }

            // Go through all audios in the audio pool and remove them if they should not persist through scenes
            keys = new List<int>(audioPool.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioPool[key];
                if (!audio.Persist && audio.Activated)
                {
                    audioPool.Remove(key);
                }
            }
        }

        /// <summary>
        /// Restores and re-adds a pooled audio to its corresponding audio dictionary
        /// </summary>
        /// <param name="audioType">The audio type of the audio to restore</param>
        /// <param name="audioID">The ID of the audio to be restored</param>
        /// <returns>True if the audio is restored, false if the audio was not in the audio pool.</returns>
        public bool RestoreAudioFromPool(Audio.AudioType audioType, int audioID)
        {
            if(audioPool.ContainsKey(audioID))
            {
                Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);
                audioDict.Add(audioID, audioPool[audioID]);
                audioPool.Remove(audioID);

                return true;
            }

            return false;
        }

        #region GetAudio Functions

        /// <summary>
        /// Returns the Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the Audio to be retrieved</param>
        /// <returns>Audio that has as its id the audioID, null if no such Audio is found</returns>
        public Audio GetAudio(int audioID)
        {
            Audio audio;

            audio = GetMusicAudio(audioID);
            if (audio != null)
            {
                return audio;
            }

            audio = GetGameplaySoundAudio(audioID);
            if (audio != null)
            {
                return audio;
            }

            audio = GetUISoundAudio(audioID);
            if (audio != null)
            {
                return audio;
            }

            return null;
        }

        /// <summary>
        /// Returns the first occurrence of Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the Audio to be retrieved</param>
        /// <returns>First occurrence of Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public Audio GetAudio(AudioClip audioClip)
        {
            Audio audio = GetMusicAudio(audioClip);
            if (audio != null)
            {
                return audio;
            }

            audio = GetGameplaySoundAudio(audioClip);
            if (audio != null)
            {
                return audio;
            }

            audio = GetUISoundAudio(audioClip);
            if (audio != null)
            {
                return audio;
            }

            return null;
        }

        /// <summary>
        /// Returns the music Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the music Audio to be returned</param>
        /// <returns>Music Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public Audio GetMusicAudio(int audioID)
        {
            return GetAudio(Audio.AudioType.Music, true, audioID);
        }

        /// <summary>
        /// Returns the first occurrence of music Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the music Audio to be retrieved</param>
        /// <returns>First occurrence of music Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public Audio GetMusicAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.Music, true, audioClip);
        }

        /// <summary>
        /// Returns the sound fx Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the sound fx Audio to be returned</param>
        /// <returns>Sound fx Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public Audio GetGameplaySoundAudio(int audioID)
        {
            return GetAudio(Audio.AudioType.GameplaySound, true, audioID);
        }

        /// <summary>
        /// Returns the first occurrence of sound Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the sound Audio to be retrieved</param>
        /// <returns>First occurrence of sound Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public Audio GetGameplaySoundAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.GameplaySound, true, audioClip);
        }

        /// <summary>
        /// Returns the UI sound fx Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the UI sound fx Audio to be returned</param>
        /// <returns>UI sound fx Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public Audio GetUISoundAudio(int audioID)
        {
            return GetAudio(Audio.AudioType.UISound, true, audioID);
        }

        /// <summary>
        /// Returns the first occurrence of UI sound Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the UI sound Audio to be retrieved</param>
        /// <returns>First occurrence of UI sound Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public Audio GetUISoundAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.UISound, true, audioClip);
        }

        Audio GetAudio(Audio.AudioType audioType, bool usePool, int audioID)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            if (audioDict.ContainsKey(audioID))
            {
                return audioDict[audioID];
            }

            if (usePool && audioPool.ContainsKey(audioID) && audioPool[audioID].Type == audioType)
            {
                return audioPool[audioID];
            }

            return null;
        }

        Audio GetAudio(Audio.AudioType audioType, bool usePool, AudioClip audioClip)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> audioTypeKeys = new List<int>(audioDict.Keys);
            List<int> poolKeys = new List<int>(audioPool.Keys);
            List<int> keys = usePool ? audioTypeKeys.Concat(poolKeys).ToList() : audioTypeKeys;
            foreach (int key in keys)
            {
                Audio audio = null;
				if (audioDict.ContainsKey(key))
				{
					audio = audioDict[key];
				}
				else if(audioPool.ContainsKey(key))
				{
					audio = audioPool[key];
				}
				if (audio == null)
				{
					return null;
				}
				if (audio.Clip == audioClip && audio.Type == audioType)
                {
                    return audio;
                }
            }

            return null;
        }

        #endregion

        #region Prepare Function

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareMusic(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, 1f, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareMusic(AudioClip clip, float volume)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name = "persist" > Whether the audio persists in between scene changes</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareMusic(AudioClip clip, float volume, bool loop, bool persist)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Prerpares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name="persist"> Whether the audio persists in between scene changes</param>
        /// <param name="fadeInValue">How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)</param>
        /// <param name="fadeOutValue"> How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareMusic(AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name="persist"> Whether the audio persists in between scene changes</param>
        /// <param name="fadeInValue">How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)</param>
        /// <param name="fadeOutValue"> How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)</param>
        /// <param name="currentMusicfadeOutSeconds"> How many seconds it needs for current music audio to fade out. It will override its own fade out seconds. If -1 is passed, current music will keep its own fade out seconds</param>
        /// <param name="sourceTransform">The transform that is the source of the music (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareMusic(AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareGameplaySound(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.GameplaySound, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareGameplaySound(AudioClip clip, float volume)
        {
            return PrepareAudio(Audio.AudioType.GameplaySound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareGameplaySound(AudioClip clip, bool loop)
        {
            return PrepareAudio(Audio.AudioType.GameplaySound, clip, 1f, loop, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareGameplaySound(AudioClip clip, float volume, bool loop, Transform sourceTransform)
        {
            return PrepareAudio(Audio.AudioType.GameplaySound, clip, volume, loop, false, 0f, 0f, -1f, sourceTransform);
        }

        /// <summary>
        /// Prepares and initializes a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareUISound(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.UISound, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Prepares and initializes a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public int PrepareUISound(AudioClip clip, float volume)
        {
            return PrepareAudio(Audio.AudioType.UISound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        int PrepareAudio(Audio.AudioType audioType, AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            if (clip == null)
            {
                Debug.LogError("[Eazy Sound Manager] Audio clip is null", clip);
            }

            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);
            bool ignoreDuplicateAudio = GetAudioTypeIgnoreDuplicateSetting(audioType);

            if (ignoreDuplicateAudio)
            {
                Audio duplicateAudio = GetAudio(audioType, true, clip);
                if(duplicateAudio != null)
                {
                    return duplicateAudio.AudioID;
                }
            }

            // Create the audioSource
            Audio audio = new Audio(audioType, clip, loop, persist, volume, fadeInSeconds, fadeOutSeconds, sourceTransform);

            // Add it to dictionary
            audioDict.Add(audio.AudioID, audio);

            return audio.AudioID;
        }

        #endregion

        #region Play Functions
        
        void TryLogError(string type , string loadId)
        {
            if (isShowingLog)
                Core.PhDebug.LogError<SoundManager>("Cannot play " + type +" with id : " + loadId + ". Clip not found");
        }
        
        public int FadeCurrentMusicIntoNewMusic(string loadId , bool loop, bool persist, float fadeInNewMusicSeconds = 0, float fadeOutCurrentMusicSeconds = 0,float fadeOutNewMusicSeconds = 0,  Transform sourceTransform = null)
        {
            var tryGetMusic = FindMusic(loadId);
            if (tryGetMusic == null)
            {
                TryLogError("Music",loadId);
                return -1;
            }
            
            return FadeCurrentMusicIntoNewMusic(tryGetMusic, loop, persist, fadeInNewMusicSeconds, fadeOutCurrentMusicSeconds,fadeOutNewMusicSeconds ,sourceTransform);
        }
        
        public int FadeCurrentMusicIntoNewMusic(AudioClip clip, bool loop, bool persist, float fadeInNewMusicSeconds = 0, float fadeOutCurrentMusicSeconds = 0,float fadeOutNewMusicSeconds = 0,  Transform sourceTransform = null)
        {
            return PlayMusic(clip, loop, persist, fadeInNewMusicSeconds, fadeOutCurrentMusicSeconds, fadeOutNewMusicSeconds,
                sourceTransform, true);
        }
        
        public int PlayMusicAdditively(string loadId, bool loop, bool persist, float fadeInSeconds = 0, float fadeOutSeconds = 0, Transform sourceTransform = null)
        {
            var tryGetMusic = FindMusic(loadId);
            if (tryGetMusic == null)
            {
                TryLogError("Music",loadId);
                return -1;
            }

            return PlayMusicAdditively(tryGetMusic, loop,  persist, fadeInSeconds, fadeOutSeconds, sourceTransform);
        }
        
        public int PlayMusicAdditively(AudioClip clip, bool loop, bool persist, float fadeInSeconds = 0, float fadeOutSeconds = 0, Transform sourceTransform = null)
        {
            return PlayMusic(clip, loop, persist, fadeInSeconds, -1f , fadeOutSeconds,
                sourceTransform);
        }
        
        int PlayMusic(AudioClip clip, bool loop, bool persist, float fadeInSeconds = 0, float currentMusicfadeOutSeconds = -1, float fadeOutSeconds = 0,  Transform sourceTransform = null, bool isRemoveCurrentMusic = false)
        {
            return PlayAudio(Audio.AudioType.Music, clip, MusicVolume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform, isRemoveCurrentMusic);
        }
        
        public int PlayGameplaySound(string loadId, float fadeInSeconds = 0, float fadeOutSeconds = 0, bool loop = false, bool persist = true,  Transform sourceTransform = null)
        {
            var tryGetGameplaySound = FindGameplaySound(loadId);
            if (tryGetGameplaySound == null)
            {
                TryLogError("Gameplay Sound",loadId);
                return -1;
            }
            
            return PlayGameplaySound(tryGetGameplaySound,  fadeInSeconds, fadeOutSeconds, loop, persist , sourceTransform);
        }
        
        public int PlayGameplaySound(AudioClip clip, float fadeInSeconds = 0, float fadeOutSeconds = 0, bool loop = false, bool persist = true,  Transform sourceTransform = null)
        {
            return PlayAudio(Audio.AudioType.GameplaySound, clip, GameplaySoundsVolume, loop, persist, fadeInSeconds, fadeOutSeconds, -1f, sourceTransform);
        }
        
        public int PlayUISound(string loadId, float fadeInSeconds = 0, float fadeOutSeconds = 0, bool loop = false, bool persist = true,  Transform sourceTransform = null)
        {
            var tryGetUISound = FindUISound(loadId);
            if (tryGetUISound == null)
            {
                TryLogError("UI Sound",loadId);
                return -1;
            }
            
            return PlayUISound(tryGetUISound, fadeInSeconds, fadeOutSeconds, loop, persist, sourceTransform);
        }
        
        public int PlayUISound(AudioClip clip, float fadeInSeconds = 0, float fadeOutSeconds = 0, bool loop = false, bool persist = true,  Transform sourceTransform = null)
        {
            return PlayAudio(Audio.AudioType.UISound, clip, UISoundsVolume, false, persist, 0f, 0f, -1f, null);
        }
        
        public int PlayAudio(Audio.AudioType audioType, string loadId, float volume, bool loop, bool persist, float fadeInSeconds =0f, float fadeOutSeconds =0f, float currentMusicfadeOutSeconds = -1, Transform sourceTransform = null, bool isRemoveCurrentMusic = true)
        {
            var tryGetSound = Find(loadId);
            if (tryGetSound == null)
            {
                TryLogError("Sound",loadId);
                return -1;
            }
            
            return PlayAudio(audioType, tryGetSound, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform, isRemoveCurrentMusic);
        }

        public int PlayAudio(Audio.AudioType audioType, AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds =0f, float fadeOutSeconds = 0f, float currentMusicfadeOutSeconds = -1, Transform sourceTransform = null, bool isRemoveCurrentMusic = true)
        {
            int audioID = PrepareAudio(audioType, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform);

            // Stop all current music playing
            if (audioType == Audio.AudioType.Music && isRemoveCurrentMusic)
            {
                StopAllMusic(currentMusicfadeOutSeconds);
            }

            GetAudio(audioType, false, audioID).Play();

            return audioID;
        }

        #endregion

        #region Stop Functions

        /// <summary>
        /// Stop all audio playing
        /// </summary>
        public void StopAll()
        {
            StopAll(-1f);
        }

        /// <summary>
        /// Stop all audio playing
        /// </summary>
        /// <param name="musicFadeOutSeconds"> How many seconds it needs for all music audio to fade out. It will override  their own fade out seconds. If -1 is passed, all music will keep their own fade out seconds</param>
        public void StopAll(float musicFadeOutSeconds)
        {
            StopAllMusic(musicFadeOutSeconds);
            StopAllGameplaySounds();
            StopAllUISounds();
        }

        /// <summary>
        /// Stop all music playing
        /// </summary>
        public void StopAllMusic()
        {
            StopAllAudio(Audio.AudioType.Music, -1f);
        }

        /// <summary>
        /// Stop all music playing
        /// </summary>
        /// <param name="fadeOutSeconds"> How many seconds it needs for all music audio to fade out. It will override  their own fade out seconds. If -1 is passed, all music will keep their own fade out seconds</param>
        public void StopAllMusic(float fadeOutSeconds)
        {
            StopAllAudio(Audio.AudioType.Music, fadeOutSeconds);
        }

        /// <summary>
        /// Stop all sound fx playing
        /// </summary>
        public void StopAllGameplaySounds()
        {
            StopAllAudio(Audio.AudioType.GameplaySound, -1f);
        }

        /// <summary>
        /// Stop all UI sound fx playing
        /// </summary>
        public void StopAllUISounds()
        {
            StopAllAudio(Audio.AudioType.UISound, -1f);
        }

        void StopAllAudio(Audio.AudioType audioType, float fadeOutSeconds)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                if (fadeOutSeconds > 0)
                {
                    audio.FadeOutSeconds = fadeOutSeconds;
                }
                audio.Stop();
            }
        }

        #endregion

        #region Pause Functions

        /// <summary>
        /// Pause all audio playing
        /// </summary>
        public void PauseAll()
        {
            PauseAllMusic();
            PauseAllGameplaySounds();
            PauseAllUISounds();
        }

        /// <summary>
        /// Pause all music playing
        /// </summary>
        public void PauseAllMusic()
        {
            PauseAllAudio(Audio.AudioType.Music);
        }

        /// <summary>
        /// Pause all sound fx playing
        /// </summary>
        public void PauseAllGameplaySounds()
        {
            PauseAllAudio(Audio.AudioType.GameplaySound);
        }

        /// <summary>
        /// Pause all UI sound fx playing
        /// </summary>
        public void PauseAllUISounds()
        {
            PauseAllAudio(Audio.AudioType.UISound);
        }

        void PauseAllAudio(Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                audio.Pause();
            }
        }

        #endregion

        #region Resume Functions

        /// <summary>
        /// Resume all audio playing
        /// </summary>
        public void ResumeAll()
        {
            ResumeAllMusic();
            ResumeAllGameplaySounds();
            ResumeAllUISounds();
        }

        /// <summary>
        /// Resume all music playing
        /// </summary>
        public void ResumeAllMusic()
        {
            ResumeAllAudio(Audio.AudioType.Music);
        }

        /// <summary>
        /// Resume all sound fx playing
        /// </summary>
        public void ResumeAllGameplaySounds()
        {
            ResumeAllAudio(Audio.AudioType.GameplaySound);
        }

        /// <summary>
        /// Resume all UI sound fx playing
        /// </summary>
        public void ResumeAllUISounds()
        {
            ResumeAllAudio(Audio.AudioType.UISound);
        }

        private void ResumeAllAudio(Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                audio.Resume();
            }
        }

        #endregion

        protected override void InitAfterAwake()
        {
            Init();
        }
    }
}
