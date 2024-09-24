using UnityEngine;

public class Profile : MonoBehaviour
{
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private UnityEngine.UI.Image profileImage;
    [SerializeField] private string information;

    public bool IsSelected = false;
    
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        selectedIndicator.SetActive(selected);
    }
}
