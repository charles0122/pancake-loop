using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using UnityEngine;
using ByteLoop.Manager;
using UnityEngine.UI;

public class DialogSystemTest : PersistentMonoSingleton<DialogSystemTest>
{
    [SerializeField] public DialogBox _dialogBox;
    [Multiline][SerializeField] private string content;
    public List<DialogData> datas;
    [SerializeField]public int _index = 0;
    [SerializeField] private Widget _widget;


    public void Next(bool forceDisplayDirectly)
    {
        AudioManager.Instance.PlayNpcVoice(GameManager.Instance.Npc);
        _dialogBox._nextCursor.GetComponent<Image>().sprite =  GameManager.Instance.npcList[GameManager.Instance.Npc].RoleAvatar;
        _dialogBox.StartCoroutine(_dialogBox.PrintDialog(
            datas[_index].Content,
            GameManager.Instance.npcList[GameManager.Instance.Npc].RoleName,
            !forceDisplayDirectly && datas[_index].NeedTyping,
            datas[_index].AutoNext,
            datas[_index].CanQuickShow
        ));
        _index++;
        _index = _index % datas.Count;


    }

    public void StartDialog()
    {
        _dialogBox.Open(Next);
    }

    /// <summary>点单台词数量（datas 末尾两条固定是成功/失败台词）。</summary>
    public int DefaultLineCount => datas != null ? Mathf.Max(0, datas.Count - 2) : 0;
}
