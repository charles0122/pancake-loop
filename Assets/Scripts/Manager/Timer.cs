// using System;
using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using UnityEngine;
using UnityEngine.UI;
using ByteLoop.Manager;

public class Timer : MonoBehaviour
{

    [SerializeField] private Slider slider;
    [field: SerializeField] public bool Start { get; set; } = false;

    [field: SerializeField] public float TotalTime { get; set; } = 10f;
    [SerializeField] private float startTime = 0;

    private bool _tickPlayed;

    private void Awake()
    {
        GameManager.Instance.InputAllowed = false;
        // 进度条染成金色，和煎饼的视觉呼应
        if (slider != null && slider.fillRect != null)
        {
            var fill = slider.fillRect.GetComponent<Image>();
            if (fill != null) fill.color = new Color(1f, 0.84f, 0.35f);
        }
    }

    private void Update()
    {
        if (Start && startTime <= TotalTime)
        {
            slider.value = startTime / TotalTime;
            startTime += Time.deltaTime;
            // 快完成时滴答提示
            if (!_tickPlayed && TotalTime - startTime <= 0.6f)
            {
                _tickPlayed = true;
                AudioManager.Instance.PlaySfx(SfxType.Clock, 0.6f);
            }
        }

        if (startTime > TotalTime)
        {
            Destroy(this.gameObject);
        }
    }
    private void OnDestroy()
    {
        GameManager.Instance.InputAllowed = true;
    }


}
