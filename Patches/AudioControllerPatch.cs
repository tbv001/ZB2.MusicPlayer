using HarmonyLib;
using MusicPlayer.Components;

namespace MusicPlayer.Patches;

[HarmonyPatch(typeof(AudioController))]
public class AudioControllerPatch
{
    [HarmonyPatch("SetAmbientVolumes")]
    [HarmonyPostfix]
    public static void SetAmbientVolumes_Postfix(AudioController __instance)
    {
        if (!ZBMain.instance.mapIsLoaded)
        {
            __instance.ambientTargetVolume[0] = 0f;
            __instance.ambientSource[0].volume = 0f;

            if (AudioLoader.Instance != null && AudioLoader.Instance.CurrentMusicType != MusicType.Menu)
            {
                AudioLoader.Instance.PlayMusic(MusicType.Menu);
            }
        }
        else
        {
            if (AudioLoader.Instance != null && AudioLoader.Instance.CurrentMusicType == MusicType.Menu)
            {
                AudioLoader.Instance.StopMusic();
            }

            var audioLoaderInstance = AudioLoader.Instance;
            var isWaveActive = WavesController.instance.HaveToKillZombies;
            var activeBoss = GetHighestTierActiveBoss();
            var currentWave = WavesController.instance.LastSpawnedWave;

            if (activeBoss != null)
            {
                var currentBoss = activeBoss.identity.type;
                var phase = activeBoss.BossBehaviour.healthStage;
                switch (currentBoss)
                {
                    case ZombieType.BossRiot:
                        audioLoaderInstance?.PlayMusic(MusicType.BossRiot, phase: phase);
                        break;

                    case ZombieType.BossQueen:
                        audioLoaderInstance?.PlayMusic(MusicType.BossQueen, phase: phase);
                        break;

                    case ZombieType.BossReaper:
                        audioLoaderInstance?.PlayMusic(MusicType.BossReaper, phase: phase);
                        break;

                    default:
                        audioLoaderInstance?.StopMusic();
                        break;
                }
            }
            else if (isWaveActive)
            {
                var curWaveTier = Traverse.Create(WavesController.instance).Field("WaveDefinition")
                    .Method("GetWaveTier", WavesController.instance.LastSpawnedWave).GetValue<int>();
                audioLoaderInstance?.PlayMusic(MusicType.ActiveWave, curWaveTier);
            }
            else if (currentWave == 0)
            {
                audioLoaderInstance?.PlayMusic(MusicType.TimeUntilFirstWave);
            }
            else if (audioLoaderInstance != null && audioLoaderInstance.CurrentMusicType != MusicType.None &&
                     audioLoaderInstance.AudioSource.isPlaying)
            {
                audioLoaderInstance.StopMusic();
            }
        }
    }

    private static Zombie GetHighestTierActiveBoss()
    {
        if (ZombieLoader.Instance == null || BossfightController.instance == null ||
            ZombieController.instance == null) return null;
        if (!BossfightController.IsBossActive && !ZombieController.instance.respawningBossExists) return null;

        Zombie highestTierBoss = null;
        var maxTier = -1;

        var zombies = ZombieLoader.Instance.zombies;
        foreach (var zombie in zombies)
        {
            if (zombie.IsBoss && zombie.health.isAlive)
            {
                int tier = BossfightController.instance.GetBossTier(zombie.identity.type);
                if (tier > maxTier)
                {
                    maxTier = tier;
                    highestTierBoss = zombie;
                }
            }
        }

        return highestTierBoss;
    }
}
