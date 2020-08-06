using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolUI {
    public Image BackgroundImage = null;
    public Image Image = null;
    public GameObject EmptyImage = null;
    public Tool Tool = null;
}

public class SelectedProjectileUI : MonoBehaviour
{
    [SerializeField] private Image _currentProjectileImage = null;
    [SerializeField] private GameObject ToolUIPrefab = null;
    [SerializeField] private float _step = 100f;
    [SerializeField] private float _swithDelay = .5f;
    [SerializeField] private float _switchSpeed = 10f;

    #region Cache Fields

    private HorizontalLayoutGroup _horizontalLayoutGroup = null;
    public HorizontalLayoutGroup HorizontalLayoutGroup
    {
        get
        {
            if (_horizontalLayoutGroup == null)
                _horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();

            return _horizontalLayoutGroup;
        }
    }

    #endregion

    #region Private Fields

    private List<ToolUI> _toolUIS = new List<ToolUI>();
    private IEnumerator _coroutine = null;

    #endregion 

    private void Start()
    {
        foreach (Tool tool in GameManager.Instance.LocalPlayer.PlayerTools.Tools)
        {
            GameObject tmp = Instantiate(ToolUIPrefab, transform);

            ToolUI toolUI = new ToolUI();

            toolUI.BackgroundImage = tmp.GetComponent<Image>();
            toolUI.Image = tmp.transform.Find("Tool Image").GetComponent<Image>();
            toolUI.EmptyImage = tmp.transform.Find("Empty Image").gameObject;
            toolUI.Tool = tool;
            toolUI.Image.sprite = tool.Sprite;

            _toolUIS.Add(toolUI);
        }
    }

    private void Update()
    {
        for (int i = 0; i < _toolUIS.Count; i++)
        {
            _toolUIS[i].EmptyImage.SetActive(!_toolUIS[i].Tool.InPossession);
            _toolUIS[i].BackgroundImage.enabled = i == GameManager.Instance.LocalPlayer.PlayerTools.SelectedToolIndex;
        }
        
        float nextLeft = -(GameManager.Instance.LocalPlayer.PlayerTools.SelectedToolIndex * _step - _step);
        int left = (int)Mathf.Lerp(HorizontalLayoutGroup.padding.left, nextLeft, _switchSpeed * Time.deltaTime);
        HorizontalLayoutGroup.padding = new RectOffset(left, HorizontalLayoutGroup.padding.right, HorizontalLayoutGroup.padding.top, HorizontalLayoutGroup.padding.bottom);

    }
}
