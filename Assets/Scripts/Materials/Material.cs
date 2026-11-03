using System.Collections;
using System.Collections.Generic;
using ByteLoop.Manager;
using ByteLoop.Tool;
using UnityEngine;
using System;

public class Material : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Collider2D _collider2D;
    [SerializeField] protected MaterialType _type = MaterialType.None;
    [SerializeField] protected float _needTime = 5f;
    [SerializeField] protected Animator _anim;

    [SerializeField] protected Sprite defaultSprite, dragSprite, productionSprite;
    protected Vector2 startPos;
    [SerializeField] protected Transform targetTrans;
    [SerializeField] private bool isFinished;


    public MaterialType Type => _type;

    private Vector3 _baseScale = Vector3.one;

    private void Start()
    {
        targetTrans = GameObject.FindGameObjectWithTag("MarterialParent").transform;
        if (defaultSprite != null)
        {
            _spriteRenderer.sprite = defaultSprite;
        }
        startPos = transform.position;
        _baseScale = transform.localScale;
    }
    private void OnMouseDrag()
    {
        if (!isFinished&&dragSprite != null)
        {
            _spriteRenderer.sprite = dragSprite;
        }
        if (!isFinished && GameManager.Instance.InputAllowed)
        {
            Vector3 vector3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector2(vector3.x, vector3.y);
        }
        else
        {
            Debug.Log("不允许移动或点击" + GameManager.Instance.InputAllowed);
        }
    }

    private void OnMouseDown()
    {
        // 拖起任意配料时高亮锅的落点
        if (!isFinished && GameManager.Instance.InputAllowed)
        {
            PanPresenter.Ensure().OnDragStart();
        }
    }

    private void OnMouseUp()
    {

        if (!isFinished)
        {
            PanPresenter.Ensure().OnDragEnd();
            // 距离锅中心的多少距离  3f   可以改成原料属性
            if (Mathf.Abs(transform.position.x - targetTrans.position.x) <= 3f &&
            Mathf.Abs(transform.position.y - targetTrans.position.y) <= 3f)
            {
                transform.position = new Vector2(transform.position.x, transform.position.y);
                isFinished = true;
                _collider2D.enabled = false; // 落锅后不再响应鼠标
                StartCoroutine(InstantiateMaterial(transform.position));
            }
            else
            {
                // 没扔进锅：抖一下再滑回原位
                Juice.Shake(transform, 0.25f, 0.2f);
                Juice.DelayCall(0.22f, () => Juice.TweenPosition(transform, startPos, 0.3f, Juice.Ease.Back));
            }
        }
    }
    private void OnMouseEnter()
    {
        if(!isFinished && GameManager.Instance.InputAllowed)
            Juice.TweenScale(transform, _baseScale * 1.08f, 0.12f);
    }
    private void OnMouseExit()
    {
        if(!isFinished)
            Juice.TweenScale(transform, _baseScale, 0.12f);
    }

    // private void OnMouseDown()
    // {
    //     if (GameManager.Instance.InputAllowed)
    //     {
    //         StartCoroutine(InstantiateMaterial());
    //     }
    //     else
    //     {
    //         Debug.Log("不允许点击");
    //     }
    // }
    private IEnumerator InstantiateMaterial(Vector2 pos)
    {
        if (productionSprite != null)
        {
            _spriteRenderer.sprite = productionSprite;
        }
        GameManager.Instance.InputAllowed = false;
        // Debug.Log("添加原材料"+_type);
        // GameObject GO = Instantiate(this.gameObject, pos, Quaternion.identity);
        this.transform.SetParent(GameObject.FindGameObjectWithTag("MarterialParent").transform);
        if (ByteLoop.UI.OrderPanel.Instance != null)
        {
            ByteLoop.UI.OrderPanel.Instance.Refresh();
        }
        // this.gameObject.SetActive(false);

        // 通知锅视觉层：落位/弹跳/倒面糊等
        PanPresenter.Ensure().OnMaterialDropped(this);

        // Timer
        GameObject GO = Instantiate(GameManager.Instance.TimerGO);
        GO.GetComponent<Timer>().TotalTime = _needTime;
        GO.GetComponent<Timer>().Start = true;
        // 滑条移到锅上方
        GO.transform.position = PanPresenter.Ensure().CookUiPos;

        yield return new WaitForSeconds(_needTime);
        // 检查是否
        EventCenter.CallComfirmCurrentProductionEvent();
        Debug.Log("等待完毕");
        GameManager.Instance.InputAllowed = true;
        // Destroy(this.gameObject);
    }
}



public enum MaterialType
{
    None,
    Egg,    // 鸡蛋
    Pepper, // 胡椒粉
    Sauce,      // 甜面酱
    Sausage,    // 芝麻
    Sesame,     // 芝麻
    Coriander,  // 香菜
    Onion,      // 香葱
    RemaineLettuce,     // 生菜
    Flour,      // 面糊
    Meat,   // 里脊
    Oil,    // 油
    Crisp   // 薄脆
}
