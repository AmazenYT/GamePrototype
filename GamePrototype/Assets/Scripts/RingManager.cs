using UnityEngine;
using TMPro;
public class RingManager : MonoBehaviour
{
    public int ringCount;
    public TextMeshProUGUI ringText;
    
    // Update is called once per frame
    void Update()
    {
        ringText.text = "Rings: " + ringCount.ToString();
    }
}
