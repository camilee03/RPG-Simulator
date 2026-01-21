using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Notebook : MonoBehaviour
{
    [Header("Notebook Pages")]
    [SerializeField] GameObject[] pages;
    [SerializeField] GameObject openButton;
    [SerializeField] GameObject closeButton;
    int currentPage = 0;
    bool isMoving = false;
    public bool isOpen = false;

    public void SaveContents()
    {

    }

    public void LoadContents()
    {

    }

    public void GoToPage(int pageNumber)
    {
        pages[currentPage].SetActive(false);
        currentPage = pageNumber - 1;
        pages[currentPage].SetActive(true);
    }

    public void Open()
    {
        isOpen = true;
        
        if (!isMoving) StartCoroutine(RaiseNotebook());
        openButton.SetActive(false);
        closeButton.SetActive(true);

        // re-add all page children to active to allow interaction
        for (int i = 0; i < pages[currentPage].transform.childCount; i++)
        {
            pages[currentPage].transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        isOpen = false;
        
        if (!isMoving) StartCoroutine(LowerNotebook());
        openButton.SetActive(true);
        closeButton.SetActive(false);

        // set all page children to inactive to avoid interaction issues and performance hits
        for (int i = 0; i < pages[currentPage].transform.childCount; i++)
        {
            pages[currentPage].transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    IEnumerator RaiseNotebook()
    {
        isMoving = true;
        while (transform.localPosition.y < -100)
        {
            transform.localPosition += new Vector3(0, 10, 0);
            yield return new WaitForSeconds(0.01f);
        }
        Cursor.lockState = CursorLockMode.None;
        isMoving = false;
    }

    IEnumerator LowerNotebook()
    {
        isMoving = true;
        while (transform.localPosition.y > -440)
        {
            transform.localPosition -= new Vector3(0, 10, 0);
            yield return new WaitForSeconds(0.01f);
        }
        Cursor.lockState = CursorLockMode.Locked;
        isMoving = false;
    }
}
