using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ByteLoop.DialogSystem;
using System;
using ByteLoop.Manager;
using ByteLoop.Tool;

public class DialogBox : MonoBehaviour
{
    // [SerializeField] private Image _background;
    [SerializeField] public Widget _widget, _nextCursor;
    [SerializeField] private TMPro.TextMeshProUGUI _npc;
    [SerializeField] private AdvancedText _content;
    // [SerializeField] private Animator _nextCursorAnim;


    public bool _interactable;
    private bool _printFinished;
    private bool _canQuickShow;
    private bool _autoNext;

    private bool CanQuickShow => !_printFinished && _canQuickShow;

    private bool CanNext => _printFinished;

    public Action<bool> OnNext;

    private void Awake()
    {
        _content.OnFinished = PrintFinished;
        WireClickAdvance();
    }

    // 点击对话框任意处：打字中=立即显示全文；打完=推进下一句点单台词
    private void WireClickAdvance()
    {
        var img = GetComponent<Image>();
        if (img != null && GetComponent<Button>() == null)
        {
            var btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(HandleAdvanceClick);
        }
        if (_nextCursor != null && _nextCursor.GetComponent<Button>() == null)
        {
            var cursorImg = _nextCursor.GetComponent<Image>();
            if (cursorImg != null)
            {
                var cursorBtn = _nextCursor.gameObject.AddComponent<Button>();
                cursorBtn.transition = Selectable.Transition.None;
                cursorBtn.onClick.AddListener(HandleAdvanceClick);
            }
        }
    }

    private void HandleAdvanceClick()
    {
        if (CanQuickShow)
        {
            _content.QuickShowRemaining();
            return;
        }
        if (!CanNext) return;

        // 只在还有点单台词时点击推进；结果台词（成功/失败）由 B/A 键驱动，避免剧透
        var dst = DialogSystemTest.Instance;
        if (dst != null && dst._index > 0 && dst._index < dst.DefaultLineCount)
        {
            _interactable = false;
            _nextCursor.Fade(0, 0.2f, null);
            OnNext?.Invoke(false);
        }
    }
    // 打印结束
    private void PrintFinished()
    {
        if (_autoNext)
        {
            _interactable = false;
            OnNext?.Invoke(false);
        }
        else
        {
            _interactable = true;
            _printFinished = true;
            _nextCursor.Fade(1, 0.2f, () =>
            {
                // 出现时弹一下，提示可点击
                Juice.PunchScale(_nextCursor.transform, 0.3f, 0.45f);
            });
        }
    }

    // private void Update()
    // {
    //     Debug.Log(_interactable);
    //     if (_interactable)
    //     {
    //         // UpdateInput();
    //         GameManager.Instance.InputAllowed = true;
    //     }else{
    //         GameManager.Instance.InputAllowed = false;
    //     }
    // }


    public void ShowTextByRecipeState(RecipeState recipeState)
    {
        switch (recipeState)
        {
            case RecipeState.Fail:
                DialogSystemTest.Instance._index=3;
                if (CanNext)
                {
                    _interactable = false;
                    _nextCursor.Fade(0, 0.5f, null);
                    OnNext?.Invoke(false);
                }
                DialogSystemTest.Instance._index=0;
                break;
            case RecipeState.Success:
                if (CanQuickShow)
                {
                    _content.QuickShowRemaining();
                }
                else if (CanNext)
                {
                    _interactable = false;
                    _nextCursor.Fade(0, 0.5f, null);
                    OnNext?.Invoke(true);
                }
                DialogSystemTest.Instance._index=0;
                break;
        }
        // Debug.Log(GameManager.Instance.TodayHasNextNpc());
        // if (GameManager.Instance.TodayHasNextNpc())
        // {
        //     GameManager.Instance.Npc++;
        // }

    }

    private void UpdateInput()
    {
        // if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.N))
        // {
        //     Debug.Log("Cancel");
        //     if (CanQuickShow)
        //     {
        //         _content.QuickShowRemaining();
        //     }
        //     else if (CanNext)
        //     {
        //         _interactable = false;
        //         _nextCursor.Fade(0, 0.5f, null);
        //         OnNext?.Invoke(true);
        //     }
        // }
        // else if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.M))
        // {
        //     Debug.Log("Submit");
        //     if (CanNext)
        //     {
        //         _interactable = false;
        //         _nextCursor.Fade(0, 0.5f, null);
        //         OnNext?.Invoke(false);
        //     }
        // }
    }

    public void Open(Action<bool> nextEvent)
    {
        OnNext = nextEvent;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            _widget.RenderOpacity = 0;
            _widget.Fade(1, 0.2f, null);
            _npc.SetText("");
            _content.Init();

        }
        _nextCursor.RenderOpacity = 0;

        OnNext?.Invoke((false));

    }
    public void Close(Action onClosed)
    {
        _widget.Fade(0, 0.4f, () =>
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        });
    }

    public IEnumerator PrintDialog(string content, string npc, bool needTyping = true, bool autoNext = false, bool canQuickShow = true)
    {
        _interactable = false;
        _printFinished = false;
        if (_content.text != "")
        {
            _content.Disappear();
            yield return new WaitForSecondsRealtime(0.2f);
        }
        _canQuickShow = canQuickShow;
        _autoNext = autoNext;
        _npc.SetText(npc); ;
        if (needTyping)
        {
            _interactable = true;
            _content.StartCoroutine(_content.SetText(content, AdvancedText.DisplayType.Typing));
        }
        else
        {
            _content.StartCoroutine(_content.SetText(content, AdvancedText.DisplayType.Fading));
        }
    }



}
