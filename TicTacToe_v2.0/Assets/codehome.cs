using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class codehome : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Choose1_0()
    {
        SceneManager.LoadScene(0);
    }
    public void Choose0_1()
    {
        SceneManager.LoadScene(1);
    }
    public void Choose1_1()
    {
        SceneManager.LoadScene(2);
    }
    public void Choose1_2()
    {
        SceneManager.LoadScene(3);
    }
    public void BackHome()
    {
        SceneManager.LoadScene(4);
    }
}
