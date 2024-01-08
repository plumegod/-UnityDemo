using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RandomEventsManager : MonoBehaviour
{
    public enum RandomEvent
    {
        Blizzard, // 暴风雪
        SlipperyRoad, // 滑道
        SlowDown, // 减速
        Name1, // 名称1
        Name2, // 名称2
        Name3, // 名称3
        Name4 // 名称4
    }

    public static RandomEventsManager Instance;
    private static Snowplow Player;
    public GameObject SlowSnow; // 减速雪
    public Animator EventHint; // 事件提示动画
    public GameObject SnowAndIce; // 雪和冰
    public GameObject ThinIce; // 薄冰
    public GameObject PerilousSituation; // 危险情况
    public GameObject AddInsult; // 添加侮辱
    public GameObject DifficultProgress; // 困难进展
    public GameObject SlipperyRoad; // 滑道
    public GameObject Blizzard; // 暴风雪
    public Text Text; // 文本显示
    public bool MutualCancellation; // 互相取消
    public RandomEvent CurrentEvent; // 当前事件

    private void Update()
    {
        /* if (Input.GetKeyDown(KeyCode.K))
         {
             RandomEvent Event = (RandomEvent)UnityEngine.Random.Range(0, 8);
             Debug.Log(Event);
             SwitchEvent(Event);
             Text.text= Enum.GetName(typeof(RandomEvent), Event);
         }*/
    }

    private void OnEnable()
    {
        Instance = this;
    }

    public void RandomEventInitialize()
    {
        foreach (Snowplow snowplow in EnemyManager.instance.all)
            if (snowplow is Player)
            {
                Player = snowplow;
                break;
            }

        var Event = (RandomEvent)Random.Range(0, 7);
        Debug.Log(Event);
        SwitchEvent(Event);
        /*    SwitchEvent(RandomEvent.Name1);*/
    }

    private void RestoreNormal() // 恢复所有行为为正常状态
    {
        MutualCancellation = false;
        foreach (Snowplow snowplow in EnemyManager.instance.all)
        {
            snowplow.snowballEnlargeMultiplier = 1;
            snowplow.snowball_max = 1;
            snowplow.launchEnlarge = false;
            snowplow.moveMultiplier = 1;
            snowplow.lifetime = 100;
            snowplow.snowballMoveSpeed = 15;
            snowplow.snowballMoveMultiplier = 1;
            snowplow.snowballCooldown = 2;
        }
    }

    public void SwitchEvent(RandomEvent switchEvent)
    {
        SlowSnow.SetActive(false);
        SnowAndIce.SetActive(false);
        ThinIce.SetActive(false);
        PerilousSituation.SetActive(false);
        AddInsult.SetActive(false);
        DifficultProgress.SetActive(false);
        SlipperyRoad.SetActive(false);
        Blizzard.SetActive(false);
        EventHint.SetTrigger("Switch");
        CurrentEvent = switchEvent;
        Text.text = Enum.GetName(typeof(RandomEvent), switchEvent);
        RestoreNormal();
        switch (switchEvent)
        {
            case RandomEvent.Blizzard:
                Blizzard.SetActive(true);
                foreach (Snowplow snowplow in EnemyManager.instance.all)
                    if (snowplow is Enemy)
                    {
                        snowplow.snowballEnlargeMultiplier = 2f;
                        snowplow.snowballCooldown = 1;
                    }

                break;
            case RandomEvent.SlipperyRoad:
                SlipperyRoad.SetActive(true);
                Player.snowball_max = 0.6f;
                break;
            case RandomEvent.SlowDown:
                SlowSnow.SetActive(true);
                DifficultProgress.SetActive(true);
                Player.moveMultiplier = 0.75f;
                break;
            case RandomEvent.Name1:
                AddInsult.SetActive(true);
                foreach (Snowplow snowplow in EnemyManager.instance.all)
                    if (snowplow is Enemy)
                        snowplow.launchEnlarge = true;
                break;
            case RandomEvent.Name2:
                PerilousSituation.SetActive(true);
                foreach (Snowplow snowplow in EnemyManager.instance.all)
                    if (snowplow is Enemy)
                        snowplow.snowballMoveMultiplier = 2f;
                break;
            case RandomEvent.Name3:
                ThinIce.SetActive(true);
                Player.lifetime = 0.75f;
                break;
            case RandomEvent.Name4:
                SnowAndIce.SetActive(true);
                MutualCancellation = true;
                break;
        }
    }
}