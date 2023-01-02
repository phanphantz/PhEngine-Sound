using System;
using PhEngine.Motion;

namespace PhEngine.Sound
{
    [Serializable]
    public class MotionSetting_SoundPlayer : MotionSetting
    {
        public float playDelayInSeconds;
        public string clipLoadId;
        public Audio.AudioType type;
        public float fadeInDuration;
        public float fadeOutDuration;
        public bool isLoop;
        public bool isPersistAcrossScene = true;
    }
}