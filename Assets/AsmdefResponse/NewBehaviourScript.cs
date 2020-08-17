using System.Threading;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("main thread log message.");
        new Thread(() =>
        {
            Debug.Log("background thread log message.");
        }).Start();
    }
}
