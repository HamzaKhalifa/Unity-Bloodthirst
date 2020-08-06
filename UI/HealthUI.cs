using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider = null;

    private void Update()
    {
        if (GameManager.Instance.LocalPlayer != null)
        {
            _healthSlider.value = GameManager.Instance.LocalPlayer.Health.HitPointsRemaining / GameManager.Instance.LocalPlayer.Health.HitPoints;
        }
    }
}
