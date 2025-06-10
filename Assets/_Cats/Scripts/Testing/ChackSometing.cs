using UnityEngine;
using UnityEngine.SceneManagement;

public class ChackSometing : CATSMonoBehaviour
{
   [SerializeField] private string NameOfTheScene = "Game";

   public override void Start()
   {
      base.Start();
      Debug.Log("Chack Someting started");
   }


   public override void OnUpdate(float deltaTime)
   {
      base.OnUpdate(deltaTime);
      Debug.Log("im here show me the counter " + this.gameObject.name);
   }


   [ContextMenu("Something")]
   public void MoveToScene()
   {
      _manager.SceneManager.LoadScene(NameOfTheScene);
   }
}
