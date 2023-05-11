using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DataText : MonoBehaviour
{
    private TMP_Text _tmpText;
    [SerializeField] private AppsFlyerObjectScript _appsFlyer;

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
    }
    void Start()
    {
      SDKText();
    }

    public void SDKText()
    {
        _tmpText.text = _appsFlyer._datatext;
    }
  
}