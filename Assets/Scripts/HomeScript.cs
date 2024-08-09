using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeScript : MonoBehaviour
{
    /* Metodo per settare l'ip e caricare la scena principale. In una fase successiva si può scaricare la scena dal
     server e caricare poi tale scena.
     Input: serverIP -> ip o dominio del server a cui connettersi.
     Output: null
     */
    public GameObject keypad;
    public static void LoadServer(string serverIp)
    {
        PlayerPrefs.SetString("ip", serverIp);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
    
    public void ToggleKeypad()
    {
        keypad.SetActive(!gameObject.activeSelf);
    }
}
