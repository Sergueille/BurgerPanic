using System.Collections.Generic;
using UnityEngine;

public class IngredientGenerator : MonoBehaviour
{
    [SerializeField] private Collider2D clickCollider;
    [SerializeField] private GameObject[] prefabs;

    private void Start()
    {
        GameManager.i.ingredientGenerators.Add(this);
    }

    private void OnDestroy()
    {
        GameManager.i.ingredientGenerators.Remove(this);
    }

    public bool TestGenerator()
    {
        Vector2 mousePos = GameManager.i.mainCamera.ScreenToWorldPoint(Input.mousePosition);

        if (clickCollider.OverlapPoint(mousePos))
        {
            GameObject newSalad = Instantiate(prefabs[UnityEngine.Random.Range(0, prefabs.Length)]);
            InteractableObject saladInter = newSalad.GetComponent<InteractableObject>();

            newSalad.transform.position = mousePos + Util.RandomVectorInCircle(saladInter.interactRadius);
    
            GameManager.i.grabbedObject = saladInter;
            saladInter.OnGrabbed();

            return true;
        }

        return false;
    }
}


