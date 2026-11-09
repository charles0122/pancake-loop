using System;
using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using ByteLoop.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ByteLoop.Manager
{


    public class GameManager : PersistentMonoSingleton<GameManager>
    {

        [SerializeField] private int day = 0;
        [SerializeField] private int npc = 0;
        public bool IsPaused = false;
        public bool InputAllowed = true;
        public GameObject TimerGO;
        [Space]
        [Header("NPC 列表")]
        [SerializeField] public List<Role> npcList;

        [Space]
        [Header("对话相关")]

        [SerializeField] public List<DayDialog> dayDialogList;
        private List<RoleDialogSO> roleDialogSOList;
        [SerializeField] public DialogBox _dialogBox;
        [SerializeField] public GameObject DesktapPrefab;
        public GameObject OverAnimGo;
        // private int dialogIndex = 0;

        public bool isFade;
        public CanvasGroup fadeCanvasGroup;
        public float fadeDuration;

        public int Day
        {
            get => day;
            set
            {
                DialogSystemTest.Instance._index = 0;
                npc = 0;
                if (HasNextDay())
                {

                    day = value;
                    if (!isFade)
                    {
                        StartCoroutine(SwitchDayScene());
                    }

                }
                else
                {
                    // day = 0;
                    // npc=0;
                    Debug.Log("next loop");
                    // day = 0;
                    // Npc = 0;
                    // DialogSystemTest.Instance.StartDialog();

                    // test
                    Debug.Log("开始动画");
                    StartCoroutine(OverLoop());

                }

            }
        }

        private IEnumerator OverLoop()
        {
            yield return new WaitForSeconds(2f);
            // yield return ;// 过场动画
            HideGameHud();
            UIManager.Instance.HidePanel(UIManager.Instance.GamePanel);
            OverAnimGo.SetActive(true);
            // AudioManager.Instance.PlayBGM(Music.MainMenuBGM, true); // 全程bgm
            yield return new WaitForSeconds(25f);
            // 最后一张CG 延迟 10s
            yield return new WaitForSeconds(10f);
            // 暂停音乐
            AudioManager.Instance.Stop();
            // 延迟 2s返回主界面
            yield return new WaitForSeconds(2f);
            _gameStarting = false;
            UIManager.Instance.SwitchMainMenuState(true);
        }

        private IEnumerator SwitchDayScene()
        {
            fadeCanvasGroup.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText("第" + (Day + 1) + "天");
            yield return Fade(1);
            Debug.Log("next day");
            Destroy(GameObject.FindGameObjectWithTag("Env").transform.GetChild(0).gameObject);
            GameObject gameObject = Instantiate(DesktapPrefab, new Vector3(0,-2.35f,0), Quaternion.identity);
            gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("Env").transform);
            RefalshCurrentProductionDialog();
            RefalshCurrentProductionStationRecipe();
            // 换天：重置锅视觉 + 刷新订单卡 + 提示音
            PanPresenter.Ensure().ResetVisual();
            AudioManager.Instance.PlaySfx(SfxType.Clock, 0.8f);
            var orderPanel = OrderPanel.Ensure();
            if (orderPanel != null)
            {
                orderPanel.gameObject.SetActive(true);
                orderPanel.Rebuild();
            }
            yield return new WaitForSeconds(1.5f);
            yield return Fade(0);

        }


        private IEnumerator Fade(float targetAlpha)
        {
            InputAllowed = false;
            isFade = true;
            fadeCanvasGroup.blocksRaycasts = true;
            float speed = Mathf.Abs(fadeCanvasGroup.alpha - targetAlpha) / fadeDuration;
            while (!Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
            {
                fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, speed * Time.deltaTime);
                yield return null;
            }
            fadeCanvasGroup.blocksRaycasts = false;
            isFade = false;
            InputAllowed = true;

        }
        public int Npc
        {
            get => npc;
            set
            {
                DialogSystemTest.Instance._index = 0;
                if (TodayHasNextNpc())
                {
                    Debug.Log("next npc");
                    npc = value;
                    RefalshCurrentProductionDialog();
                    RefalshCurrentProductionStationRecipe();
                    // 下一位顾客：重置锅视觉 + 刷新订单卡 + 提示音
                    PanPresenter.Ensure().ResetVisual();
                    AudioManager.Instance.PlaySfx(SfxType.Give, 0.7f);
                    var orderPanel = OrderPanel.Ensure();
                    if (orderPanel != null)
                    {
                        orderPanel.gameObject.SetActive(true);
                        orderPanel.Rebuild();
                    }
                }
                else
                {
                    npc = 0;
                    Day++;
                    // RefalshCurrentProductionDialog();
                    // RefalshCurrentProductionStationRecipe();
                }

            }
        }

        public void RefalshCurrentProductionDialog()
        {

            Debug.Log(Day + "  " + Npc);
            List<DialogData> list = new List<DialogData>();
            list.AddRange(dayDialogList[day].dialogSOs[npc].defaultDialog);
            list.Add(dayDialogList[day].dialogSOs[npc].successDialog);
            list.Add(dayDialogList[day].dialogSOs[npc].failDialog);
            DialogSystemTest.Instance.datas = list;


        }

        private void Start()
        {
            // 帧率上限：2D 小游戏 60fps 足够，避免 CPU/GPU 空转发热（WebGL 下同样生效）
            if (Application.targetFrameRate <= 0 || Application.targetFrameRate > 60)
            {
                Application.targetFrameRate = 60;
            }
            UIManager.Instance.SwitchMainMenuState(true);
            if (UIManager.Instance.MainMenu != null)
            {
                MainMenuEnhancer.Ensure(UIManager.Instance.MainMenu.transform, this);
            }
        }

        private bool _gameStarting;

        public void StartGame()
        {
            if (_gameStarting || isFade) return;
            _gameStarting = true;
            Application.runInBackground = true;
            StartCoroutine(StartGameRoutine());
        }

        IEnumerator StartGameRoutine()
        {
            yield return Fade(1);
            // 如果做存储的话 保存这两个值
            day = 0;
            Npc = 0;
            // UIManager.Instance.HidePanel(UIManager.Instance.MainMenu);
            UIManager.Instance.SwitchMainMenuState(false);
            // 显示订单卡与按键提示
            EnsureGameHud();
            yield return new WaitForSeconds(1f);
            DialogSystemTest.Instance.StartDialog();
            yield return Fade(0);
            AudioManager.Instance.PlayBGM(true);
            Application.runInBackground = false;
            

            // test
            // yield return new WaitForSeconds(2f);
            // StartCoroutine(SwitchDayScene());

            // Debug.Log("开始动画");
            // StartCoroutine(OverLoop());

        }

        void RefalshCurrentProductionStationRecipe()
        {
            if (TodayHasNextNpc())
            {
                ProductionStation.Instance.CurrentRecipe = npcList[Npc].recipe;
            }
        }

        // 创建/显示游戏内 HUD（订单卡 + 按键提示）
        private void EnsureGameHud()
        {
            var orderPanel = ByteLoop.UI.OrderPanel.Ensure();
            if (orderPanel != null)
            {
                orderPanel.gameObject.SetActive(true);
                orderPanel.Rebuild();
            }
            var keyHint = ByteLoop.UI.KeyHintBar.Ensure();
            if (keyHint != null) keyHint.gameObject.SetActive(true);
        }

        // 返回主菜单时隐藏 HUD
        public void HideGameHud()
        {
            if (ByteLoop.UI.OrderPanel.Instance != null) ByteLoop.UI.OrderPanel.Instance.Hide();
            if (ByteLoop.UI.KeyHintBar.Instance != null) ByteLoop.UI.KeyHintBar.Instance.Hide();
        }

        public bool TodayHasNextNpc()
        {
            return Day < dayDialogList.Count && Npc + 1 < dayDialogList[Day].dialogSOs.Count;
        }
        public bool HasNextDay()
        {
            return Day + 1 < dayDialogList.Count;
        }

        public void ExitGame()
        {
            Application.Quit();
        }

    }
    [Serializable]
    public class DayDialog
    {
        [SerializeField] public List<RoleDialogSO> dialogSOs;
    }

}