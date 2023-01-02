using System.Collections;
using PhEngine.Motion;
using UnityEngine;

namespace PhEngine.Sound
{
    [CreateAssetMenu(menuName = "PhEngine/Motion/MotionBlueprint/Sound/SoundPlayer", fileName = "MotionBlueprint_SoundPlayer")]
    public class MotionBlueprint_SoundPlayer : MotionBlueprintGen<MonoBehaviour,MotionSetting_SoundPlayer>
    {
        protected override MotionSetting_SoundPlayer DefaultSetting => defaultSetting;
        public MotionSetting_SoundPlayer defaultSetting;
        
        protected override void PlayAndNotifyFinish(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var target = motionCommand.Component;
            target.StartCoroutine(PlaySoundRoutine(motionCommand));
        }

        IEnumerator PlaySoundRoutine(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var clip = GetClipByLoadId(motionCommand);
            var type = motionCommand.Setting.type;
            var isLoop = motionCommand.Setting.isLoop;
            var playDelayInSeconds = motionCommand.Setting.playDelayInSeconds;
            var fadeInDuration  = motionCommand.Setting.fadeInDuration;
            var fadeOutDuration  = motionCommand.Setting.fadeOutDuration;
            var isPersistAcrossScene  = motionCommand.Setting.isPersistAcrossScene;

            yield return new WaitForSeconds(playDelayInSeconds);
            
            SoundManager.Instance.PlayAudio
            (
                type,
                clip,
                1f,
                isLoop,
                isPersistAcrossScene,
                fadeInDuration,
                fadeOutDuration
            );
            
            yield return new WaitForSeconds(clip.length);
            motionCommand.Player.NotifyMotionFinish(motionCommand.Motion);
        }

        protected override void Pause(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var clip = GetClipByLoadId(motionCommand);
            SoundManager.Instance.GetAudio(clip).Pause();
        }

        protected override void Resume(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var clip = GetClipByLoadId(motionCommand);
            SoundManager.Instance.GetAudio(clip).Resume();
        }

        protected override void Kill(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var clip = GetClipByLoadId(motionCommand);
            SoundManager.Instance.GetAudio(clip).Stop();
        }

        protected override void Complete(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var clip = GetClipByLoadId(motionCommand);
            SoundManager.Instance.GetAudio(clip).Stop();
        }

        static AudioClip GetClipByLoadId(MotionCommand<MonoBehaviour, MotionSetting_SoundPlayer> motionCommand)
        {
            var id = motionCommand.Setting.clipLoadId;
            var clip = SoundManager.Instance.Find(id);
            return clip;
        }
    }
}