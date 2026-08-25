using UnityEngine;

public class PlusButtonClick : MonoBehaviour
{
    public GameObject labelText;

    void OnMouseDown()
    {
        if (labelText != null)
        {
            labelText.SetActive(true);
        }
        gameObject.SetActive(false);
    }
}