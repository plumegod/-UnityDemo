using System.Collections;
using System.Collections.Generic;
using MXZOO.Audios;
using UnityEngine;
using YooAsset;

public class SoundEffect : MonoBehaviour
{
    public static SoundEffect instance; // SoundEffect单例
    public string battleLoopID; // 战斗循环音效ID
    public string waterSplashID; // 水花音效ID

    public GameObject randomEventHint; // 随机事件提示
    public GameObject failure; // 失败界面

    public bool playEndSound; // 是否播放结束音效
    public AudioData battleLoop; // 战斗循环音效数据
    public AudioData waterSplash; // 水花音效数据
    public AudioData snowballCollision; // 雪球碰撞音效数据
    public AudioData snowballHitPlayer; // 雪球击中玩家音效数据
    public AudioData characterDrowning; // 角色溺水音效数据
    public AudioData gameVictory; // 游戏胜利音效数据
    public AudioData gameFailure; // 游戏失败音效数据

    private Dictionary<AudioData, string> dictionary; // 音效数据字典
    public Player player; // 玩家

    private void Start()
    {
    }

    public void OnEnable()
    {
        instance = this;
        if (Time.timeScale == 0) Time.timeScale = 1;
        battleLoopID = Audio.Control.CreateAudio(battleLoop); // 创建战斗循环音效
        Audio.Control.ChargeAudio(battleLoopID, AudioStates.Play); // 播放战斗循环音效
        waterSplashID = Audio.Control.CreateAudio(waterSplash); // 创建水花音效
        Audio.Control.ChargeAudio(waterSplashID, AudioStates.Play); // 播放水花音效
    }

    public void Play(SoundEffects soundEffect)
    {
        switch (soundEffect)
        {
            case SoundEffects.SnowballCollision:
                StartCoroutine(PlaySoundEffect("SoundEffect_SnowballCollision")); // 播放雪球碰撞音效
                break;
            case SoundEffects.SnowballHitPlayer:
                StartCoroutine(PlaySoundEffect("SoundEffect_SnowballHitPlayer")); // 播放雪球击中玩家音效
                break;
            case SoundEffects.CharacterDrowning:
                StartCoroutine(PlaySoundEffect("SoundEffect_CharacterDrowning")); // 播放角色溺水音效
                break;
            case SoundEffects.GameVictory:
                if (!playEndSound)
                {
                    StartCoroutine(PlaySoundEffect("SoundEffect_GameVictory")); // 播放游戏胜利音效
                    playEndSound = true;
                    randomEventHint.SetActive(false); // 隐藏随机事件提示
                    player.Victory(); // 触发玩家胜利事件
                    Audio.Control.ChargeAudio("Music_BattleBGM", AudioStates.Pause); // 暂停战斗背景音乐
                }

                break;
            case SoundEffects.GameFailure:
                if (!playEndSound)
                {
                    StartCoroutine(PlaySoundEffect("SoundEffect_GameFailure")); // 播放游戏失败音效
                    playEndSound = true;
                    randomEventHint.SetActive(false); // 隐藏随机事件提示
                    failure.SetActive(true); // 显示失败界面
                    Audio.Control.ChargeAudio("Music_BattleBGM", AudioStates.Pause); // 暂停战斗背景音乐
                }

                break;
            case SoundEffects.ShootSoundEffect:
                StartCoroutine(PlaySoundEffect("SoundEffect_ShootSoundEffect")); // 播放射击音效
                break;
        }
    }

    private IEnumerator PlaySoundEffect(string name)
    {
        var handle = YooAssets.LoadAssetAsync<GameObject>(name); // 异步加载音效资源
        yield return handle;
        Destroy(handle.InstantiateSync(), 10); // 实例化音效并在10秒后销毁
    }
}

public enum SoundEffects
{
    SnowballCollision, // 雪球碰撞音效
    SnowballHitPlayer, // 雪球击中玩家音效
    CharacterDrowning, // 角色溺水音效
    GameVictory, // 游戏胜利音效
    GameFailure, // 游戏失败音效
    ShootSoundEffect // 射击音效
}