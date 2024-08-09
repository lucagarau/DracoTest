using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NumpadScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ipText = null;
    [SerializeField] private UpdateMeshListServer _updatemeshlistserver = null;
    // Start is called before the first frame update
    void Start()
    {
        if(_ipText == null)
        {
            _ipText = GameObject.Find("IpText").GetComponent<TextMeshProUGUI>();
        }
    }

    public void KeyPressed(string key)
    {
        if (key == "DEL" && _ipText.text.Length > 0)
        {
            _ipText.text = _ipText.text.Substring(0, _ipText.text.Length - 1);
        }
        else if (key == "INV")
        {
            string ipPattern = @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
            Regex regex = new Regex(ipPattern);
            if (regex.IsMatch(_ipText.text))
            {
                //_updatemeshlistserver.SetIp();
                PlayerPrefs.SetString("ip", _ipText.text);
                PlayerPrefs.Save();
                SceneManager.LoadScene(1);
            }
            else Debug.Log("Invalid IP");
        }
        else
        {
            _ipText.text += key;
            Debug.Log(_ipText.text);
        }
        
        
        
    }
    
    public void toggleNumpad()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
