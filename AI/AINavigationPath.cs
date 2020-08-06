using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AINavigationPath : MonoBehaviour
{
    [SerializeField] private List<Transform> _navigationPoints = new List<Transform>();

    public List<Transform> NavigationPoints { get { return _navigationPoints; } }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < _navigationPoints.Count; i++)
        {
            Gizmos.color = Color.green;
            Vector3 nextNavigationPath = i < _navigationPoints.Count - 1 ? _navigationPoints[i + 1].position : _navigationPoints[0].position;
            Gizmos.DrawLine(_navigationPoints[i].position, nextNavigationPath);
        }
    }
}
