using System;
using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using UnityEngine;
using ByteLoop.DialogSystem;

namespace ByteLoop.Manager
{


    public class ProductionStation : PersistentMonoSingleton<ProductionStation>
    {
        [SerializeField] private Recipe currentRecipe;
        [SerializeField] private RecipeState currentRecipeState = RecipeState.None;
        // public List<Recipe> RecipeList;  // GameManager 每次更新
        // private bool   useOil=false,usePaste=false;

        public RecipeState CurrentRecipeState
        {
            get => currentRecipeState;
            set
            {
                currentRecipeState = value;
                if (currentRecipeState == RecipeState.Success || currentRecipeState == RecipeState.Fail)
                {
                    GameManager.Instance._dialogBox.ShowTextByRecipeState(currentRecipeState);

                    CurrentRecipeState = RecipeState.None;
                }


            }
        }

        public Recipe CurrentRecipe
        {
            get => currentRecipe;
            set
            {
                currentRecipe = value;
            }
        }


        public void ClearCurrentProduction()
        {
            GameObject Go = GameObject.FindGameObjectWithTag("MarterialParent");
            Transform transform;
            for (int i = 0; i < Go.transform.childCount; i++)
            {
                transform = Go.transform.GetChild(i);
                GameObject.Destroy(transform.gameObject);
            }
            // Destroy 延迟到帧末执行，下一帧再刷新订单卡
            Juice.DelayCall(0f, () =>
            {
                if (ByteLoop.UI.OrderPanel.Instance != null)
                    ByteLoop.UI.OrderPanel.Instance.Refresh();
            });
        }

        private void Update()
        {
            if (!DialogSystemTest.Instance._dialogBox._interactable && GameManager.Instance.InputAllowed)
            {
                return;
            }
            if (currentRecipeState == RecipeState.Confirm && Input.GetKeyDown(KeyCode.B) )
            {
                // 先让锅视觉层接管（折叠→滑出→结果CG），再清锅
                PanPresenter.Ensure().BeginServe(true);
                CurrentRecipeState = RecipeState.Success;
                ClearCurrentProduction();
            }
            else if (currentRecipeState == RecipeState.UnConfirm && Input.GetKeyDown(KeyCode.B) )
            {
                PanPresenter.Ensure().BeginServe(false);
                CurrentRecipeState = RecipeState.Fail;
                ClearCurrentProduction();
            }
            else if ( DialogSystemTest.Instance._index == 0 && Input.GetKeyDown(KeyCode.A))
            {
                GameManager.Instance.Npc++;
                DialogSystemTest.Instance.Next(false);
            }
        }
        protected override void Awake()
        {
            base.Awake();
            // RecipeList = new List<Recipe>();
        }
        private void OnEnable()
        {
            EventCenter.ComfirmCurrentProductionEvent += CheckCurrentRecipeState;

        }

        private void OnDisable()
        {
            EventCenter.ComfirmCurrentProductionEvent -= CheckCurrentRecipeState;
        }
        // void CheckCanUseMaterial(){
        //     GameObject Go = GameObject.FindGameObjectWithTag("MarterialParent");
        //     Material[] materials = Go.GetComponentsInChildren<Material>();
        // }

        void CheckCurrentRecipeState()
        {
            GameObject Go = GameObject.FindGameObjectWithTag("MarterialParent");
            Material[] materials = Go.GetComponentsInChildren<Material>();
            Dictionary<MaterialType, int> dic = new Dictionary<MaterialType, int>();
            // 整理已经加工好的原料
            for (int i = 0; i < materials.Length; i++)
            {
                if (dic.ContainsKey(materials[i].Type))
                {
                    dic[materials[i].Type]++;
                }
                else
                {
                    dic.Add(materials[i].Type, 1);
                }
            }

            if(!dic.ContainsKey(MaterialType.Flour)){
                CurrentRecipeState = RecipeState.None;
                Debug.Log("没有面糊");
                return;
            }

            int count = currentRecipe.RecipeItemList.Count;
            // 对比菜单和原料
            for (int i = 0; i < count; i++)
            {
                MaterialType materialType = currentRecipe.RecipeItemList[i].materialTypes;
                // Debug.Log(dic.ContainsKey(materialType));
                if (dic.ContainsKey(materialType))
                {
                    currentRecipeState = RecipeState.UnConfirm;
                   
                    if (dic[materialType] < currentRecipe.RecipeItemList[i].nums)
                    {
                        Debug.Log("UnConfirm");

                        return;
                    }
                }
                else
                {
                    currentRecipeState = RecipeState.UnConfirm;
                    // Debug.Log("ssssssssssss");
                    // currentRecipeState=RecipeState.None;
                    return;
                }
            }
            // Debug.Log(currentRecipe.RecipeItemList.Count);
            // Debug.Log(dic.Count);
            // Debug.Log("Confirm");
            currentRecipeState = RecipeState.Confirm;

            // currentRecipe.RecipeItemList.Contains
            // return RecipeState.Confirm;

        }
    }
}